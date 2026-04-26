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
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            HandleInput();
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
            transform.LookAt(_smoothPosition);
        }
    }
}
