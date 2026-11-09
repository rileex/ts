//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using UnityEngine;
using System.Collections;
using ALIyerEdon;

namespace ALIyerEdon
{
	public class CameraSwitch : MonoBehaviour
	{

		// MainCamera
		GameObject mainCamera;

		[Header("The first slot must be empty")]
		[Header("Because is used for Main camera")]
		[Space(3)]

		[Header("Camera List :")]
		// List of the camera's gameObjects
		public GameObject[] cameras;

		// Hold curent active camera id
		int currentCamera = 0;

		void Start()
		{
			mainCamera = GameObject.Find("Main Camera");

			cameras[0] = mainCamera;
		}


		// Switch to next camera based total camera counts
		public void NextCamera()
		{
			if (currentCamera < cameras.Length - 1)
				currentCamera++;
			else
				currentCamera = 0;

			SelectCamera(currentCamera);
		}

		// Diactivate all cameras and activate current selected
		public void SelectCamera(int id)
		{

			for (int a = 0; a < cameras.Length; a++)
			{
				cameras[a].SetActive(false);
				cameras[a].GetComponent<AudioListener>().enabled = false;
				cameras[a].GetComponent<Camera>().enabled = false;
			}

			cameras[id].SetActive(true);
			cameras[id].GetComponent<AudioListener>().enabled = true;
			cameras[id].GetComponent<Camera>().enabled = true;

            #region Quality_Level

            UnityEngine.Rendering.PostProcessing.PostProcessLayer PostLayer
                 = cameras[id].GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>();
           
            // Very Low
            if (PlayerPrefs.GetInt("QualityLevel") == 0)
            {                
                PostLayer.enabled = true;
                PostLayer.antialiasingMode = UnityEngine.Rendering.PostProcessing.PostProcessLayer.Antialiasing.None;

                if (cameras[id].GetComponent<LightingBox.Effects.GlobalFog>())
                    cameras[id].GetComponent<LightingBox.Effects.GlobalFog>().enabled = false;

                Set_Reflection(false, false, cameras[id].GetComponent<Camera>());
            }
            //_________________________________________
            //  Low
            if (PlayerPrefs.GetInt("QualityLevel") == 1)
            {
                PostLayer.enabled = true;
                PostLayer.antialiasingMode = UnityEngine.Rendering.PostProcessing.PostProcessLayer.Antialiasing.None;

                if(cameras[id].GetComponent<LightingBox.Effects.GlobalFog>())
                    cameras[id].GetComponent<LightingBox.Effects.GlobalFog>().enabled = false;

                Set_Reflection(false, false, cameras[id].GetComponent<Camera>());
            }
            //_________________________________________
            //  Medium
            if (PlayerPrefs.GetInt("QualityLevel") == 2)
            {
                PostLayer.enabled = true;
                PostLayer.antialiasingMode = UnityEngine.Rendering.PostProcessing.
                    PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
                PostLayer.subpixelMorphologicalAntialiasing.quality = UnityEngine.Rendering.PostProcessing.SubpixelMorphologicalAntialiasing.Quality.Low;

                if (cameras[id].GetComponent<LightingBox.Effects.GlobalFog>())
                    cameras[id].GetComponent<LightingBox.Effects.GlobalFog>().enabled = true;

                Set_Reflection(false, false, cameras[id].GetComponent<Camera>());
            }
            //_________________________________________
            //  High
            if (PlayerPrefs.GetInt("QualityLevel") == 3)
            {
                PostLayer.enabled = true;
                PostLayer.antialiasingMode = UnityEngine.Rendering.PostProcessing.
                    PostProcessLayer.Antialiasing.TemporalAntialiasing;

                if (cameras[id].GetComponent<LightingBox.Effects.GlobalFog>())
                    cameras[id].GetComponent<LightingBox.Effects.GlobalFog>().enabled = true;

                Set_Reflection(true, false, cameras[id].GetComponent<Camera>());
            }
            //_________________________________________
            //  Ultra
            if (PlayerPrefs.GetInt("QualityLevel") == 4)
            {
                PostLayer.enabled = true;
                PostLayer.antialiasingMode = UnityEngine.Rendering.PostProcessing.
                    PostProcessLayer.Antialiasing.TemporalAntialiasing;

                if (cameras[id].GetComponent<LightingBox.Effects.GlobalFog>())
                    cameras[id].GetComponent<LightingBox.Effects.GlobalFog>().enabled = true;

                Set_Reflection(false, true, cameras[id].GetComponent<Camera>());
            }
            //_________________________________________
            //  Max
            if (PlayerPrefs.GetInt("QualityLevel") == 5)
            {
                PostLayer.enabled = true;
                PostLayer.antialiasingMode = UnityEngine.Rendering.PostProcessing.
                    PostProcessLayer.Antialiasing.TemporalAntialiasing;

                if (cameras[id].GetComponent<LightingBox.Effects.GlobalFog>())
                    cameras[id].GetComponent<LightingBox.Effects.GlobalFog>().enabled = true;

                Set_Reflection(false, true, cameras[id].GetComponent<Camera>());
            }
            #endregion

            #region Sun Shaft           

            if (PlayerPrefs.GetInt("Sun Shaft") == 0)
			{
                if (cameras[id].GetComponent<LightingBox.Effects.SunShafts>())
                {
                    cameras[id].GetComponent<LightingBox.Effects.SunShafts>()
                        .enabled = false;
                }
			}
			if (PlayerPrefs.GetInt("Sun Shaft") == 1)
			{
				if (FindFirstObjectByType<RainLights>())
				{
					if (cameras[id].GetComponent<LightingBox.Effects.SunShafts>())
						cameras[id].GetComponent<LightingBox.Effects.SunShafts>().enabled = false;
				}
				else
				{
					if (cameras[id].GetComponent<LightingBox.Effects.SunShafts>())
					{
						cameras[id].GetComponent<LightingBox.Effects.SunShafts>()
							.enabled = true;
					}
				}
			}

			#endregion
		}

        public void Set_Reflection(bool ssr_1, bool ssr_2,Camera targetCamera)
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
                targetCamera.renderingPath = RenderingPath.DeferredShading;

                ssr2.enabled.value = false;
                ssr1.enabled.value = true;
            }
            if (ssr_2)
            {
                targetCamera.renderingPath = RenderingPath.DeferredShading;

                ssr2.enabled.value = true;

                ssr2.resolveDownsample.value = false;
                ssr2.raycastDownsample.value = false;

                ssr1.enabled.value = false;
            }
            if (!ssr_1 && !ssr_2)
            {
                targetCamera.renderingPath = RenderingPath.Forward;

                ssr2.enabled.value = false;
                ssr1.enabled.value = false;
            }
        }
    }
}