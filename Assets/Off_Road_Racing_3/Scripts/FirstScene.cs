using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirstScene : MonoBehaviour
{
    public float percent = 0;
    private AsyncOperation async = null;
    public Image progressImage;
    //	public GUITexture footerUp;
    //	public GUITexture footerBottom;
    //	public GUITexture loadingBar;

    private void Start()
    {
        PlayerPrefs.SetInt("menuIndex", 0);
        StartCoroutine(LoadNextScene());
    }

    private IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(4f);

        // The garage reads money and unlocks in its first frame: wait for the YouTube cloud save
        // (answers within 3 s at worst, immediately off YouTube).
        while (!MicroCar.Web.CloudSave.Ready)
            yield return null;

        async = SceneManager.LoadSceneAsync(1);

        async.allowSceneActivation = true;

        // While the asynchronous operation to load the new scene is not yet complete, continue waiting until it's done.
        while (!async.isDone)
        {
            if (async.progress < 0.9f)
            {
                percent = async.progress;
            }
            else
            {
                percent = (async.progress / 0.9f);
                async.allowSceneActivation = true;
            }

            yield return null;
        }


//        yield return async;
    }

    void Update()
    {
        progressImage.fillAmount = percent;
    }
}
