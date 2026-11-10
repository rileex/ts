using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MicroCar.Web
{
    /// <summary>
    /// QA aid for the browser build: open the game with <c>?debug=input</c> in the address and every
    /// mouse press / touch logs to the browser console what the UI raycast hits there, which
    /// EventSystem is current and how many there are. Does nothing without the flag, and nothing
    /// at all outside WebGL players.
    /// </summary>
    public sealed class InputProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (string.IsNullOrEmpty(Application.absoluteURL) || !Application.absoluteURL.Contains("debug=input"))
                return;
            var go = new GameObject("InputProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<InputProbe>();
            Debug.Log("[InputProbe] on");
#endif
        }

        readonly List<RaycastResult> hits = new List<RaycastResult>();

        void Update()
        {
            if (Input.GetMouseButtonDown(0)) Report("down", Input.mousePosition);
            if (Input.GetMouseButtonUp(0)) Report("up", Input.mousePosition);
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began || t.phase == TouchPhase.Ended) Report("touch " + t.phase, t.position);
            }
        }

        void Report(string what, Vector2 position)
        {
            var systems = FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var es = EventSystem.current;
            string hitList = "no EventSystem";
            if (es != null)
            {
                hits.Clear();
                es.RaycastAll(new PointerEventData(es) { position = position }, hits);
                hitList = hits.Count == 0 ? "nothing" : string.Join(" | ", hits.Take(5).Select(h => Path(h.gameObject.transform) + " (" + h.gameObject.scene.name + ")"));
            }
            Debug.Log($"[InputProbe] {what} at {position} screen {Screen.width}x{Screen.height} frame {Time.frameCount} " +
                      $"eventSystems={systems.Length} current={(es ? es.name + "@" + es.gameObject.scene.name : "none")} " +
                      $"selected={(es && es.currentSelectedGameObject ? es.currentSelectedGameObject.name : "-")} hits: {hitList}");

            if (what == "down") ExplainButtons(position);
        }

        /// <summary>For every active button: would the GraphicRaycaster take it at this point, and if not, why.</summary>
        static void ExplainButtons(Vector2 position)
        {
            foreach (var b in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var g = b.targetGraphic;
                if (g == null) { Debug.Log("[InputProbe]   " + b.name + ": no target graphic"); continue; }
                var canvas = g.canvas;
                var cam = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                bool inside = RectTransformUtility.RectangleContainsScreenPoint(g.rectTransform, position, cam, g.raycastPadding);
                var corners = new Vector3[4];
                g.rectTransform.GetWorldCorners(corners);
                var screenMin = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                var screenMax = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
                var groups = b.GetComponentsInParent<CanvasGroup>();
                Debug.Log($"[InputProbe]   {Path(b.transform)}: rect {screenMin}-{screenMax} inside={inside} raycastTarget={g.raycastTarget} " +
                          $"cull={g.canvasRenderer.cull} depth={g.depth} graphicRaycast={(inside ? g.Raycast(position, cam).ToString() : "-")} " +
                          $"interactable={b.IsInteractable()} canvas={(canvas ? canvas.name + " mode=" + canvas.renderMode + " scale=" + canvas.scaleFactor + " display=" + canvas.targetDisplay : "none")} " +
                          $"raycaster={(canvas && canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() ? canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>().enabled.ToString() : "none")} " +
                          $"groups={string.Join(",", groups.Select(cg => cg.name + ":" + cg.blocksRaycasts + "/" + cg.interactable))}");
            }
        }

        static string Path(Transform t)
        {
            string p = t.name;
            for (int d = 0; d < 3 && t.parent; d++) { t = t.parent; p = t.name + "/" + p; }
            return p;
        }
    }
}
