using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace PuzzleDungeon.UI
{
    public class TutorialUIManager : MonoBehaviour
    {
        public static TutorialUIManager Instance { get; private set; }

        [Header("UI Components")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _tutorialText;
        [SerializeField] private Image _textBackground; // Nouveau : image de fond pour le texte
        [SerializeField] private Image _tutorialImage;

        [Header("Animation Settings")]
        [SerializeField] private float _fadeDuration = 0.5f;

        private Coroutine _displayCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ShowTutorial(string message, Sprite image, float duration)
        {
            if (_displayCoroutine != null)
            {
                StopCoroutine(_displayCoroutine);
            }
            _displayCoroutine = StartCoroutine(DisplayRoutine(message, image, duration));
        }

        private IEnumerator DisplayRoutine(string message, Sprite image, float duration)
        {
            // Setup content
            bool hasText = !string.IsNullOrEmpty(message);
            if (_tutorialText != null) 
            {
                _tutorialText.text = message;
                _tutorialText.gameObject.SetActive(hasText);
            }

            if (_textBackground != null)
            {
                _textBackground.gameObject.SetActive(hasText);
            }
            
            if (_tutorialImage != null)
            {
                if (image != null)
                {
                    _tutorialImage.sprite = image;
                    _tutorialImage.gameObject.SetActive(true);
                }
                else
                {
                    _tutorialImage.gameObject.SetActive(false);
                }
            }

            // Fade In
            yield return StartCoroutine(Fade(1f));

            // Wait
            yield return new WaitForSeconds(duration);

            // Fade Out
            yield return StartCoroutine(Fade(0f));

            _displayCoroutine = null;
        }

        private IEnumerator Fade(float targetAlpha)
        {
            if (_canvasGroup == null) yield break;

            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / _fadeDuration);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
        }
    }
}
