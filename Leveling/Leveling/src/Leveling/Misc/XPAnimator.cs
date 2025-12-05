using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Leveling.Misc
{
    public class XPAnimator : MonoBehaviour
    {
        public TextMeshProUGUI text;
        public bool isLevelUp = false;
        public float fadeInTime = 0.3f;
        public float stayTime = 2f;
        public float floatUpTime = 1f;
        public float floatDistance = 50f;

        private void Start()
        {
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            Color c = text.color;
            c.a = 0f;
            text.color = c;

            RectTransform rt = text.rectTransform;
            Vector2 startPos = rt.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(0, floatDistance);

            float t = 0;
            while (t < fadeInTime)
            {
                t += Time.deltaTime;
                float a = t / fadeInTime;
                c.a = a;
                text.color = c;
                yield return null;
            }

            yield return new WaitForSeconds(stayTime);

            t = 0;
            while (t < floatUpTime)
            {

                t += Time.deltaTime;
                float a = 1f - t / floatUpTime;
                c.a = a;
                text.color = c;

                if (!isLevelUp)
                {
                    rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t / floatUpTime);
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }

}
