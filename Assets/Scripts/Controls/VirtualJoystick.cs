using UnityEngine;
using UnityEngine.EventSystems;

namespace PuzzleDungeon.Controls
{
    /// <summary>
    /// Joystick flottant : il apparaît là où le doigt se pose dans sa zone,
    /// et alimente GameInput.SetTouchMove.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;
        [SerializeField] private float _radius = 110f;
        [SerializeField, Range(0f, 0.5f)] private float _deadZone = 0.15f;

        private RectTransform _area;
        private Vector2 _restPosition;
        private int _pointerId = int.MinValue;

        public void Setup(RectTransform background, RectTransform handle)
        {
            _background = background;
            _handle = handle;
            _restPosition = background.anchoredPosition;
        }

        private void Awake()
        {
            _area = (RectTransform)transform;
            if (_background != null) _restPosition = _background.anchoredPosition;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != int.MinValue) return;
            _pointerId = eventData.pointerId;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                _background.anchoredPosition = local - _area.rect.min - _area.rect.size * _background.anchorMin;
            }
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_background, eventData.position, eventData.pressEventCamera, out Vector2 local);
            Vector2 offset = Vector2.ClampMagnitude(local, _radius);
            _handle.anchoredPosition = offset;

            Vector2 value = offset / _radius;
            GameInput.SetTouchMove(value.magnitude < _deadZone ? Vector2.zero : value);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            Release();
        }

        private void OnDisable() => Release();

        private void Release()
        {
            _pointerId = int.MinValue;
            if (_handle != null) _handle.anchoredPosition = Vector2.zero;
            if (_background != null) _background.anchoredPosition = _restPosition;
            GameInput.SetTouchMove(Vector2.zero);
        }
    }
}
