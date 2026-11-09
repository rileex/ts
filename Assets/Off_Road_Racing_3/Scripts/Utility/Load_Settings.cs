//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using UnityEngine;
using System.Collections;
using ALIyerEdon;

namespace ALIyerEdon
{
	public class Load_Settings : MonoBehaviour
	{
		public AudioSource music;

        public GameObject smokeEffect; 
        public GameObject Balloons; 

        void Start()
		{
            Update_Balloons();
            Update_SmokeEffect();

            music.volume = PlayerPrefs.GetFloat("Music");

            #region Position_UI
            if (FindFirstObjectByType<Race_Manager>())
			{
				if (PlayerPrefs.GetInt("ShowLocalPosition") == 3)
                    FindFirstObjectByType<Race_Manager>().showLocalPosition = true;
				else
                    FindFirstObjectByType<Race_Manager>().showLocalPosition = false;

                if (PlayerPrefs.GetInt("ShowPositionUI") == 3)
                    FindFirstObjectByType<Race_Manager>().positionUI.SetActive(true);
                else
                    FindFirstObjectByType<Race_Manager>().positionUI.SetActive(false);
            }
            #endregion

            #region Sun_Shaft
            if (GameObject.FindGameObjectWithTag("MainCamera")
                .GetComponent<LightingBox.Effects.SunShafts>())
            {
                if (PlayerPrefs.GetInt("Sun Shaft") == 1)
                {
                    GameObject.FindGameObjectWithTag("MainCamera")
                             .GetComponent<LightingBox.Effects.SunShafts>().enabled = true;
                }
                else
                {
                    GameObject.FindGameObjectWithTag("MainCamera")
                             .GetComponent<LightingBox.Effects.SunShafts>().enabled = false;
                }
            }
            #endregion

            #region Quality_Level
            Set_QualityLevel(PlayerPrefs.GetInt("QualityLevel"));
            #endregion

        }
        public void Update_Balloons()
        {
            if (Balloons)
            {
                if (PlayerPrefs.GetInt("Balloons") == 0)
                    Balloons.SetActive(false);
                if (PlayerPrefs.GetInt("Balloons") == 1)
                    Balloons.SetActive(true);
            }
        }
        public void Update_SmokeEffect()
        {
            if (smokeEffect)
            {
                if (PlayerPrefs.GetInt("Wheel Smoke") == 0)
                    smokeEffect.SetActive(false);
                if (PlayerPrefs.GetInt("Wheel Smoke") == 1)
                    smokeEffect.SetActive(true);
            }
        }
        public void Set_Reflection(bool ssr_1, bool ssr_2)
        {
            Trive.Rendering.StochasticReflections ssr2;

            UnityEngine.Rendering.PostProcessing.ScreenSpaceReflections ssr1;

            GameObject.FindFirstObjectByType<
                    UnityEngine.Rendering.PostProcessing.PostProcessVolume>()
                    .profile.TryGetSettings(out ssr2);

            GameObject.FindFirstObjectByType<
                    UnityEngine.Rendering.PostProcessing.PostProcessVolume>()
                    .profile.TryGetSettings(out ssr1);

            if (ssr_1)
            {
                foreach (Camera cam in FindObjectsOfType<Camera>())
                    cam.renderingPath = RenderingPath.DeferredShading;

                ssr2.enabled.value = false;
                ssr1.enabled.value = true;
            }
            if (ssr_2)
            {
                foreach (Camera cam in FindObjectsOfType<Camera>())
                    cam.renderingPath = RenderingPath.DeferredShading;

                ssr2.enabled.value = true;

                ssr2.resolveDownsample.value = false;
                ssr2.raycastDownsample.value = false;

                ssr1.enabled.value = false;
            }
            if (!ssr_1 && !ssr_2)
            {
                foreach (Camera cam in FindObjectsOfType<Camera>())
                    cam.renderingPath = RenderingPath.Forward;

                ssr2.enabled.value = false;
                ssr1.enabled.value = false;
            }
        }

        public void Update_MusicVolume(float volume)
		{
			music.volume = volume;
		}

