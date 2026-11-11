using UnityEngine;

namespace MicroCar.UI
{
    // The Telemetry screens live one per file (Unity needs a MonoBehaviour's file to carry its name):
    // GarageView, TracksView, SettingsView (garage), HudView, ResultsView, PauseView, TeleTouch (race),
    // TeleSegment. The game logic stays in the template's scripts (CarSelect, LevelSelect, SettingsMenu,
    // Race_Manager, Start_Finish_UI, Pause_Menu, InputSystem, Nitro); TeleBuilder points their UI fields
    // at the new elements, and the views add what the template never showed (car names, the race clock,
    // the player's row) and the keyboard shortcuts.

    /// <summary>Keyboard shortcuts, only where key hints are shown (desktop, landscape).</summary>
    static class Keys
    {
        public static bool Down(KeyCode key) => TeleScreen.ShowKeys && Input.GetKeyDown(key);
    }

    /// <summary>Race time, from the green light to the finish (game time: it stops in the pause).</summary>
    public static class RaceClock
    {
        public static float Seconds;
    }
}
