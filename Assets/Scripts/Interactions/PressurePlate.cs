using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using PuzzleDungeon.Player;

namespace PuzzleDungeon.Interactions
{
    /// <summary>
    /// Manages a physical pressure plate that sinks when occupied.
    /// Uses Physics.OverlapBox for 100% reliable detection.
    /// </summary>
    public class PressurePlate : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private Transform _movingPart;
        [SerializeField] private float _maxDepression = 0.15f;
        [SerializeField] private float _pressSpeed = 5f;
        [SerializeField] private float _crushSpeed = 20f;
        [SerializeField] private float _releaseSpeed = 3f;

        [Header("Logic")]
        [SerializeField, Range(0.1f, 1f)] private float _activationThreshold = 0.8f;
        [SerializeField] private LayerMask _detectableLayers = ~0;

        [Header("Detection Settings")]
        [SerializeField] private Vector3 _detectionHalfExtents = new Vector3(0.5f, 0.5f, 0.5f);
        [SerializeField] private Vector3 _detectionOffset = new Vector3(0, 0.25f, 0);

        [Header("Events")]
        public UnityEvent OnActivated;
        public UnityEvent OnDeactivated;

        private Vector3 _restLocalPos;
        private Vector3 _depressedLocalPos;
        private float _currentDepression = 0f; // 0 = rest, 1 = max
        private bool _isPressed = false;
        private HashSet<Collider> _occupants = new HashSet<Collider>();
        private bool _hasBlock = false;

        private void Awake()
        {
            if (_movingPart == null) _movingPart = transform;
            _restLocalPos = _movingPart.localPosition;
            _depressedLocalPos = _restLocalPos + Vector3.down * _maxDepression;
        }

        private void Update()
        {
            ScanForOccupants();
            UpdateDepression();
            CheckActivation();
        }

        private void ScanForOccupants()
        {
            // On scanne TOUT dans la zone, incluant les Triggers
            Collider[] colliders = Physics.OverlapBox(
                transform.position + transform.TransformDirection(_detectionOffset), 
                _detectionHalfExtents, 
                transform.rotation, 
                ~0, // On force TOUS les layers
                QueryTriggerInteraction.Collide // On force la détection des Triggers
            );
            
            _occupants.Clear();
            _hasBlock = false;

            foreach (var col in colliders)
            {
                if (col.transform.IsChildOf(transform)) continue;

                // On cherche le bloc (très agressif)
                PushableBlock block = col.GetComponentInParent<PushableBlock>();
                if (block == null) block = col.GetComponentInChildren<PushableBlock>();

                PlayerController player = col.GetComponentInParent<PlayerController>();

                if (block != null)
                {
                    _occupants.Add(col);
                    _hasBlock = true;
                }
                else if (player != null)
                {
                    if (player.CurrentState != PlayerController.PlayerState.Jump && player.CurrentState != PlayerController.PlayerState.Fall)
                    {
                        _occupants.Add(col);
                    }
                }
            }
        }

        private void UpdateDepression()
        {
            float target = (_occupants.Count > 0) ? 1f : 0f;
            float speed = target > _currentDepression ? (_hasBlock ? _crushSpeed : _pressSpeed) : _releaseSpeed;

            float prevDepression = _currentDepression;
            _currentDepression = Mathf.MoveTowards(_currentDepression, target, speed * Time.deltaTime);
            
            if (!Mathf.Approximately(prevDepression, _currentDepression))
            {
                _movingPart.localPosition = Vector3.Lerp(_restLocalPos, _depressedLocalPos, _currentDepression);
                
                // Occasional log for debugging
                if (Time.frameCount % 30 == 0 && _currentDepression > 0)
                    Debug.Log($"[PressurePlate] {name} Occupants: {_occupants.Count} | Sinking: {_currentDepression:P0}");
            }
        }

        [SerializeField] private float _deactivationDelay = 1.0f;
        private float _deactivationTimer = 0f;

        private void CheckActivation()
        {
            if (_occupants.Count > 0)
            {
                _deactivationTimer = _deactivationDelay; // On reset le timer tant qu'il y a du monde
                
                if (!_isPressed && _currentDepression >= _activationThreshold)
                {
                    _isPressed = true;
                    OnActivated?.Invoke();
                }
            }
            else
            {
                // Si plus d'occupant, on attend avant de désactiver
                if (_isPressed)
                {
                    _deactivationTimer -= Time.deltaTime;
                    if (_deactivationTimer <= 0)
                    {
                        _isPressed = false;
                        OnDeactivated?.Invoke();
                    }
                }
            }
        }

        private bool IsBlock(Collider col)
        {
            return col.GetComponentInParent<PushableBlock>() != null;
        }

        public void NotifyAutoJumpZone(Collider other)
        {
            // Si le bouton est déjà bien enfoncé, on ne propose plus de saut automatique
            if (_currentDepression > 0.3f) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null && player.CurrentState != PlayerController.PlayerState.Jump)
            {
                Vector3 toButton = (transform.position - player.transform.position);
                toButton.y = 0;
                float distance = toButton.magnitude;

                // On ne déclenche le saut que si on est à une distance raisonnable
                // (Si on est trop près, comme 0.5m, c'est qu'on est déjà dessus ou en train de pousser un bloc)
                if (distance < 0.6f) return;

                // Vérification de la direction
                float dot = Vector3.Dot(player.transform.forward, toButton.normalized);
                if (dot > 0.5f) 
                {
                    Debug.Log($"[PressurePlate] {name} Auto-Jump triggered for {player.name}");
                    player.JumpTo(transform.position);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(_detectionOffset, _detectionHalfExtents * 2f);
        }
    }
}
