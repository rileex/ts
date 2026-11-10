using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Getting the WebGL build under McPlay's per-file limit (30 MiB hard, 15 MB recommended).
///
/// The first YouTube build was one 157 MiB WebGL.data: 316 MB of user assets, 86% of them
/// textures. Everything here works through WebGL platform overrides of the import settings,
/// so Android and iOS keep their own quality; mesh compression is the one global setting.
///
/// Reads import settings only (never loads pixels), the same rule as Car Simulator's ShrinkBuild.
/// Menu: Build > Web Shrink > ...
/// </summary>
public static class WebShrink
{
    const string Platform = "WebGL";
    static readonly string[] TextureExtensions = { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tif", ".tiff", ".bmp", ".exr", ".hdr", ".gif" };
    static readonly string[] AudioExtensions = { ".wav", ".mp3", ".ogg", ".aif", ".aiff" };
    static readonly string[] ModelExtensions = { ".fbx", ".obj", ".blend", ".dae", ".3ds", ".max" };

    static string ReportPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "shrink-report.txt");

    /// <summary>Everything the player will contain: the enabled scenes' dependency trees plus every Resources folder.</summary>
    public static string[] BuildAssets()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var set = new HashSet<string>(AssetDatabase.GetDependencies(scenes, true));
        foreach (var path in AssetDatabase.GetAllAssetPaths())
            if (IsInResources(path) && !AssetDatabase.IsValidFolder(path))
                foreach (var dep in AssetDatabase.GetDependencies(path, true))
                    set.Add(dep);
        return set.Where(p => p.StartsWith("Assets/") || p.StartsWith("Packages/")).OrderBy(p => p).ToArray();
    }

    static bool IsInResources(string path)
    {
        if (!path.StartsWith("Assets/")) return false;
        var parts = path.Split('/');
        bool resources = false;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i] == "Editor") return false;
            if (parts[i] == "Resources") resources = true;
        }
        return resources;
    }

    static bool Has(string path, string[] exts) { var l = path.ToLowerInvariant(); return exts.Any(e => l.EndsWith(e, StringComparison.Ordinal)); }

    /// <summary>Rough size of a texture in the build: DXT1 = 0.5 B/px, DXT5/BC5/RGBM = 1 B/px, +1/3 for mips.</summary>
    static void SourceSize(TextureImporter ti, out int w, out int h)
    {
        // Throws for a texture whose import never finished; assume it is as large as it is allowed to be.
        try { ti.GetSourceTextureWidthAndHeight(out w, out h); }
        catch (InvalidOperationException) { w = h = ti.maxTextureSize; }
    }

    static long Estimate(TextureImporter ti, string path, int maxOverride = -1, bool? crunchOverride = null)
    {
        SourceSize(ti, out int w, out int h);
        var ps = ti.GetPlatformTextureSettings(Platform);
        int max = maxOverride > 0 ? maxOverride : (ps.overridden ? ps.maxTextureSize : ti.maxTextureSize);
        bool crunch = crunchOverride ?? (ps.overridden ? ps.crunchedCompression : ti.crunchedCompression);
        while (w > max || h > max) { w = Math.Max(1, w / 2); h = Math.Max(1, h / 2); }
        // DoesSourceTextureHaveAlpha only answers inside a postprocessor; guess from the format instead.
        string lower = path.ToLowerInvariant();
        bool sourceAlpha = lower.EndsWith(".png") || lower.EndsWith(".tga") || lower.EndsWith(".psd") || lower.EndsWith(".tif");
        bool alpha = sourceAlpha && ti.alphaSource != TextureImporterAlphaSource.None;
        bool hdrOrNormal = ti.textureType == TextureImporterType.NormalMap || ti.textureType == TextureImporterType.Lightmap || path.EndsWith(".exr", StringComparison.OrdinalIgnoreCase);
        double bpp = (alpha || hdrOrNormal) ? 1.0 : 0.5;
        if (ti.textureCompression == TextureImporterCompression.Uncompressed && !ps.overridden) bpp = alpha ? 4 : 3;
        double bytes = w * (double)h * bpp * (ti.mipmapEnabled ? 4.0 / 3.0 : 1.0);
        if (ti.textureShape == TextureImporterShape.TextureCube) bytes *= 1; // cube faces are already in the source size
        if (crunch) bytes *= 0.35;
        return (long)bytes;
    }

    [MenuItem("Build/Web Shrink/Report")]
    public static string Report()
    {
        var assets = BuildAssets();
        var sb = new StringBuilder();
        long texTotal = 0, texShrunk = 0;
        var rows = new List<(long size, long shrunk, string line)>();
        var byFolder = new Dictionary<string, long>();

        foreach (var path in assets.Where(p => Has(p, TextureExtensions)))
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
            SourceSize(ti, out int w, out int h);
            long est = Estimate(ti, path);
            long shrunk = Estimate(ti, path, PlannedMax(ti, path), PlannedCrunch(ti, path));
            texTotal += est; texShrunk += shrunk;
            var ps = ti.GetPlatformTextureSettings(Platform);
            string folder = FolderKey(path);
            byFolder[folder] = (byFolder.TryGetValue(folder, out var f) ? f : 0) + est;
            rows.Add((est, shrunk, $"{est / 1048576.0,6:F2} -> {shrunk / 1048576.0,5:F2} MiB  {w}x{h} max{ti.maxTextureSize}{(ps.overridden ? " web" + ps.maxTextureSize : "")} {ti.textureType}{(ti.crunchedCompression ? " crunch" : "")}  {path}"));
        }

        sb.AppendLine($"textures: {rows.Count}, estimated now {texTotal / 1048576.0:F1} MiB, after plan {texShrunk / 1048576.0:F1} MiB");
        sb.AppendLine("\nby folder (estimated now):");
        foreach (var kv in byFolder.OrderByDescending(k => k.Value).Take(25))
            sb.AppendLine($"  {kv.Value / 1048576.0,7:F1} MiB  {kv.Key}");

        sb.AppendLine("\nheaviest textures:");
        foreach (var r in rows.OrderByDescending(r => r.size).Take(60)) sb.AppendLine("  " + r.line);

        long audioSrc = 0;
        sb.AppendLine("\naudio:");
        foreach (var path in assets.Where(p => Has(p, AudioExtensions)))
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter ai)) continue;
            long len = new FileInfo(path).Length; audioSrc += len;
            var s = ai.GetOverrideSampleSettings(Platform);
            if (len > 300 * 1024)
                sb.AppendLine($"  {len / 1048576.0,6:F2} MiB src  {s.loadType} {s.compressionFormat} q{s.quality:F2} mono={ai.forceToMono}  {path}");
        }
        sb.AppendLine($"  audio source total {audioSrc / 1048576.0:F1} MiB");

        long modelSrc = 0;
        var models = new List<(long, string)>();
        foreach (var path in assets.Where(p => Has(p, ModelExtensions)))
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter mi)) continue;
            long len = new FileInfo(path).Length; modelSrc += len;
            models.Add((len, $"  {len / 1048576.0,6:F2} MiB src  compression={mi.meshCompression} rw={mi.isReadable}  {path}"));
        }
        sb.AppendLine($"\nmodels: {models.Count}, source total {modelSrc / 1048576.0:F1} MiB; heaviest:");
        foreach (var m in models.OrderByDescending(m => m.Item1).Take(15)) sb.AppendLine(m.Item2);

        sb.AppendLine("\nCamera Filter Pack / SSR scripts reached by the scenes:");
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        foreach (var p in AssetDatabase.GetDependencies(scenes, true).Where(p => p.EndsWith(".cs") && (p.Contains("Camera Filter Pack") || p.Contains("StochasticSSR"))))
            sb.AppendLine("  " + p);

        sb.AppendLine("\nResources folders (all of it ships):");
        foreach (var g in AssetDatabase.GetAllAssetPaths().Where(p => IsInResources(p) && !AssetDatabase.IsValidFolder(p)).GroupBy(FolderKey))
            sb.AppendLine($"  {g.Count(),4} files {g.Sum(p => File.Exists(p) ? new FileInfo(p).Length : 0) / 1048576.0,7:F1} MiB src  {g.Key}");

        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, sb.ToString());
        Debug.Log("[WebShrink] report written to " + ReportPath);
        return sb.ToString();
    }

    /// <summary>
    /// The whole shrink, in order. Idempotent: run it again after adding content.
    /// Writes "RUNNING" / the summary to Builds/shrink-apply.txt so a script can wait on it.
    /// </summary>
    [MenuItem("Build/Web Shrink/Apply All")]
    public static string ApplyAll()
    {
        var marker = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "shrink-apply.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(marker));
        File.WriteAllText(marker, "RUNNING");
        string summary;
        try
        {
            summary = MoveUnusedFilterResources() + "\n" + ApplyTextureOverrides() + "\n" + ApplyAudioOverrides() + "\n" + CompressMeshes();
        }
        catch (Exception ex) { summary = "EXCEPTION\n" + ex; }
        File.WriteAllText(marker, "done\n" + summary);
        Debug.Log("[WebShrink] " + summary);
        return summary;
    }

    const string FilterPack = "Assets/Camera Filter Pack";
    const string FilterResources = FilterPack + "/Resources";
    const string FilterParked = FilterPack + "/NotInBuild";

    /// <summary>
    /// Camera Filter Pack keeps 294 shaders and 43 textures (75 MiB in the build) in a Resources
    /// folder, and everything in a Resources folder ships. The game uses one filter,
    /// CameraFilterPack_TV_ARCADE, which needs only its shader (found with Shader.Find).
    /// The rest is parked in a folder that is not called Resources; GUIDs survive the move.
    /// To use another filter later, move its shader (and textures) back into Resources.
    /// </summary>
    [MenuItem("Build/Web Shrink/Park Unused Camera Filters")]
    public static string MoveUnusedFilterResources()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var used = AssetDatabase.GetDependencies(scenes, true)
            .Where(p => p.StartsWith(FilterPack + "/Scripts/") && p.EndsWith(".cs"))
            .Select(p => Path.GetFileNameWithoutExtension(p))
            .ToHashSet();

        if (!AssetDatabase.IsValidFolder(FilterResources)) return "filters: nothing to park";
        if (!AssetDatabase.IsValidFolder(FilterParked)) AssetDatabase.CreateFolder(FilterPack, "NotInBuild");

        int moved = 0, kept = 0;
        {
            foreach (var guid in AssetDatabase.FindAssets("", new[] { FilterResources }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path)) continue;
                var name = Path.GetFileNameWithoutExtension(path);
                // A used filter keeps its shader and any texture named after it.
                if (used.Any(u => name == u || name.StartsWith(u + "_"))) { kept++; continue; }
                var error = AssetDatabase.MoveAsset(path, FilterParked + "/" + Path.GetFileName(path));
                if (string.IsNullOrEmpty(error)) moved++; else Debug.LogWarning("[WebShrink] " + error);
            }
        }
        AssetDatabase.Refresh();
        return $"filters: used [{string.Join(", ", used)}], kept {kept}, parked {moved} in {FilterParked}";
    }

    /// <summary>WebGL override per texture: capped size, crunch where it is safe. Other platforms untouched.</summary>
    [MenuItem("Build/Web Shrink/Texture Overrides")]
    public static string ApplyTextureOverrides()
    {
        var todo = new List<string>();
        foreach (var path in BuildAssets().Where(p => p.StartsWith("Assets/") && Has(p, TextureExtensions)))
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
            var ps = ti.GetPlatformTextureSettings(Platform);
            int max = Math.Min(PlannedMax(ti, path), ti.maxTextureSize);
            bool crunch = PlannedCrunch(ti, path) && ti.textureCompression != TextureImporterCompression.Uncompressed;
            if (ps.overridden && ps.maxTextureSize == max && ps.crunchedCompression == crunch
                && ps.textureCompression == WebCompression(ti, crunch)) continue;
            todo.Add(path);
        }

        int done = 0;
        // No StartAssetEditing: SaveAndReimport inside it makes later GetAtPath calls return null
        // (seen in Car Simulator's ShrinkBuild), and the pass then silently does a fraction of the work.
        {
            foreach (var path in todo)
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                var ps = ti.GetPlatformTextureSettings(Platform);
                ps.overridden = true;
                ps.maxTextureSize = Math.Min(PlannedMax(ti, path), ti.maxTextureSize);
                ps.crunchedCompression = PlannedCrunch(ti, path) && ti.textureCompression != TextureImporterCompression.Uncompressed;
                ps.compressionQuality = 50;
                ps.textureCompression = WebCompression(ti, ps.crunchedCompression);
                ps.format = TextureImporterFormat.Automatic;
                ti.SetPlatformTextureSettings(ps);
                ti.SaveAndReimport();
                done++;
                if (done % 50 == 0) Debug.Log($"[WebShrink] textures {done}/{todo.Count}");
            }
        }
        return $"textures: WebGL override set on {done} (of {todo.Count} needing it)";
    }

    /// <summary>WebGL audio: music at 35% AAC quality, effects at 60%. Source files untouched.</summary>
    [MenuItem("Build/Web Shrink/Audio Overrides")]
    public static string ApplyAudioOverrides()
    {
        int done = 0;
        foreach (var path in BuildAssets().Where(p => p.StartsWith("Assets/") && Has(p, AudioExtensions)))
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter ai)) continue;
            bool music = new FileInfo(path).Length > 2 * 1024 * 1024;
            var s = ai.GetOverrideSampleSettings(Platform);
            float quality = music ? 0.25f : 0.6f;
            if (ai.ContainsSampleSettingsOverride(Platform) && Mathf.Approximately(s.quality, quality)) continue;
            s.compressionFormat = AudioCompressionFormat.AAC;
            s.quality = quality;
            s.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            ai.SetOverrideSampleSettings(Platform, s);
            ai.SaveAndReimport();
            done++;
        }
        return $"audio: WebGL override set on {done}";
    }

    /// <summary>
    /// Medium mesh compression for every model the build reaches (global: also helps Android/iOS).
    /// Read/Write is left alone - the car damage code edits vertices at runtime.
    /// </summary>
    [MenuItem("Build/Web Shrink/Compress Meshes")]
    public static string CompressMeshes()
    {
        int done = 0;
        foreach (var path in BuildAssets().Where(p => p.StartsWith("Assets/") && Has(p, ModelExtensions)))
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter mi)) continue;
            if (mi.meshCompression != ModelImporterMeshCompression.Off) continue;
            mi.meshCompression = ModelImporterMeshCompression.Medium;
            mi.SaveAndReimport();
            done++;
        }
        return $"meshes: Medium compression on {done}";
    }

    static string FolderKey(string path)
    {
        var parts = path.Split('/');
        return string.Join("/", parts.Take(Math.Min(parts.Length - 1, 3)));
    }

    /// <summary>The WebGL cap each kind of texture gets.</summary>
    static int PlannedMax(TextureImporter ti, string path)
    {
        if (ti.textureType == TextureImporterType.Lightmap || path.Contains("/Lightmap-")) return 512;
        if (ti.textureShape == TextureImporterShape.TextureCube) return 1024; // the sky: 512 per face looks soft
        // UI: the template already caps most of it at 512 for a 1280x720 canvas; the few 1024-2048 icons
        // are sprites with sizes that are not multiples of 4, so they ship as raw RGBA32 (DXT needs 4x4 blocks).
        if (ti.textureType == TextureImporterType.Sprite || ti.textureType == TextureImporterType.GUI) return 512;
        if (ti.textureType == TextureImporterType.NormalMap) return 256;
        return 512;
    }

    /// <summary>
    /// Never raw RGBA on the web. Crunch only exists for the normal-quality DXT formats: with
    /// HighQuality (BC7 on desktop) the crunch flag is silently ignored, which is how Overcast.png
    /// kept shipping at full size.
    /// </summary>
    static TextureImporterCompression WebCompression(TextureImporter ti, bool crunch)
    {
        if (crunch || ti.textureCompression == TextureImporterCompression.Uncompressed) return TextureImporterCompression.Compressed;
        return ti.textureCompression;
    }

    static bool PlannedCrunch(TextureImporter ti, string path)
    {
        if (ti.textureType == TextureImporterType.Lightmap || path.EndsWith(".exr", StringComparison.OrdinalIgnoreCase)) return false;
        if (ti.textureType == TextureImporterType.NormalMap) return false;
        return true; // the sky cube included: 4 MiB as DXT1, crunch is what lets it keep 1024 per face
    }
}
