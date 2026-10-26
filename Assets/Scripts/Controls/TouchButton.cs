using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PuzzleDungeon.Controls
{
    /// <summary>
    /// Bouton tactile : équivaut à un GetKeyDown sur la frame qui suit l'appui.
    /// </summary>
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private GameButton _button;
        [SerializeField] private Graphic _graphic;
        [SerializeField] private float _pressedAlpha = 0.85f;

        private float _releasedAlpha;

        public GameButton Button => _button;

        public void Setup(GameButton button, Graphic graphic)
        {
            _button = button;
            _graphic = graphic;
            _releasedAlpha = graphic.color.a;
        }

        private void Awake()
        {
            if (_graphic == null) _graphic = GetComponent<Graphic>();
            if (_graphic != null) _releasedAlpha = _graphic.color.a;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            GameInput.PressTouchButton(_button);
            SetAlpha(_pressedAlpha);
        }

        public void OnPointerUp(PointerEventData eventData) => SetAlpha(_releasedAlpha);

        // Bouton masqué pendant l'appui : on ne recevra pas le PointerUp
        private void OnDisable() => SetAlpha(_releasedAlpha);

        private void SetAlpha(float alpha)
        {
            if (_graphic == null) return;
            Color c = _graphic.color;
            c.a = alpha;
            _graphic.color = c;
        }
    }
}
