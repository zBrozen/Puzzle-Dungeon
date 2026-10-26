using UnityEngine;
using UnityEngine.EventSystems;

namespace PuzzleDungeon.Controls
{
    /// <summary>
    /// Zone où glisser le doigt pour orienter la caméra (alimente GameInput.AddTouchLook).
    /// </summary>
    public class TouchLookArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        // Converti dans l'unité des axes "Mouse X/Y" pour réutiliser la sensibilité de CameraController
        [SerializeField] private float _sensitivity = 0.08f;

        private int _pointerId = int.MinValue;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId == int.MinValue) _pointerId = eventData.pointerId;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;

            // Normalisé sur une hauteur de référence de 1080 px pour être indépendant de la résolution
            float scale = 1080f / Mathf.Max(1, Screen.height);
            GameInput.AddTouchLook(eventData.delta * scale * _sensitivity);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == _pointerId) _pointerId = int.MinValue;
        }

        private void OnDisable() => _pointerId = int.MinValue;
    }
}
