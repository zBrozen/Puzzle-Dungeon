using UnityEngine;

namespace PuzzleDungeon.Player
{
    public class CameraController : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(0, 1.5f, 0);

        [Header("Orbit Settings")]
        [SerializeField] private float _distance = 5f;
        [SerializeField] private float _minDistance = 2f;
        [SerializeField] private float _maxDistance = 10f;
        [SerializeField] private float _sensitivity = 3f;
        [SerializeField] private float _verticalMinAngle = -20f;
        [SerializeField] private float _verticalMaxAngle = 60f;
        [SerializeField] private float _smoothTime = 0.12f;

        [Header("Collision Settings")]
        [SerializeField] private LayerMask _collisionLayers;
        [SerializeField] private float _collisionRadius = 0.3f; // Augmenté pour plus de sécurité
        [SerializeField] private float _collisionSmoothTime = 0.05f;

        private float _currentX = 0f;
        private float _currentY = 20f;
        private float _currentDistance;
        private float _distanceVelocity;
        private Vector3 _currentVelocity;
        private Vector3 _smoothPosition;

        [Header("Focus Settings")]
        [SerializeField] private float _focusSmoothTime = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _focusLookAtWeight = 0.8f; // 1 = Porte au centre, 0 = Joueur au centre
        [SerializeField] private float _focusYOffset = 1.0f; // Pour monter un peu la vue
        
        private Transform _focusTarget;
        private float _focusTimeRemaining = 0f;
        private float _totalFocusDuration = 0f;
        private System.Collections.Generic.HashSet<Transform> _focusedTargets = new System.Collections.Generic.HashSet<Transform>();

        private void Start()
        {
            if (_target == null)
            {
                Debug.LogWarning("CameraController: No target assigned!");
                return;
            }

            _currentDistance = _distance;
            
            // On laisse l'utilisateur configurer les Collision Layers dans l'inspecteur.
            // Si rien n'est coché, on met "Everything" par défaut.
            if (_collisionLayers == 0) _collisionLayers = ~0;

            Cursor.lockState = CursorLockMode.Locked;
            _smoothPosition = _target.position + _offset;

            // Initialisation des angles actuels
            _currentX = transform.eulerAngles.y;
            _currentY = transform.eulerAngles.x;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            if (_focusTimeRemaining > 0)
            {
                UpdateFocus();
            }
            else
            {
                HandleInput();
            }
            
            CalculateCameraPosition();
        }

        private void HandleInput()
        {
            _currentX += Input.GetAxis("Mouse X") * _sensitivity;
            _currentY -= Input.GetAxis("Mouse Y") * _sensitivity;

            _currentY = Mathf.Clamp(_currentY, _verticalMinAngle, _verticalMaxAngle);

            _distance -= Input.GetAxis("Mouse ScrollWheel") * 5f;
            _distance = Mathf.Clamp(_distance, _minDistance, _maxDistance);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
            }
        }

        private void UpdateFocus()
        {
            _focusTimeRemaining -= Time.deltaTime;

            Vector3 dirToTarget = (_focusTarget.position - (_target.position + _offset)).normalized;
            Quaternion targetRot = Quaternion.LookRotation(dirToTarget);
            
            float targetX = targetRot.eulerAngles.y;
            float targetY = targetRot.eulerAngles.x;
            if (targetY > 180) targetY -= 360;

            // Augmentation de la réactivité de la transition
            float t = Time.deltaTime * (1f / _focusSmoothTime);
            _currentX = Mathf.LerpAngle(_currentX, targetX, t * 2f);
            _currentY = Mathf.Lerp(_currentY, targetY, t * 2f);
            
            if (_focusTimeRemaining <= 0)
            {
                Debug.Log($"[CameraController] Focus on {_focusTarget.name} finished.");
                _focusTarget = null;
            }
        }

        public void FocusOnOnce(Transform target) => FocusOnOnce(target, 2.0f);

        public void FocusOnOnce(Transform target, float duration)
        {
            if (target == null) return;

            if (_focusedTargets.Contains(target))
            {
                Debug.Log($"[CameraController] Already focused on {target.name}, skipping.");
                return;
            }
            
            Debug.Log($"[CameraController] First focus on {target.name}, starting cinematic.");
            _focusedTargets.Add(target);
            FocusOn(target, duration);
        }

        /// <summary>
        /// Force la caméra à regarder une cible pendant une durée déterminée.
        /// </summary>
        public void FocusOn(Transform target) => FocusOn(target, 2.0f);

        public void FocusOn(Transform target, float duration)
        {
            if (target == null) return;
            
            // Sécurité : si la durée est 0 (erreur dans l'inspecteur Unity), on met 2s
            if (duration <= 0) duration = 2.0f;

            Debug.Log($"[CameraController] Starting focus on {target.name} for {duration}s");
            _focusTarget = target;
            _focusTimeRemaining = duration;
            _totalFocusDuration = duration;
        }

        private void CalculateCameraPosition()
        {
            _smoothPosition = Vector3.SmoothDamp(_smoothPosition, _target.position + _offset, ref _currentVelocity, _smoothTime);

            Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0);
            Vector3 desiredDirection = rotation * new Vector3(0, 0, -_distance);
            Vector3 desiredPosition = _smoothPosition + desiredDirection;

            // --- Gestion des Collisions ---
            float targetDistance = _distance;
            RaycastHit hit;
            Vector3 rayDirection = (desiredPosition - _smoothPosition).normalized;

            // On utilise un SphereCast (rayon épais) pour détecter les obstacles
            if (Physics.SphereCast(_smoothPosition, _collisionRadius, rayDirection, out hit, _distance, _collisionLayers))
            {
                // La distance du SphereCast est déjà la distance à laquelle le centre de la caméra doit s'arrêter
                targetDistance = Mathf.Max(hit.distance, 0.2f);
            }

            // LOGIQUE DE RÉACTIVITÉ :
            // Si on doit se rapprocher (collision), on le fait INSTANTANÉMENT
            if (targetDistance < _currentDistance)
            {
                _currentDistance = targetDistance;
                _distanceVelocity = 0f;
            }
            else
            {
                // Si on s'éloigne, on lisse pour la fluidité
                _currentDistance = Mathf.SmoothDamp(_currentDistance, targetDistance, ref _distanceVelocity, _collisionSmoothTime);
            }
            
            transform.position = _smoothPosition + rayDirection * _currentDistance;
            
            // Gestion du cadrage (Look At)
            Vector3 lookTarget = _smoothPosition;
            if (_focusTarget != null && _focusTimeRemaining > 0)
            {
                // On mélange la position du joueur et celle de la cible pour le cadrage
                Vector3 focusPoint = _focusTarget.position + Vector3.up * _focusYOffset;
                lookTarget = Vector3.Lerp(_smoothPosition, focusPoint, _focusLookAtWeight);
            }

            transform.LookAt(lookTarget);
        }
    }
}
