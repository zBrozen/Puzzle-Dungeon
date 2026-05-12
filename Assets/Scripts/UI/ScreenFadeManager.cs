using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace PuzzleDungeon.UI
{
    public class ScreenFadeManager : MonoBehaviour
    {
        public static ScreenFadeManager Instance { get; private set; }

        [SerializeField] private CanvasGroup _fadeCanvasGroup;
        [SerializeField] private Image _fadeImage;
        [SerializeField] private float _defaultDuration = 1.0f;

        private void Awake()
        {
            Instance = this;
            HideFade(true);
        }


        private void HideFade(bool hide)
        {
            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.alpha = hide ? 0f : 1f;
                _fadeCanvasGroup.gameObject.SetActive(!hide);
            }
            else if (_fadeImage != null)
            {
                Color c = _fadeImage.color;
                c.a = hide ? 0f : 1f;
                _fadeImage.color = c;
                _fadeImage.gameObject.SetActive(!hide);
            }
        }

        public void FadeToBlack(float duration = -1f)
        {
            if (duration < 0) duration = _defaultDuration;
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(1f, duration));
        }

        public void FadeFromBlack(float duration = -1f)
        {
            if (duration < 0) duration = _defaultDuration;
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(0f, duration));
        }

        private IEnumerator FadeRoutine(float targetAlpha, float duration)
        {
            GameObject targetObj = _fadeCanvasGroup != null ? _fadeCanvasGroup.gameObject : (_fadeImage != null ? _fadeImage.gameObject : null);
            if (targetObj == null) yield break;

            targetObj.SetActive(true);
            if (_fadeImage != null) _fadeImage.raycastTarget = true;

            float startAlpha = 0f;
            if (_fadeCanvasGroup != null) startAlpha = _fadeCanvasGroup.alpha;
            else if (_fadeImage != null) startAlpha = _fadeImage.color.a;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                
                if (_fadeCanvasGroup != null)
                {
                    _fadeCanvasGroup.alpha = currentAlpha;
                }
                else if (_fadeImage != null)
                {
                    Color c = _fadeImage.color;
                    c.a = currentAlpha;
                    _fadeImage.color = c;
                }
                
                yield return null;
            }

            // Final state
            if (_fadeCanvasGroup != null) _fadeCanvasGroup.alpha = targetAlpha;
            else if (_fadeImage != null)
            {
                Color c = _fadeImage.color;
                c.a = targetAlpha;
                _fadeImage.color = c;
            }

            if (targetAlpha <= 0) 
            {
                targetObj.SetActive(false);
                if (_fadeImage != null) _fadeImage.raycastTarget = false;
            }
        }

    }
}
