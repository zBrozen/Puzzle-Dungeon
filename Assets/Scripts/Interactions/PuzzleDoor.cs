using UnityEngine;

namespace PuzzleDungeon.Interactions
{
    /// <summary>
    /// Script pour gérer l'ouverture et la fermeture des portes du donjon.
    /// Peut être piloté par un PressurePlate via les UnityEvents.
    /// </summary>
    public class PuzzleDoor : MonoBehaviour, IInteractable
    {
        [Header("Locking Settings")]
        [SerializeField] private bool _isLocked = false;
        [SerializeField] private ItemData _requiredKey;
        [SerializeField] private GameObject _lockObject;
        [SerializeField] private string _lockedPrompt = "Déverrouiller avec la clé";
        [SerializeField] private string _noKeyPrompt = "Porte verrouillée";

        [Header("Cinematic Settings")]
        [SerializeField] private bool _triggerDemoEndOnUnlock = false;
        [SerializeField] private Transform _cinematicCameraPoint;
        [SerializeField] private Transform _lookAtTarget;

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

        // IInteractable implementation
        public void Interact(PuzzleDungeon.Player.PlayerController player)
        {
            if (_isLocked)
            {
                PuzzleDungeon.Player.PlayerInventory inventory = player.GetComponent<PuzzleDungeon.Player.PlayerInventory>();
                
                // Check for key
                bool hasKey = false;
                if (inventory != null)
                {
                    hasKey = inventory.HasItem(_requiredKey) || inventory.HasItem("Key") || inventory.HasItem("key");
                }



                if (hasKey)
                {
                    Debug.Log("[PuzzleDoor] Key found! Unlocking...");
                    Unlock();
                    
                    if (_triggerDemoEndOnUnlock)
                    {
                        DemoEndManager.Instance.StartEndSequence(this);
                    }
                    else
                    {
                        Open();
                    }
                }
                else
                {
                    Debug.LogWarning($"[PuzzleDoor] Key NOT found. Required: {(_requiredKey != null ? _requiredKey.ItemName : "Key")}");
                }
            }
            else
            {
                Toggle();
            }
        }



        public string GetInteractionPrompt(PuzzleDungeon.Player.PlayerController player)
        {
            if (_isLocked)
            {
                PuzzleDungeon.Player.PlayerInventory inventory = player.GetComponent<PuzzleDungeon.Player.PlayerInventory>();
                bool hasKey = inventory != null && (inventory.HasItem(_requiredKey) || inventory.HasItem("Key") || inventory.HasItem("key"));

                if (hasKey)
                {
                    return _lockedPrompt;
                }
                return _noKeyPrompt;
            }
            
            return _isOpen ? "Fermer" : "Ouvrir";
        }



        public bool CanInteract(PuzzleDungeon.Player.PlayerController player)
        {
            // Si verrouillée, on ne peut interagir que si on a la clé (ou pour voir le message "verrouillé")
            // Dans notre cas, on permet l'interaction pour afficher le prompt "Porte verrouillée"
            return true;
        }

        public void Unlock()
        {
            _isLocked = false;
            if (_lockObject != null) _lockObject.SetActive(false);
            Debug.Log($"[PuzzleDoor] {name} Unlocked");
        }

        public Transform GetCinematicPoint() => _cinematicCameraPoint;
        public Transform GetLookAtTarget() => _lookAtTarget != null ? _lookAtTarget : transform;

    }
}