        public void Set_QualityLevel(int qualityLevel)
        {

            // Very Low
            if (qualityLevel == 0)
            {
                Screen.SetResolution((int)(PlayerPrefs.GetInt("OriginalX") * 0.5f),
                    (int)(PlayerPrefs.GetInt("OriginalY") * 0.5f), true);

                foreach (UnityEngine.Rendering.PostProcessing.PostProcessLayer lll in
                    FindObjectsOfType<UnityEngine.Rendering.PostProcessing.PostProcessLayer>())
                {
                    lll.enabled = true;
                    lll.antialiasingMode = UnityEngine.Rendering.PostProcessing.PostProcessLayer.Antialiasing.None;
                }
                foreach (Light light in FindObjectsOfType<Light>())
                {
                    light.shadows = LightShadows.None;
                }
                foreach (LightingBox.Effects.GlobalFog gf in FindObjectsOfType<LightingBox.Effects.GlobalFog>())
                {
                    gf.enabled = false;
                }
                Set_Reflection(false, false);
            }
            //_________________________________________
            //  Low
            if (qualityLevel == 1)
            {
                Screen.SetResolution((int)(PlayerPrefs.GetInt("OriginalX") * 0.5f),
                    (int)(PlayerPrefs.GetInt("OriginalY") * 0.5f), true);

                foreach (UnityEngine.Rendering.PostProcessing.PostProcessLayer lll in
                    FindObjectsOfType<UnityEngine.Rendering.PostProcessing.PostProcessLayer>())
                {
                    lll.enabled = true;
                    lll.antialiasingMode = UnityEngine.Rendering.PostProcessing.PostProcessLayer.Antialiasing.None;
                }
                foreach (Light light in FindObjectsOfType<Light>())
                {
                    light.shadows = LightShadows.Soft;
                    light.shadowResolution =
                        UnityEngine.Rendering.LightShadowResolution.Low;
                }
                foreach (LightingBox.Effects.GlobalFog gf in FindObjectsOfType<LightingBox.Effects.GlobalFog>())
                {
                    gf.enabled = false;
                }
                Set_Reflection(false, false);
            }
            //_________________________________________
            //  Medium
            if (qualityLevel == 2)
            {
                Screen.SetResolution((int)(PlayerPrefs.GetInt("OriginalX") * 0.5f),
                    (int)(PlayerPrefs.GetInt("OriginalY") * 0.5f), true);

                foreach (UnityEngine.Rendering.PostProcessing.PostProcessLayer lll in
                    FindObjectsOfType<UnityEngine.Rendering.PostProcessing.PostProcessLayer>())
                {
                    lll.enabled = true;
                    lll.antialiasingMode = UnityEngine.Rendering.PostProcessing.
                        PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
                    lll.subpixelMorphologicalAntialiasing.quality = UnityEngine.Rendering.PostProcessing.SubpixelMorphologicalAntialiasing.Quality.Low;
                }
                foreach (Light light in FindObjectsOfType<Light>())
                {
                    light.shadows = LightShadows.Soft;
                    light.shadowResolution =
                        UnityEngine.Rendering.LightShadowResolution.Medium;
                }
                foreach (LightingBox.Effects.GlobalFog gf in FindObjectsOfType<LightingBox.Effects.GlobalFog>())
                {
                    gf.enabled = true;
                }
                Set_Reflection(false, false);
            }
            //_________________________________________
            //  High
            if (qualityLevel == 3)
            {
                Screen.SetResolution((int)(PlayerPrefs.GetInt("OriginalX") * 0.7f),
                    (int)(PlayerPrefs.GetInt("OriginalY") * 0.7f), true);

                foreach (UnityEngine.Rendering.PostProcessing.PostProcessLayer lll in
                    FindObjectsOfType<UnityEngine.Rendering.PostProcessing.PostProcessLayer>())
                {
                    lll.enabled = true;
                    lll.antialiasingMode = UnityEngine.Rendering.PostProcessing.
                        PostProcessLayer.Antialiasing.TemporalAntialiasing;
                }
                foreach (Light light in FindObjectsOfType<Light>())
                {
                    light.shadows = LightShadows.Soft;
                    light.shadowResolution =
                        UnityEngine.Rendering.LightShadowResolution.High;
                }
                foreach (LightingBox.Effects.GlobalFog gf in FindObjectsOfType<LightingBox.Effects.GlobalFog>())
                {
                    gf.enabled = true;
                }
                Set_Reflection(true, false);
            }
            //_________________________________________
            //  Ultra
            if (qualityLevel == 4)
            {
                Screen.SetResolution((int)(PlayerPrefs.GetInt("OriginalX") * 0.77f),
                    (int)(PlayerPrefs.GetInt("OriginalY") * 0.77f), true);

                foreach (UnityEngine.Rendering.PostProcessing.PostProcessLayer lll in
                    FindObjectsOfType<UnityEngine.Rendering.PostProcessing.PostProcessLayer>())
                {
                    lll.enabled = true;
                    lll.antialiasingMode = UnityEngine.Rendering.PostProcessing.
                        PostProcessLayer.Antialiasing.TemporalAntialiasing;
                }
                foreach (Light light in FindObjectsOfType<Light>())
                {
                    light.shadows = LightShadows.Soft;
                    light.shadowResolution =
                        UnityEngine.Rendering.LightShadowResolution.High;
                }
                foreach (LightingBox.Effects.GlobalFog gf in FindObjectsOfType<LightingBox.Effects.GlobalFog>())
                {
                    gf.enabled = true;
                }
                Set_Reflection(false, true);
            }
            //_________________________________________
            //  Ultra
            if (qualityLevel == 5)
            {
                Screen.SetResolution((int)(PlayerPrefs.GetInt("OriginalX") * 1f),
                    (int)(PlayerPrefs.GetInt("OriginalY") * 1f), true);

                foreach (UnityEngine.Rendering.PostProcessing.PostProcessLayer lll in
                    FindObjectsOfType<UnityEngine.Rendering.PostProcessing.PostProcessLayer>())
                {
                    lll.enabled = true;
                    lll.antialiasingMode = UnityEngine.Rendering.PostProcessing.
                        PostProcessLayer.Antialiasing.TemporalAntialiasing;
                }
                foreach (Light light in FindObjectsOfType<Light>())
                {
                    light.shadows = LightShadows.Soft;
                    light.shadowResolution =
                        UnityEngine.Rendering.LightShadowResolution.High;
                }
                foreach (LightingBox.Effects.GlobalFog gf in FindObjectsOfType<LightingBox.Effects.GlobalFog>())
                {
                    gf.enabled = true;
                }
                Set_Reflection(false, true);
            }
        }
    }
}