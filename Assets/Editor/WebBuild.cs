using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// YouTube Playables (McPlay) WebGL build.
///
/// Menu: Build &gt; YouTube Playables (WebGL)
/// Batch: Unity.exe -batchmode -projectPath &lt;project&gt; -buildTarget WebGL -executeMethod WebBuild.YouTube
///
/// Output: Builds/WebGL-YT (the site) and Builds/MicroCar-YouTube.zip (index.html at the root, ready
/// for McPlay). A report with every file size goes to Builds/last-build-yt.txt; files over McPlay's
/// soft (15 MB) and hard (30 MiB) limits are flagged there.
///
/// Settings touched only for this build (template, compression, texture subtarget) are put back
/// afterwards. See Docs/YouTube-Playables.md.
/// </summary>
public static class WebBuild
{
    const long SoftLimit = 15L * 1000 * 1000;
    const long HardLimit = 30L * 1024 * 1024;

    [MenuItem("Build/YouTube Playables (WebGL)")]
    public static void YouTubeFromMenu()
    {
        string report = Build();
        EditorUtility.DisplayDialog("YouTube Playables build", report, "OK");
    }

    /// <summary>
    /// Same build from a script inside the open editor (no dialog, editor stays open). Schedule it with
    /// EditorApplication.delayCall and watch Builds/last-build-yt.txt: "RUNNING" until it is done.
    /// </summary>
    public static string BuildInEditor() => Build();

    /// <summary>Batch-mode entry point; exits the editor with 0 on success.</summary>
    public static void YouTube()
    {
        string report = Build();
        EditorApplication.Exit(report.StartsWith("result=Succeeded") ? 0 : 1);
    }

    /// <summary>One-time WebGL player settings matching the certified Car Simulator build.</summary>
    [MenuItem("Build/Apply WebGL Player Settings")]
    public static void ApplyPlayerSettings()
    {
        PlayerSettings.WebGL.template = "PROJECT:YouTube";
        PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
        PlayerSettings.WebGL.initialMemorySize = 32;
        PlayerSettings.WebGL.maximumMemorySize = 2048;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
        PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.WebGL, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.stripUnusedMeshComponents = true;
        PlayerSettings.mipStripping = true;
        PlayerSettings.runInBackground = true;
        AssetDatabase.SaveAssets();
        Debug.Log("[WebBuild] WebGL player settings applied.");
    }

    static string Build()
    {
        var root = Directory.GetParent(Application.dataPath).FullName;
        var outDir = Path.Combine(root, "Builds", "WebGL-YT");
        var zip = Path.Combine(root, "Builds", "MicroCar-YouTube.zip");
        var marker = Path.Combine(root, "Builds", "last-build-yt.txt");

        Directory.CreateDirectory(Path.GetDirectoryName(marker));
        File.WriteAllText(marker, "RUNNING");

        string template = PlayerSettings.WebGL.template;
        var compression = PlayerSettings.WebGL.compressionFormat;
        bool fallback = PlayerSettings.WebGL.decompressionFallback;
        var textures = EditorUserBuildSettings.webGLBuildSubtarget;
        bool splash = PlayerSettings.SplashScreen.show;
        bool splashLogo = PlayerSettings.SplashScreen.showUnityLogo;

        string report;
        try
        {
            // WebGL import overrides for anything new (idempotent, seconds when nothing changed).
            // Without them the data file is 157 MiB instead of ~25; see Docs/YouTube-Playables.md.
            string shrink = WebShrink.ApplyAll();
            if (shrink.StartsWith("EXCEPTION")) throw new Exception("WebShrink failed:\n" + shrink);

            // The page already has its own loading screen, and YouTube measures time to the first
            // frame; the Unity splash also puts a 2.7 MB logo texture into the data file.
            // Both flags: with only show=false the 2.7 MB logo texture still went into the data file.
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            PlayerSettings.WebGL.template = "PROJECT:YouTube";
            // Compressed, with the decompressor built into the loader, so the build does not depend
            // on the host sending Content-Encoding headers (same as the certified Car Simulator build).
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            // No desktop WebGL context samples ASTC; DXT is what browsers actually offer.
            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;

            if (Directory.Exists(outDir))
                Directory.Delete(outDir, true);

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None
            });

            var summary = result.summary;
            var sb = new StringBuilder();
            sb.AppendLine("result=" + summary.result);
            sb.AppendLine("errors=" + summary.totalErrors);
            sb.AppendLine("warnings=" + summary.totalWarnings);
            sb.AppendLine("time=" + summary.totalTime);

            if (summary.result == BuildResult.Succeeded)
            {
                long total = 0;
                int soft = 0, hard = 0;
                foreach (var file in Directory.GetFiles(outDir, "*", SearchOption.AllDirectories).OrderByDescending(f => new FileInfo(f).Length))
                {
                    long len = new FileInfo(file).Length;
                    total += len;
                    string flag = len > HardLimit ? "OVER30MiB " : len > SoftLimit ? "over15MB  " : "          ";
                    if (len > HardLimit) hard++; else if (len > SoftLimit) soft++;
                    sb.AppendLine($"{flag}{len / 1024.0 / 1024.0,8:F2} MiB  {file.Substring(outDir.Length + 1)}");
                }

                sb.Insert(0, $"total={total / 1024.0 / 1024.0:F1} MiB over15MB={soft} over30MiB={hard}\n");

                if (File.Exists(zip))
                    File.Delete(zip);
                ZipFile.CreateFromDirectory(outDir, zip, System.IO.Compression.CompressionLevel.Optimal, false);
                sb.AppendLine($"zip={zip} ({new FileInfo(zip).Length / 1024.0 / 1024.0:F1} MiB)");
            }

            report = sb.ToString();
        }
        catch (Exception ex)
        {
            report = "result=EXCEPTION\n" + ex;
        }
        finally
        {
            PlayerSettings.WebGL.template = template;
            PlayerSettings.WebGL.compressionFormat = compression;
            PlayerSettings.WebGL.decompressionFallback = fallback;
            EditorUserBuildSettings.webGLBuildSubtarget = textures;
            PlayerSettings.SplashScreen.show = splash;
            PlayerSettings.SplashScreen.showUnityLogo = splashLogo;
            AssetDatabase.SaveAssets();
        }

        // result=... must stay the first line for YouTube(); the size summary is prepended after it.
        var lines = report.Split('\n').ToList();
        int resultLine = lines.FindIndex(l => l.StartsWith("result="));
        if (resultLine > 0) { var r = lines[resultLine]; lines.RemoveAt(resultLine); lines.Insert(0, r); }
        report = string.Join("\n", lines);

        File.WriteAllText(marker, report);
        Debug.Log("[WebBuild] " + report);
        return report;
    }
}
