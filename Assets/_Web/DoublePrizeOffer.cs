using UnityEngine;
using UnityEngine.UI;

namespace MicroCar.Web
{
    /// <summary>
    /// The rewarded-ad placement: "watch an ad to double the prize" on the race finish screen.
    ///
    /// <para>
    /// The finish screen (Telemetry UI) has a button named "DoublePrize" among its actions, hidden
    /// until this shows it. Shown only when the race paid a prize, once per race, and the prize is
    /// added again only when the platform reports the ad was actually watched.
    /// </para>
    /// </summary>
    public static class DoublePrizeOffer
    {
        const string ButtonName = "DoublePrize";

        /// <param name="finishMenu">The finish menu root (Start_Finish_UI.finishRaceMenu).</param>
        /// <param name="prize">What the race just paid; the reward pays it again.</param>
        /// <param name="totalText">The credits label to refresh, may be null.</param>
        public static void Show(GameObject finishMenu, int prize, Text totalText)
        {
            if (finishMenu == null)
                return;

            var found = Find(finishMenu.transform, ButtonName);
            if (found == null)
                return;
            var go = found.gameObject;
            var button = go.GetComponent<Button>();
            if (button == null)
                return;

            go.SetActive(prize > 0);
            if (prize <= 0)
                return;

            var amount = found.Find("Amount");
            if (amount != null && amount.TryGetComponent<Text>(out var amountText))
                amountText.text = "+" + MicroCar.UI.Tele.Credits(prize);

            button.interactable = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                button.interactable = false;
                AdService.ShowRewarded(AdService.RewardDoublePrize, earned =>
                {
                    if (go == null)
                        return;
                    if (!earned)
                    {
                        button.interactable = true;
                        return;
                    }
                    PlayerPrefs.SetInt("TotalScores", PlayerPrefs.GetInt("TotalScores") + prize);
                    if (totalText != null)
                        totalText.text = MicroCar.UI.Tele.Credits(PlayerPrefs.GetInt("TotalScores"));
                    CloudSave.Push();
                    go.SetActive(false);
                });
            });
        }

        static Transform Find(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name) return child;
                var deeper = Find(child, name);
                if (deeper != null) return deeper;
            }
            return null;
        }
    }
}
