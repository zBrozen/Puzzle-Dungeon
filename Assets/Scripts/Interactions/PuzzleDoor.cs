using UnityEngine;

namespace PuzzleDungeon.Interactions
{
    /// <summary>
    /// Script pour gérer l'ouverture et la fermeture des portes du donjon.
    /// Peut être piloté par un PressurePlate via les UnityEvents.
    /// </summary>
    public class PuzzleDoor : MonoBehaviour
    {
        [Header("Animation Method")]
        [Tooltip("Si assigné, le script pilotera l'Animator.")]
        [SerializeField] private Animator _animator;
        [SerializeField] private string _boolParameterName = "IsOpen";

        [Header("Procedural Movement (Optional)")]
        [Tooltip("Si aucun Animator n'est assigné, la porte glissera vers cette position locale.")]
        [SerializeField] private Vector3 _openedLocalPosition;
        [SerializeField] private float _speed = 2f;

        private Vector3 _closedLocalPosition;
        private bool _isOpen = false;

        private void Awake()
        {
            _closedLocalPosition = transform.localPosition;
            
            // Si on n'a pas d'Animator et qu'on n'a pas défini de position ouverte,
            // on propose une valeur par défaut (monte de 3m)
            if (_animator == null && _openedLocalPosition == Vector3.zero)
            {
                _openedLocalPosition = _closedLocalPosition + Vector3.up * 3f;
            }
        }

        private void Update()
        {
            // Si on n'utilise pas d'Animator, on gère le mouvement fluide ici
            if (_animator == null)
            {
                Vector3 target = _isOpen ? _openedLocalPosition : _closedLocalPosition;
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, _speed * Time.deltaTime);
            }
        }

        [ContextMenu("Open Door")]
        public void Open()
        {
            _isOpen = true;
            if (_animator != null)
            {
                _animator.SetBool(_boolParameterName, true);
            }
            Debug.Log($"[PuzzleDoor] {name} Opening");
        }

        [ContextMenu("Close Door")]
        public void Close()
        {
            _isOpen = false;
            if (_animator != null)
            {
                _animator.SetBool(_boolParameterName, false);
            }
            Debug.Log($"[PuzzleDoor] {name} Closing");
        }

        // Méthode utilitaire pour inverser l'état
        public void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }
    }
}
