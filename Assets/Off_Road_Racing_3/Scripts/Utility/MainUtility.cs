//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using ALIyerEdon;


namespace ALIyerEdon
{
	public enum Quality_Level
    {
		VeryLow,Low,Medium,High,Ultra
    }
	public class MainUtility : MonoBehaviour
	{
		public int targetFPS = 60;
		public Quality_Level defaultQualityLevel = Quality_Level.Medium;

		// Instantiate car at start
		public CarSelect carSelect;

		public AudioSource clickSound;

		public GameObject Loading, exitMenu;

		public int startingScore = 1400;

		void Awake()
		{
			AudioListener.volume = 1f;
			
			Time.timeScale = 1f;

			PlayerPrefs.SetInt("Target FPS", targetFPS);

			Application.targetFrameRate = targetFPS;

			// Is game first run?   3 & 1 => true    0 => false
			if (PlayerPrefs.GetInt("FirstRun") != 3)
			{
				PlayerPrefs.SetInt("OriginalX", Screen.width);
				PlayerPrefs.SetInt("OriginalY", Screen.height);

				// Set default qualitylevel
				if(defaultQualityLevel == Quality_Level.VeryLow)
					PlayerPrefs.SetInt("QualityLevel", 0);
				if (defaultQualityLevel == Quality_Level.Low)
					PlayerPrefs.SetInt("QualityLevel", 1);
				if (defaultQualityLevel == Quality_Level.Medium)
					PlayerPrefs.SetInt("QualityLevel", 2);
				if (defaultQualityLevel == Quality_Level.High)
					PlayerPrefs.SetInt("QualityLevel", 3);
				if (defaultQualityLevel == Quality_Level.Ultra)
					PlayerPrefs.SetInt("QualityLevel", 4);

				// Default difficulty level => 0 = simulation , 1 = Arcade
				PlayerPrefs.SetInt("Difficulty Level", 0);

				// Set default control type to the arrow keys
				//Arrow keys = 0 , Joystick = 1 , acceleration = 2
				PlayerPrefs.SetInt("ControlType", 0);

				PlayerPrefs.SetFloat("accelSensibility", 100f);

				PlayerPrefs.SetFloat("SteeringWheelSens", 250f);

				// Set starting color for the first car (car0)
				PlayerPrefs.SetInt("CarColor0", 0);

				// Set music ambient sound in settings false
				PlayerPrefs.SetFloat("Music", 0.7f);

				// Enable right position ui info display
				PlayerPrefs.SetInt("ShowPositionUI", 3);

				// Open the first car (car0)
				PlayerPrefs.SetInt("Car0", 3);

				// Open the first level (level0)
				PlayerPrefs.SetInt("Level0", 3);

				// Set none  awards for all levels
				PlayerPrefs.SetInt("Award_Level_0", 3);
				PlayerPrefs.SetInt("Award_Level_1", 3);
				PlayerPrefs.SetInt("Award_Level_2", 3);
				PlayerPrefs.SetInt("Award_Level_3", 3);

				// Player first time starting the game score
				PlayerPrefs.SetInt("TotalScores", startingScore);

				// Set default settings for graphics
				PlayerPrefs.SetInt("Display FPS", 0);
				PlayerPrefs.SetInt("Sun Shaft", 0);
				PlayerPrefs.SetInt("Dynamic Camera", 1);
				PlayerPrefs.SetInt("Wheel Smoke", 1);
				PlayerPrefs.SetInt("Balloons", 1);

				// Disable the first running the game settings
				PlayerPrefs.SetInt("FirstRun", 3);
			}
		}

		void Update()
		{
			// Exit with back button
			if (Input.GetKeyDown(KeyCode.Escape))
				exitMenu.SetActive(!exitMenu.activeSelf);

			if (Input.GetKeyDown(KeyCode.H))
			{
				PlayerPrefs.DeleteAll();
				Debug.Log("PlayerPrefs.DeleteAll ();");
			}
			if (Input.GetKeyDown(KeyCode.E))
				PlayerPrefs.SetInt("Coins", PlayerPrefs.GetInt("Coins") + 14000);
		}

		public void Exit()
		{
			Application.Quit();
		}

		public void SetTrue(GameObject target)
		{
			target.SetActive(true);
		}

		public void SetFalse(GameObject target)
		{
			target.SetActive(false);
		}

		public void ToggleObject(GameObject target)
		{
			target.SetActive(!target.activeSelf);
		}

		public void LoadLevel(string name)
		{

			Loading.SetActive(true);
			SceneManager.LoadSceneAsync(name);
		}

		public void OpenURL(string val)
		{
			Application.OpenURL(val);
		}

		public void Click_Sound()
		{
			if (clickSound)
				clickSound.PlayOneShot(clickSound.clip);
		}
	}
}