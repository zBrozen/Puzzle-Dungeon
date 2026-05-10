using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace PuzzleDungeon.UI
{
    public class MenuButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Animation Settings")]
        [SerializeField] private float hoverScaleMultiplier = 1.05f;
        [SerializeField] private float pressScaleMultiplier = 0.95f;
        [SerializeField] private float animationDuration = 0.1f;

        private Vector3 originalScale;
        private Coroutine activeRoutine;

        private void Awake()
        {
            originalScale = transform.localScale;
        }

        private void OnDisable()
        {
            transform.localScale = originalScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            StartAnimation(originalScale * hoverScaleMultiplier);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StartAnimation(originalScale);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            StartAnimation(originalScale * pressScaleMultiplier);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            StartAnimation(originalScale * hoverScaleMultiplier);
        }

        private void StartAnimation(Vector3 targetScale)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(AnimateScale(targetScale));
        }

        private IEnumerator AnimateScale(Vector3 targetScale)
        {
            float time = 0;
            Vector3 startScale = transform.localScale;

            while (time < animationDuration)
            {
                transform.localScale = Vector3.Lerp(startScale, targetScale, time / animationDuration);
                time += Time.deltaTime;
                yield return null;
            }

            transform.localScale = targetScale;
        }
    }
}
