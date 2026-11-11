using ALIyerEdon;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MicroCar.UI
{
    /// <summary>One choice out of a row of buttons (OFF / ON, BUTTONS / STICK / TILT).</summary>
    public sealed class TeleSegment : MonoBehaviour
    {
        public Button[] options;
        public Image[] backs;
        public Text[] labels;
        public System.Action<int> picked;

        void Awake()
        {
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                options[i].onClick.AddListener(() => picked?.Invoke(index));
            }
        }

        public void Show(int selected)
        {
            for (int i = 0; i < options.Length; i++)
            {
                bool on = i == selected;
                if (backs != null && i < backs.Length && backs[i]) backs[i].color = on ? Tele.Lime : Color.clear;
                if (labels != null && i < labels.Length && labels[i]) labels[i].color = on ? Tele.OnLime : Tele.Muted;
            }
        }
    }
}
