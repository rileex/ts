using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace MicroCar.UI
{
    /// <summary>
    /// Per-layout placement and visibility of one element, and optionally its text size. A preset
    /// left unset falls back to <see cref="pc"/> (and <see cref="land"/> to <see cref="pc"/>).
    /// Applied by the <see cref="TeleCanvas"/> above it whenever the screen changes shape.
    /// </summary>
    public sealed class TeleLayout : MonoBehaviour
    {
        public TeleSpot pc, land, port;
        [Tooltip("Text size per preset (pc, land, port); 0 leaves it.")]
        public Vector3 fontSize;
        [Tooltip("Uniform scale per preset (pc, land, port); 0 leaves it.")]
        public Vector3 scale;
        [Tooltip("Only with touch controls (phones, tablets).")]
        public bool touchOnly;
        [Tooltip("Only with a keyboard (desktop).")]
        public bool keysOnly;

        /// <summary>Set when this layout (not a game script) switched the object off; saved, so a
        /// scene built in the editor in the desktop layout still shows the element on a phone.</summary>
        [HideInInspector] public bool hiddenByLayout;

        public void Apply(TelePreset preset)
        {
            var spot = preset == TelePreset.Port && port.set ? port
                : preset == TelePreset.Land && land.set ? land
                : pc;

            bool touch = TeleScreen.Touch;
            if ((spot.set && spot.hide) || (touchOnly && !touch) || (keysOnly && touch))
            {
                if (gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                    hiddenByLayout = true;
                }
                return;
            }

            if (hiddenByLayout)
            {
                hiddenByLayout = false;
                gameObject.SetActive(true);
            }

            if (spot.set && transform is RectTransform rt)
                spot.ApplyTo(rt);

            float size = preset == TelePreset.Port ? fontSize.z : preset == TelePreset.Land ? fontSize.y : fontSize.x;
            if (size > 0f && TryGetComponent<Text>(out var text))
                text.fontSize = Mathf.RoundToInt(size);

            float s = preset == TelePreset.Port ? scale.z : preset == TelePreset.Land ? scale.y : scale.x;
            if (s > 0f)
                transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
