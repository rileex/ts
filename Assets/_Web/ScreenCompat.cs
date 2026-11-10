using UnityEngine;

namespace MicroCar.Web
{
    /// <summary>
    /// Screen calls the template makes that must not run in a browser. The quality menu used to
    /// lower the render resolution with Screen.SetResolution(..., fullscreen: true): in WebGL that
    /// resizes the canvas and forces fullscreen, which breaks the page inside the YouTube frame.
    /// On the web the render scale is left to the page (devicePixelRatio is clamped in index.html).
    /// </summary>
    public static class ScreenCompat
    {
        public static void SetResolution(int width, int height, bool fullscreen)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            Screen.SetResolution(width, height, fullscreen);
#endif
        }
    }
}
