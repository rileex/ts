using UnityEngine;
using UnityEngine.UI;

namespace MicroCar.Web
{
    /// <summary>
    /// The rewarded-ad placement: "watch an ad to double the prize" on the race finish screen.
    ///
    /// <para>
    /// Built at run time as a copy of the finish screen's Restart button, stacked right above it, so
    /// the scene file does not change. Not mirrored to the left: that is where the race scene's Exit
    /// button sits after the finish, and covering it left the player no way back to the garage.
    /// The right-hand column above Restart is clear of the racer list's text. Shown only when the race paid a prize, once per race, and the
    /// prize is added again only when the platform reports the ad was actually watched.
    /// </para>
    /// </summary>
    public static class DoublePrizeOffer
    {
        const string ButtonName = "DoublePrize";

        /// <param name="finishMenu">The finish menu root (Start_Finish_UI.finishRaceMenu).</param>
        /// <param name="prize">What the race just paid; the reward pays it again.</param>
        /// <param name="totalText">The "Total Scores : N" label to refresh, may be null.</param>
        public static void Show(GameObject finishMenu, int prize, Text totalText)
        {
            if (finishMenu == null || prize <= 0)
                return;

            var old = finishMenu.transform.Find(ButtonName);
            if (old != null)
                Object.Destroy(old.gameObject);

            var restart = finishMenu.transform.Find("Restart");
            if (restart == null || restart.GetComponent<Button>() == null)
                return;

            var copy = Object.Instantiate(restart.gameObject, restart.parent);
            copy.name = ButtonName;

            var rt = (RectTransform)copy.transform;
            var src = (RectTransform)restart;
            rt.anchoredPosition = src.anchoredPosition + new Vector2(0f, src.rect.height + 12f);
            rt.sizeDelta = src.sizeDelta;

            var label = copy.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = "AD: x2\n+" + prize;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 14;
                label.resizeTextMaxSize = label.fontSize;
            }

            var button = copy.GetComponent<Button>();
            // A fresh event: Instantiate copied Restart's persistent listener, which must not fire here.
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() =>
            {
                button.interactable = false;
                AdService.ShowRewarded(AdService.RewardDoublePrize, earned =>
                {
                    if (copy == null)
                        return;

                    if (!earned)
                    {
                        button.interactable = true;
                        return;
                    }

                    PlayerPrefs.SetInt("TotalScores", PlayerPrefs.GetInt("TotalScores") + prize);
                    if (totalText != null)
                        totalText.text = "Total Scores : " + PlayerPrefs.GetInt("TotalScores");
                    CloudSave.Push();
                    Object.Destroy(copy);
                });
            });
        }
    }
}
