//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ALIyerEdon;

namespace ALIyerEdon
{
    public class SettingsMenu : MonoBehaviour
    {
        // UI items in settings menu window
        public Dropdown qualityLevel;
        public Dropdown controlType;
        public Dropdown sunShaft;
        public Dropdown wheelSmoke;
        public Dropdown displayFPS;       
        public Dropdown dynamicCamera;
        public Dropdown Balloons;

        public Slider AccelSensibility;
        public Text AccelSensibilityInfo;
        public Slider musicVolume;
        public Text musicVolumeInfo;
       
        public Toggle positionUI;
        public Toggle localPosition;

        // Start is called before the first frame update
        void Start()
        {
            // Load initilial settings            
            AccelSensibility.value = PlayerPrefs.GetFloat("accelSensibility");
            AccelSensibilityInfo.text = AccelSensibility.value.ToString();

            controlType.value = PlayerPrefs.GetInt("ControlType");
            sunShaft.value = PlayerPrefs.GetInt("Sun Shaft");
            displayFPS.value = PlayerPrefs.GetInt("Display FPS");
            dynamicCamera.value = PlayerPrefs.GetInt("Dynamic Camera");
            wheelSmoke.value = PlayerPrefs.GetInt("Wheel Smoke");
            Balloons.value = PlayerPrefs.GetInt("Balloons");
            qualityLevel.value = PlayerPrefs.GetInt("QualityLevel");

            musicVolume.value = PlayerPrefs.GetFloat("Music");

            // 3 => true  0 => false
            if (PlayerPrefs.GetInt("ShowLocalPosition") == 3)
                localPosition.isOn = true;
            else
                localPosition.isOn = false;

            if (PlayerPrefs.GetInt("ShowPositionUI") == 3)
                positionUI.isOn = true;
            else
                positionUI.isOn = false;
        }

        // Accelerometer  Sensibility
        public void Accelometer_Sensibility()
        {
            PlayerPrefs.SetFloat("accelSensibility", AccelSensibility.value);
            AccelSensibilityInfo.text = AccelSensibility.value.ToString();

            if (FindObjectOfType<InputSystem>())
                FindObjectOfType<InputSystem>().Update_InputType();
        }
        public void Music_Volume()
        {
            PlayerPrefs.SetFloat("Music", musicVolume.value);
            musicVolumeInfo.text = musicVolume.value.ToString();
            FindFirstObjectByType<Load_Settings>().Update_MusicVolume(musicVolume.value);
        }
       
        public void Set_QualityLevel()
        {
            // Very Low
            if (qualityLevel.value == 0)
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
            if (qualityLevel.value == 1)
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
            if (qualityLevel.value == 2)
            {
                Screen.SetResolution((int)(PlayerPrefs.GetInt("OriginalX") * 0.5f),
                    (int)(PlayerPrefs.GetInt("OriginalY") * 0.5f), true);

                foreach (UnityEngine.Rendering.PostProcessing.PostProcessLayer lll in
                    FindObjectsOfType<UnityEngine.Rendering.PostProcessing.PostProcessLayer>())
                {
                    lll.enabled = true;
                    lll.antialiasingMode = UnityEngine.Rendering.PostProcessing.
                        PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
                    lll.subpixelMorphologicalAntialiasing.quality = 
                        UnityEngine.Rendering.PostProcessing.SubpixelMorphologicalAntialiasing.Quality.Low;
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
            if (qualityLevel.value == 3)
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
                Set_Reflection(true,false);
            }
            //_________________________________________
            //  Ultra
            if (qualityLevel.value == 4)
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
            //  Max
            if (qualityLevel.value == 5)
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

        int ReflectionState;
        // Screen space reflections
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
        public void Toggle_LocalPosition()
        {
            StartCoroutine(Toggle_LocalPosition_Save());
        }

        IEnumerator Toggle_LocalPosition_Save()
        {
            yield return new WaitForEndOfFrame();

            if (localPosition.isOn)
                PlayerPrefs.SetInt("ShowLocalPosition", 3);
            else
                PlayerPrefs.SetInt("ShowLocalPosition", 0);

            foreach(Car_Position pos in FindObjectsOfType<Car_Position>())
            {
                if (PlayerPrefs.GetInt("ShowLocalPosition") == 3)
                    pos.displayPosition = true;
                else
                    pos.displayPosition = false;
            }
        }

        // Racing Position UI Display
        public void Toggle_PositionUI()
        {
            StartCoroutine(Toggle_PositionUI_Save());
        }

        IEnumerator Toggle_PositionUI_Save()
        {
            yield return new WaitForEndOfFrame();

            if (positionUI.isOn)
                PlayerPrefs.SetInt("ShowPositionUI", 3);
            else
                PlayerPrefs.SetInt("ShowPositionUI", 0);

            if (FindFirstObjectByType<Race_Manager>())
            {
                if (PlayerPrefs.GetInt("ShowPositionUI") == 3)
                    FindFirstObjectByType<Race_Manager>().positionUI.SetActive(true);
                else
                    FindFirstObjectByType<Race_Manager>().positionUI.SetActive(false);
            }
        }

        // Control type : accelerometer , steering wheel , arrow keys
        public void Set_ControlType()
        {
            PlayerPrefs.SetInt("ControlType", controlType.value);

            if (FindObjectOfType<InputSystem>())
                FindObjectOfType<InputSystem>().Update_InputType();
        }
        public void Set_SunShaft()
        {
            PlayerPrefs.SetInt("Sun Shaft", sunShaft.value);

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
        }
        public void Set_WheelSmoke()
        {
            PlayerPrefs.SetInt("Wheel Smoke", wheelSmoke.value);

            if (FindObjectOfType<Load_Settings>())
                    FindObjectOfType<Load_Settings>().Update_SmokeEffect();

            foreach (EasyCarController car in FindObjectsOfType<EasyCarController>())
            {
                if (PlayerPrefs.GetInt("Wheel Smoke") == 0)
                    car.enableWheelEffects = false;
                if (PlayerPrefs.GetInt("Wheel Smoke") == 1)
                    car.enableWheelEffects = true;
            }
        }
        public void Set_Balloons()
        {
            PlayerPrefs.SetInt("Balloons", Balloons.value);

            if (FindObjectOfType<Load_Settings>())
                FindObjectOfType<Load_Settings>().Update_Balloons();
        }
        public void Set_DisplayFPS()
        {
            PlayerPrefs.SetInt("Display FPS", displayFPS.value);

            if (FindObjectOfType<FPSCounter>())
            {
                if (PlayerPrefs.GetInt("Display FPS") == 1)
                {
                    FindObjectOfType<FPSCounter>().panel.SetActive(true);
                }
                else
                {
                    FindObjectOfType<FPSCounter>().panel.SetActive(false);
                }
            }
        }
        public void Set_DynamicCamera()
        {
            PlayerPrefs.SetInt("Dynamic Camera", dynamicCamera.value);
        }

        // Save reflection values after close the settings menu to avoid crash on the low-end devices
        public void Close_Save()
        {
            PlayerPrefs.SetInt("QualityLevel", qualityLevel.value);
        }
    }
}