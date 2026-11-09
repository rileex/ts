using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine.Events;
using System;

public class UnityAdsScript : MonoBehaviour
{
    public string bannerPlacementId = "banner";
    public string rewardVideoPlacementId = "rewardedVideo";

    public string[] AdmobBanner;
    public string[] AdmobInterstitial;
    public string[] AdmobRewarded;



    public bool testMode = true;
    public int[] ScenesShowBanner;
    bool isAdsReady = false;
    public int loadedLevel;
    public int isShowAds = 1;
    public GameObject NoAdsButton;
    public GameObject purchaseDialog;
    public string[] purchaseSuccess;
    public string[] purchaseFailed;
    public int language = 0;
    public int rewardedType = 0;
    //    public garageScript mainObject;
    //    public moneyShopScript moneyShop;

    public InterstitialAd interstitial;
    public RewardedAd rewardedAd;

    public int rewardedAdIndex = 0;
    public int interstitialIndex = 0;

    // Initialize the Ads listener and service:
    void Start()
    {
        //        PlayerPrefs.SetInt("isShowAds", 1);
        //        Advertisement.AddListener(this);
        //        Advertisement.Initialize(gameId, testMode);
        MobileAds.Initialize(initStatus => { });
        //        StartCoroutine(ShowBannerWhenReady());
        loadedLevel = Application.loadedLevel;

        //       language = GameObject.Find("GameData").GetComponent<GameDataBase>().GetLanguage();
        //        mainObject = GameObject.Find("mainSceneObject").GetComponent<mainSceneScript>();
        //       moneyShop = GameObject.Find("MoneyShop").GetComponent<moneyShopScript>();
        //        RequestInterstitial();
        LoadRewardedAd();
        LoadInterstitial();

    }



    private void OnLevelWasLoaded(int level)
    {

        if (Application.loadedLevel == 1)
        {
            //            moneyShop = GameObject.Find("MoneyShop").GetComponent<moneyShopScript>();
            /*            NoAdsButton = GameObject.Find("RemoveAds");
                        if (!PlayerPrefs.HasKey("isShowAds"))
                        {
                            PlayerPrefs.SetInt("isShowAds", 1);
                        }
                        isShowAds = PlayerPrefs.GetInt("isShowAds");
                        if (isShowAds == 0)
                            NoAdsButton.SetActive(false);
                        purchaseDialog = GameObject.Find("PurchaseDialog");
                        purchaseDialog.SetActive(false);*/
            //           mainObject = GameObject.Find("MainObject").GetComponent<garageScript>();
        }
        if (Application.loadedLevel > 0)
        {
            ShowInterstitial();
            LoadRewardedAd();
        }
    }
    public void OnPurchaseSuccess(bool isShowAds)
    {
        purchaseDialog.SetActive(true);
        purchaseDialog.GetComponentInChildren<Text>().text = purchaseSuccess[language];
        if (!isShowAds)
        {
            PlayerPrefs.SetInt("isShowAds", 0);
            NoAdsButton.SetActive(false);
        }
    }

    public void OnPurchaseFailed()
    {
        purchaseDialog.SetActive(true);
        //        isShowAds = 1;
        purchaseDialog.GetComponentInChildren<Text>().text = purchaseFailed[language];
    }

    public void OnPurchaseDialogClick()
    {
        purchaseDialog.SetActive(false);
        if (isShowAds == 0)
            NoAdsButton.SetActive(false);
    }

    /*   void OnLevelWasLoaded(int level)
       {
           loadedLevel = level;
           if (isShowAds==1)
               StartCoroutine(ShowBannerWhenReady());

       }*/

    IEnumerator ShowBannerWhenReady()
    {
        //        while (!Advertisement.IsReady(bannerPlacementId))
        //        {
        yield return new WaitForSeconds(0.5f);
        /*        }
                if (ScenesShowBanner.Length > 0)
                {
                    for (int i = 0; i < ScenesShowBanner.Length; i++)
                    {
                        if (loadedLevel == ScenesShowBanner[i])
                        {
                            Advertisement.Banner.SetPosition(BannerPosition.TOP_CENTER);
                            Advertisement.Banner.Show(bannerPlacementId);
                        }
                    }

                }*/

    }

    // Implement IUnityAdsListener interface methods:
    /*   public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
       {
           // Define conditional logic for each ad completion status:
           if (showResult == ShowResult.Finished)
           {
               // Reward the user for watching the ad to completion.
               if (rewardedType == 0)
               {
                   moneyShop.SetVideoReward();
               }
               if (rewardedType == 2)
               {
   //                mainObject.GasStationWatchAds();
               }
           }
           else if (showResult == ShowResult.Skipped)
           {
               // Do not reward the user for skipping the ad.
           }
           else if (showResult == ShowResult.Failed)
           {
               Debug.LogWarning("The ad did not finish due to an error.");
           }
       }*/

    public void OnUnityAdsReady(string placementId)
    {
        // If the ready Placement is rewarded, show the ad:
        if (placementId == rewardVideoPlacementId)
        {
            isAdsReady = true;
        }
    }

    public void OnUnityAdsDidError(string message)
    {
        // Log the error.
    }

    public void OnUnityAdsDidStart(string placementId)
    {
        // Optional actions to take when the end-users triggers an ad.
    }

