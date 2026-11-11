//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using UnityEngine;
using System.Collections;
using ALIyerEdon;

namespace ALIyerEdon
{
	public class InputSystem : MonoBehaviour
	{

		[HideInInspector] public bool canControl = false;

		[Tooltip("Automatically switch between keyboard and mobile controls based on the running platform")]
		public bool autoSwitchPlatform = true;
		// Select control type => Touch or keyboard
		[Tooltip("Keyboard is for desktop and Mobile is for touch platforms")]
		public InputType controlType;


		EasyCarController controller;


		float motorInput, steerInput;
		bool handBrake;

		[Header("Components")]
		ALIyerEdon.Joystick vJoystick;
		bool joystickControl;

		public GameObject joystick;
		public GameObject arrowKeys;

		// Accelerometer controlling
		[Header("Accelerometer")]
		public float accelSensibility = 10f;
		public float accelSmooth = 0.5f;
		Vector3 curAc;
		bool accelInput;

		[Header("Keyboard Keys")]
		public KeyCode CameraSwitch = KeyCode.C;
		public KeyCode HandBrake = KeyCode.Space;
		public KeyCode Nitro = KeyCode.LeftShift;
		public KeyCode Pause = KeyCode.Escape;

		IEnumerator Start()
		{
			if (PlayerPrefs.GetFloat("accelSensibility") == 0)
				PlayerPrefs.SetFloat("accelSensibility", 10f);


			vJoystick = joystick.GetComponent<ALIyerEdon.Joystick>();

			if (autoSwitchPlatform)
			{
#if UNITY_WEBGL && !UNITY_EDITOR
				// Browser: keyboard on desktop, on-screen touch controls on phones and tablets.
				controlType = Application.isMobilePlatform ? InputType.Mobile : InputType.Keyboard;
#elif UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA || UNITY_64
				// In the editor, MicroCar/UI/Simulate Touch tries the phone UI and touch controls.
				controlType = MicroCar.UI.TeleScreen.Touch ? InputType.Mobile : InputType.Keyboard;
#else
						controlType = InputType.Mobile;			
#endif
			}

			Update_InputType();

			yield return new WaitForEndOfFrame();

			controller = GameObject.FindGameObjectWithTag("Player")
				.GetComponent<EasyCarController>();

			GameObject.FindGameObjectWithTag("Player")
				.GetComponent<Car_AI>().enabled = false;
		}

		public void Update_InputType()
        {
			// Arrow keys
			if (PlayerPrefs.GetInt("ControlType") == 0)
			{
				// Controls
				joystick.SetActive(false);
				arrowKeys.SetActive(true);
				// Drive Mode
				joystickControl = false;
				accelInput = false;
			}
			// Joystick
			if (PlayerPrefs.GetInt("ControlType") == 1)
			{
				// Controls
				joystick.SetActive(true);
				arrowKeys.SetActive(false);
				// Drive Mode
				joystickControl = true;
				accelInput = false;
			}
			// Accelerometer
			if (PlayerPrefs.GetInt("ControlType") == 2)
			{
				// Controls
				joystick.SetActive(false);
				arrowKeys.SetActive(false);
				// Drive Mode
				joystickControl = false;
				accelInput = true;
			}

			accelSensibility = PlayerPrefs.GetFloat("accelSensibility");
		}

		void Update()
		{

			if (!controller || !canControl)
				return;

			if (accelInput)
			{
				// Controll steering (mobile)	
				// 		
				if (Input.acceleration.x > 0.2f || Input.acceleration.x < -0.2f)
				{
					steerInput = Input.acceleration.x * Time.deltaTime * accelSensibility;
				}
				else
				{
					steerInput = 0;
				}

			}

			if (joystickControl)
				steerInput = vJoystick.GetHorizontal(0) * Time.deltaTime * 23;


			if (controlType == InputType.Keyboard)
			{

				motorInput = Input.GetAxis("Vertical");

				steerInput = Input.GetAxis("Horizontal");

				if (Input.GetKey(HandBrake))
					handBrake = true;
				else
					handBrake = false;


				if (Input.GetKeyDown(CameraSwitch))
					FindFirstObjectByType<CameraSwitch>().NextCamera();

				if (Input.GetKeyDown(Pause))
				{
					// Esc pauses, and resumes again from the pause menu.
					var pauseMenu = FindFirstObjectByType<Pause_Menu>();
					if (pauseMenu.pauseMenu.activeSelf)
						pauseMenu.Resume();
					else
						pauseMenu.Pause();
				}
			}

			controller.Move(motorInput, steerInput, handBrake);

		}

		public void Throttle()
		{
			if (controlType == InputType.Mobile)
				motorInput = 1f;
		}

		public void ThrottleRelease()
		{
			if (controlType == InputType.Mobile)
				motorInput = 0;
		}

		public void Steer(bool state)
		{
			if (controlType == InputType.Mobile)
			{
				if (state)
					steerInput = Mathf.Lerp(steerInput, 1f, Time.deltaTime * 25);
				else
					steerInput = Mathf.Lerp(steerInput, -1f, Time.deltaTime * 25);
			}
		}

		public void SteerRelease()
		{
			if (controlType == InputType.Mobile)
				steerInput = 0;

		}

		public void Brake(bool state)
		{
			if (controlType == InputType.Mobile)
			{
				if (state)
					motorInput = -1f;
				else
					motorInput = 0;
			}
		}

		public void Hand_Brake(bool state)
		{
			handBrake = state;
		}

		public void LoadLevel(string name)
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene(name);
		}

		public void Pause_Game()
		{
			FindFirstObjectByType<Pause_Menu>().Pause();
		}

		public void Switch_Camera()
		{
			FindFirstObjectByType<CameraSwitch>().NextCamera();
		}
	}
}