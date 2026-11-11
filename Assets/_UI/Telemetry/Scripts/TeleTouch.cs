using ALIyerEdon;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MicroCar.UI
{
    /// <summary>
    /// A held touch control. Steering is fed every frame while held (the template's arrows nudged it
    /// once per press and never reached full lock).
    /// </summary>
    public sealed class TeleTouch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public enum Kind { Gas, Brake, Left, Right, Handbrake, Nitro }

        public Kind kind;
        InputSystem input;
        Nitro nitro;
        bool held;

        void Awake()
        {
            input = FindFirstObjectByType<InputSystem>(FindObjectsInactive.Include);
            nitro = FindFirstObjectByType<Nitro>(FindObjectsInactive.Include);
        }

        public void OnPointerDown(PointerEventData e)
        {
            held = true;
            transform.localScale = Vector3.one * 0.92f;
            if (input == null) return;
            switch (kind)
            {
                case Kind.Gas: input.Throttle(); break;
                case Kind.Brake: input.Brake(true); break;
                case Kind.Left: input.Steer(false); break;
                case Kind.Right: input.Steer(true); break;
                case Kind.Handbrake: input.Hand_Brake(true); break;
                case Kind.Nitro: if (nitro) nitro.Apply_Nitro(); break;
            }
        }

        public void OnPointerUp(PointerEventData e) => Release();

        void Release()
        {
            if (!held) return;
            held = false;
            transform.localScale = Vector3.one;
            if (input == null) return;
            switch (kind)
            {
                case Kind.Gas: input.ThrottleRelease(); break;
                case Kind.Brake: input.Brake(false); break;
                case Kind.Left:
                case Kind.Right: input.SteerRelease(); break;
                case Kind.Handbrake: input.Hand_Brake(false); break;
                case Kind.Nitro: if (nitro) nitro.Release_Nitro(); break;
            }
        }

        void Update()
        {
            if (held && input != null && (kind == Kind.Left || kind == Kind.Right))
                input.Steer(kind == Kind.Right);
        }

        void OnDisable() => Release();
    }
}
