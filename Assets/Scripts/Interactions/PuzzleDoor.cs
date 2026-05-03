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
        [SerializeField] private bool _hideMeshWhenOpened = true;
        [SerializeField] private bool _startOpen = false;

        private Vector3 _closedLocalPosition;
        private bool _isOpen = false;
        private MeshRenderer _meshRenderer;
        private Collider _collider;

        private void Awake()
        {
            _closedLocalPosition = transform.localPosition;
            _meshRenderer = GetComponent<MeshRenderer>();
            _collider = GetComponent<Collider>();
            
            // Si on n'a pas d'Animator et qu'on n'a pas défini de position ouverte,
            // on propose une valeur par défaut (monte de 4m)
            if (_animator == null && _openedLocalPosition == Vector3.zero)
            {
                _openedLocalPosition = _closedLocalPosition + Vector3.up * 4f;
            }

            // État initial
            _isOpen = _startOpen;
            if (_isOpen)
            {
                if (_animator != null)
                {
                    _animator.SetBool(_boolParameterName, true);
                }
                else
                {
                    transform.localPosition = _openedLocalPosition;
                    if (_hideMeshWhenOpened)
                    {
                        if (_meshRenderer != null) _meshRenderer.enabled = false;
                        if (_collider != null) _collider.enabled = false;
                    }
                }
            }
        }

        private void Update()
        {
            // Si on n'utilise pas d'Animator, on gère le mouvement fluide ici
            if (_animator == null)
            {
                Vector3 target = _isOpen ? _openedLocalPosition : _closedLocalPosition;
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, _speed * Time.deltaTime);

                // Gestion de la visibilité quand la porte est ouverte
                if (_hideMeshWhenOpened)
                {
                    bool shouldBeVisible = !_isOpen || Vector3.Distance(transform.localPosition, _openedLocalPosition) > 0.01f;
                    
                    if (_meshRenderer != null && _meshRenderer.enabled != shouldBeVisible)
                        _meshRenderer.enabled = shouldBeVisible;
                        
                    if (_collider != null && _collider.enabled != shouldBeVisible)
                        _collider.enabled = shouldBeVisible;
                }
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
            
            // On s'assure de réactiver les visuels si on referme la porte
            if (_meshRenderer != null) _meshRenderer.enabled = true;
            if (_collider != null) _collider.enabled = true;

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
