using UnityEngine;
using UnityEngine.Events;

namespace PuzzleDungeon.Interactions
{
    /// <summary>
    /// Cible réactive au Scarabée. 
    /// Peut déclencher n'importe quel événement (ouverture de porte, etc.)
    /// </summary>
    public class ScarabTarget : MonoBehaviour
    {
        public enum TargetAction { Activate, Deactivate, Toggle, None }

        [Header("Quick Action")]
        [SerializeField] private GameObject _objectToControl;
        [SerializeField] private TargetAction _action = TargetAction.Activate;

        [Header("Events")]
        public UnityEvent OnHit;

        [Header("Settings")]
        [SerializeField] private bool _oneShot = true;
        [SerializeField] private float _resetTime = 0f;

        private bool _hasBeenHit = false;

        /// <summary>
        /// Appelé par le script Scarab lors de l'impact.
        /// </summary>
        public void OnHitByScarab()
        {
            if (_oneShot && _hasBeenHit) return;

            Debug.Log($"[ScarabTarget] {name} touchée par le scarabée !");
            _hasBeenHit = true;
            
            // Applique l'action rapide
            ApplyAction(true);

            // Déclenche l'événement Unity (optionnel si l'action rapide suffit)
            OnHit?.Invoke();

            // Si ce n'est pas une cible à usage unique, on peut la réinitialiser
            if (!_oneShot && _resetTime > 0)
            {
                Invoke(nameof(ResetTarget), _resetTime);
            }
        }

        private void ApplyAction(bool isHit)
        {
            if (_objectToControl == null || _action == TargetAction.None) return;

            switch (_action)
            {
                case TargetAction.Activate:
                    _objectToControl.SetActive(isHit);
                    break;
                case TargetAction.Deactivate:
                    _objectToControl.SetActive(!isHit);
                    break;
                case TargetAction.Toggle:
                    if (isHit) _objectToControl.SetActive(!_objectToControl.activeSelf);
                    break;
            }
        }

        private void ResetTarget()
        {
            _hasBeenHit = false;
            
            // Si on veut que l'objet revienne à son état initial lors du reset
            if (_action == TargetAction.Activate || _action == TargetAction.Deactivate)
            {
                ApplyAction(false);
            }

            Debug.Log($"[ScarabTarget] {name} est de nouveau active.");
        }
    }
}