    public void ShowRewardedVideo(int type)
    {
        //        if (isAdsReady && isShowAds == 1)
        //        {
        //            Advertisement.Show(rewardVideoPlacementId);
        rewardedType = type;
        RequestRewarded();
        //        }
    }

    public void ShowInterstitial()
    {
        if (isShowAds == 1)
        {
            if (this.interstitial.IsLoaded())
            {
                this.interstitial.Show();
            }
        }
    }

    // Use this for initialization

    void Awake()
    {

        DontDestroyOnLoad(this.gameObject);
    }


    //   InterstitialAd interstitial;

    public void LoadInterstitial()
    {
        interstitial = new InterstitialAd(AdmobInterstitial[interstitialIndex]);
        // Initialize an InterstitialAd.
        AdRequest request = new AdRequest.Builder().Build();
        // Called when an ad request has successfully loaded.
        interstitial.OnAdLoaded += HandleOnAdLoaded;
        // Called when an ad request failed to load.
        interstitial.OnAdFailedToLoad += HandleOnAdFailedToLoad;
        // Called when an ad is shown.
        interstitial.OnAdOpening += HandleOnAdOpened;
        // Called when the ad is closed.
        interstitial.OnAdClosed += HandleOnAdClosed;
        // Called when the ad click caused the user to leave the application.
        //        this.interstitial.OnAdLeavingApplication += HandleOnAdLeavingApplication;
        interstitial.LoadAd(request);

    }

    public void LoadRewardedAd()
    {
        rewardedAd = new RewardedAd(AdmobRewarded[rewardedAdIndex]);
        // Called when an ad request has successfully loaded.
        rewardedAd.OnAdLoaded += HandleRewardedAdLoaded;
        // Called when an ad request failed to load.
        rewardedAd.OnAdFailedToLoad += HandleRewardedAdFailedToLoad;
        // Called when an ad is shown.
        rewardedAd.OnAdOpening += HandleRewardedAdOpening;
        // Called when an ad request failed to show.
        rewardedAd.OnAdFailedToShow += HandleRewardedAdFailedToShow;
        // Called when the user should be rewarded for interacting with the ad.
        rewardedAd.OnUserEarnedReward += HandleUserEarnedReward;
        // Called when the ad is closed.
        rewardedAd.OnAdClosed += HandleRewardedAdClosed;
        AdRequest request = new AdRequest.Builder().Build();
        rewardedAd.LoadAd(request);
    }

    public void HandleOnAdLoaded(object sender, EventArgs args)
    {
        MonoBehaviour.print("HandleAdLoaded event received");
    }


    public void HandleOnAdFailedToLoad(object sender, AdFailedToLoadEventArgs args)
    {

        if (interstitialIndex < AdmobInterstitial.Length - 1)
        {
            interstitialIndex++;
            LoadInterstitial();
        }
        else
        {
            interstitialIndex = 0;
            //            LoadRewardedAd();
        }
    }

    public void HandleOnAdOpened(object sender, EventArgs args)
    {
        interstitialIndex = 0;
    }

    public void HandleOnAdClosed(object sender, EventArgs args)
    {
        LoadInterstitial();
    }

    public void HandleOnAdLeavingApplication(object sender, EventArgs args)
    {
        MonoBehaviour.print("HandleAdLeavingApplication event received");
    }

    public void RequestRewarded()
    {
        if (rewardedAd.IsLoaded())
        {
            rewardedAd.Show();
        }
    }



    public void HandleRewardedAdLoaded(object sender, EventArgs args)
    {
        rewardedAdIndex = 0;
    }

    public void HandleRewardedAdFailedToLoad(object sender, AdFailedToLoadEventArgs args)
    {
        if (rewardedAdIndex < AdmobRewarded.Length - 1)
        {
            rewardedAdIndex++;
            LoadRewardedAd();
        }
        else
        {
            rewardedAdIndex = 0;
            //            LoadRewardedAd();
        }
    }

    public void HandleRewardedAdOpening(object sender, EventArgs args)
    {
        MonoBehaviour.print("HandleRewardedAdOpening event received");
    }

    public void HandleRewardedAdFailedToShow(object sender, AdErrorEventArgs args)
    {
        MonoBehaviour.print(
            "HandleRewardedAdFailedToShow event received with message: "
                             + args.Message);
    }

    public void HandleRewardedAdClosed(object sender, EventArgs args)
    {
        LoadRewardedAd();
    }

    public void HandleUserEarnedReward(object sender, Reward args)
    {
        if (rewardedType == 0)
        {
 //           CCDS_UI_MainMenuManager.Instance.OnAdsSelectVehicle();
 //           FindObjectOfType<CCDS_UI_MainMenuManager>().OnAdsSelectVehicle();
        }
        if (rewardedType == 1)
        {
 //           FindObjectOfType<OffroadGameScript>().AddTime();
        }
//        FindObjectOfType<changeCarScript>().Next();
        //       PlayerPrefs.SetInt("playerMoney", PlayerPrefs.GetInt("playerMoney") + rewardedType);
        //       mainObject.playerMoney = PlayerPrefs.GetInt("playerMoney");
        //       Debug.Log(rewardedAdIndex);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
