//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using ALIyerEdon;

namespace ALIyerEdon
{
    public class Start_Finish_UI : MonoBehaviour
    {
        // Display race finish menu
        [Header("Menu ____________________________________________________" +
            "____________________________________________________")]
        [Space(5)]
        public GameObject finishRaceMenu;
        public GameObject raceUI;
        public GameObject startButton;
        public UnityEngine.UI.Text totalScores;

        // Award icons 
        [Header("Awards ____________________________________________________" +
            "____________________________________________________")]
        [Space(5)]
        public GameObject goldIcon;
        public GameObject bronzeIcon;
        public GameObject silverIcon;
        public GameObject noAward;

        // Give scores, by finishing place. The template named these after the medal it showed
        // (2nd place "bronze" 2000, 3rd place "silver" 1000), which put the medals the wrong way
        // round; the old names keep the values already saved in the race scenes.
        [Header("Scores ____________________________________________________" +
            "____________________________________________________")]
        [Space(5)]
        public int goldAward = 3000;
        [FormerlySerializedAs("bronzeAward")] public int secondPlaceAward = 2000;
        [FormerlySerializedAs("silverAward")] public int thirdPlaceAward = 1000;

        // Update player's position
        [Header("Positions ____________________________________________________" +
            "____________________________________________________")]
        [Space(5)] public UnityEngine.UI.Text[] positions;
        public UnityEngine.UI.Text[] driversName;

        // Update award menu from "Race_Manager" component at the race finish
        public void Update_Award(int position, int level_ID)
        {
            //  Save current level award to display in the main menu (garage scene)
            if (PlayerPrefs.GetInt("Award_Level_" + level_ID.ToString()) > position)
                PlayerPrefs.SetInt("Award_Level_" + level_ID.ToString(), position);

            // Award icons: 1st gold, 2nd silver, 3rd bronze, anything else none.
            goldIcon.SetActive(position == 0);
            silverIcon.SetActive(position == 1);
            bronzeIcon.SetActive(position == 2);
            noAward.SetActive(position > 2);

            int prize = position == 0 ? goldAward : position == 1 ? secondPlaceAward : position == 2 ? thirdPlaceAward : 0;
            if (prize > 0)
                PlayerPrefs.SetInt("TotalScores", PlayerPrefs.GetInt("TotalScores") + prize);

            // Rewarded ad: offer to pay the same prize again (MicroCar.Web.DoublePrizeOffer).
            MicroCar.Web.DoublePrizeOffer.Show(finishRaceMenu, prize, totalScores);
        }
    }


}