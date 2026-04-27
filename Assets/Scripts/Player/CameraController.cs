using UnityEngine;

namespace PuzzleDungeon.Player
{
    public class CameraController : MonoBehaviour
    {
        public enum CameraMode { Orbit, DirectFollow }
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
        [SerializeField] private float _collisionRadius = 0.3f;
        [SerializeField] private float _collisionSmoothTime = 0.05f;

        private float _currentX = 0f;
        private float _currentY = 20f;
        private float _currentDistance;
        private float _distanceVelocity;
        private Vector3 _currentVelocity;
        private Vector3 _smoothPosition;

        [Header("Focus Settings")]
        [SerializeField] private float _focusSmoothTime = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _focusLookAtWeight = 0.8f;
        [SerializeField] private float _focusYOffset = 1.0f;
        
        private Transform _focusTarget;
        private Transform _originalTarget;
        private float _originalDistance;
        private Vector3 _originalOffset;
        private CameraMode _currentMode = CameraMode.Orbit;
        private float _focusTimeRemaining = 0f;
        private float _totalFocusDuration = 0f;
        private System.Collections.Generic.HashSet<Transform> _focusedTargets = new System.Collections.Generic.HashSet<Transform>();

        private void Start()
        {
            if (_target == null) return;
            _originalTarget = _target;
            _originalDistance = _distance;
            _originalOffset = _offset;
            _currentDistance = _distance;
            if (_collisionLayers == 0) _collisionLayers = ~0;
            Cursor.lockState = CursorLockMode.Locked;
            _smoothPosition = _target.position + _offset;
            _currentX = transform.eulerAngles.y;
            _currentY = transform.eulerAngles.x;
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            if (_focusTimeRemaining > 0) UpdateFocus();
            else HandleInput();
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
            float t = Time.deltaTime * (1f / _focusSmoothTime);
            _currentX = Mathf.LerpAngle(_currentX, targetX, t * 2f);
            _currentY = Mathf.Lerp(_currentY, targetY, t * 2f);
            if (_focusTimeRemaining <= 0) _focusTarget = null;
        }

        public void FocusOnOnce(Transform target) => FocusOnOnce(target, 2.0f);

        public void FocusOnOnce(Transform target, float duration)
        {
            if (target == null) return;
            if (_focusedTargets.Contains(target)) return;
            
            _focusedTargets.Add(target);
            FocusOn(target, duration);
        }

        public void FocusOn(Transform target) => FocusOn(target, 2.0f);

        public void FocusOn(Transform target, float duration)
        {
            if (target == null) return;
            if (duration <= 0) duration = 2.0f;

            _focusTarget = target;
            _focusTimeRemaining = duration;
            _totalFocusDuration = duration;
        }

        public void SetTarget(Transform newTarget, CameraMode mode = CameraMode.Orbit, float? overrideDistance = null, Vector3? overrideOffset = null)
        {
            if (newTarget == null) return;
            _target = newTarget;
            _currentMode = mode;
            
            if (overrideDistance.HasValue) _distance = overrideDistance.Value;
            if (overrideOffset.HasValue) _offset = overrideOffset.Value;

            _smoothPosition = _target.position + _offset;
            _currentDistance = _distance;

            if (mode == CameraMode.DirectFollow)
            {
                _currentX = _target.eulerAngles.y;
                _currentY = _target.eulerAngles.x;
            }
        }

        public void ResetTarget()
        {
            _target = _originalTarget;
            _distance = _originalDistance;
            _offset = _originalOffset;
            _currentMode = CameraMode.Orbit;
        }

        private void CalculateCameraPosition()
        {
            // === MODE VOL (DirectFollow) ===
            // On court-circuite entièrement le système de collision.
            // La caméra se place directement, derrière et au-dessus selon le Y du monde.
            if (_currentMode == CameraMode.DirectFollow)
            {
                // Position : derrière et au-dessus selon l'orientation locale du scarabée
                transform.position = _target.position
                                     - _target.forward * _distance
                                     + _target.up * _offset.y;

                // LookRotation = même direction que le scarabée (il est donc au centre)
                // + son up pour gérer les loopings sans le LookAt qui décale
                transform.rotation = Quaternion.LookRotation(_target.forward, _target.up);
                return;
            }

            // === MODE ORBITE (Joueur) ===
            _smoothPosition = Vector3.SmoothDamp(_smoothPosition, _target.position + _offset, ref _currentVelocity, _smoothTime);
            Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0);
            
            Vector3 desiredDirection = rotation * new Vector3(0, 0, -_distance);
            Vector3 desiredPosition = _smoothPosition + desiredDirection;

            // --- Gestion des Collisions (Orbite uniquement) ---
            float targetDistance = _distance;
            RaycastHit hit;
            Vector3 rayDirection = (desiredPosition - _smoothPosition).normalized;

            if (Physics.SphereCast(_smoothPosition, _collisionRadius, rayDirection, out hit, _distance, _collisionLayers))
            {
                targetDistance = Mathf.Max(hit.distance, 0.2f);
            }

            if (targetDistance < _currentDistance)
            {
                _currentDistance = targetDistance;
                _distanceVelocity = 0f;
            }
            else
            {
                _currentDistance = Mathf.SmoothDamp(_currentDistance, targetDistance, ref _distanceVelocity, _collisionSmoothTime);
            }

            transform.position = _smoothPosition + rayDirection * _currentDistance;

            Vector3 lookTarget = _smoothPosition;
            if (_focusTarget != null && _focusTimeRemaining > 0)
            {
                Vector3 focusPoint = _focusTarget.position + Vector3.up * _focusYOffset;
                lookTarget = Vector3.Lerp(_smoothPosition, focusPoint, _focusLookAtWeight);
            }
            transform.LookAt(lookTarget);
        }
    }
}
