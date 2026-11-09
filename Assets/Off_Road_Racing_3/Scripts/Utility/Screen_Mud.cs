using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ALIyerEdon
{
    public class Screen_Mud : MonoBehaviour
    {
        Image mudImage;
        Color mudColor;

        public float minAlpha;
        public float maxAlpha;

        bool mudOn;

        void Start()
        {
            mudImage = GetComponent<Image>();
            mudColor = mudImage.color;
            mudImage.color =
                    new Color(mudColor.r, mudColor.g, mudColor.b,
                    Mathf.Lerp(mudImage.color.a, minAlpha, Time.deltaTime * 100));
        }

        void Update()
        {
            if (mudOn)
            {
                mudImage.color =
                    new Color(mudColor.r, mudColor.g, mudColor.b,
                    Mathf.Lerp(mudImage.color.a, maxAlpha, Time.deltaTime*15));
            }
            else
            {
                mudImage.color =
                    new Color(mudColor.r, mudColor.g, mudColor.b,
                    Mathf.Lerp(mudImage.color.a, minAlpha, Time.deltaTime));
            }
        }

        public void Apply_Mud(float delay)
        {
            StartCoroutine(Mud_Duration(delay));
        }

        IEnumerator Mud_Duration(float delay)
        {
            
            mudOn = true;
            yield return new WaitForSeconds(delay); 
            
            yield return new WaitForSeconds(3f);
            mudOn = false;
        }
    }
}