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
        [SerializeField] private float _minCollisionDistance = 0.4f;

        [Header("Transition Settings")]
        [SerializeField] private float _transitionTime = 0.5f;
        private float _transitionTimer = 0f;
        private Vector3 _transitionStartPos;
        private Quaternion _transitionStartRot;

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
        
        [SerializeField] private Vector3 _defaultFocusCameraOffset = Vector3.zero;
        [SerializeField] private Vector3 _defaultFocusTargetOffset = Vector3.zero;
        
        private Transform _focusTarget;
        private Transform _originalTarget;
        private float _originalDistance;
        private Vector3 _originalOffset;
        private CameraMode _currentMode = CameraMode.Orbit;
        
        private float _activeMinAngle;
        private float _activeMaxAngle;
        private float _focusTimeRemaining = 0f;
        private float _totalFocusDuration = 0f;
        private Vector3 _activeFocusCameraOffset;
        private Vector3 _activeFocusTargetOffset;
        private System.Collections.Generic.HashSet<Transform> _focusedTargets = new System.Collections.Generic.HashSet<Transform>();
        private Transform _ignoreTarget;
        
        public bool IsInputDisabled { get; set; }

        public bool IsTransitioning => _transitionTimer > 0;

        private void Start()
        {
            if (_target == null) return;
            _originalTarget = _target;
            _originalDistance = _distance;
            _originalOffset = _offset;
            _currentDistance = _distance;
            if (_collisionLayers == 0) _collisionLayers = ~0;
            
            _activeMinAngle = _verticalMinAngle;
            _activeMaxAngle = _verticalMaxAngle;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _smoothPosition = _target.position + _offset;
            _currentX = transform.eulerAngles.y;
            _currentY = transform.eulerAngles.x;
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            if (_focusTimeRemaining > 0) UpdateFocus();
            else if (!IsInputDisabled) HandleInput();
            CalculateCameraPosition();
        }

        public void AddOrbitRotation(float x, float y)
        {
            _currentX += x;
            _currentY -= y;
            _currentY = Mathf.Clamp(_currentY, _activeMinAngle, _activeMaxAngle);
        }

        private void HandleInput()
        {
            _currentX += Input.GetAxis("Mouse X") * _sensitivity;
            _currentY -= Input.GetAxis("Mouse Y") * _sensitivity;
            _currentY = Mathf.Clamp(_currentY, _activeMinAngle, _activeMaxAngle);
            _distance -= Input.GetAxis("Mouse ScrollWheel") * 5f;
            _distance = Mathf.Clamp(_distance, _minDistance, _maxDistance);
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        private void UpdateFocus()
        {
            _focusTimeRemaining -= Time.deltaTime;
            Vector3 targetFocusPos = _focusTarget.position + Vector3.up * _focusYOffset + _activeFocusTargetOffset;
            Vector3 currentPivot = _target.position + _offset + _activeFocusCameraOffset;
            Vector3 dirToTarget = (targetFocusPos - currentPivot).normalized;
            
            Quaternion targetRot = Quaternion.LookRotation(dirToTarget);
            float targetX = targetRot.eulerAngles.y;
            float targetY = targetRot.eulerAngles.x;
            if (targetY > 180) targetY -= 360;
            
            float t = Time.deltaTime * (1f / _focusSmoothTime);
            _currentX = Mathf.LerpAngle(_currentX, targetX, t * 2f);
            _currentY = Mathf.Lerp(_currentY, targetY, t * 2f);
            
            if (_focusTimeRemaining <= 0)
            {
                _focusTarget = null;
                _activeFocusCameraOffset = Vector3.zero;
                _activeFocusTargetOffset = Vector3.zero;
            }
        }

        public void FocusOnOnce(Transform target) => FocusOnOnce(target, 2.0f);

        public void FocusOnOnce(Transform target, float duration, Vector3? cameraOffset = null, Vector3? targetOffset = null)
        {
            if (target == null) return;
            if (_focusedTargets.Contains(target)) return;
            
            _focusedTargets.Add(target);
            FocusOn(target, duration, cameraOffset, targetOffset);
        }

        public void FocusOn(Transform target) => FocusOn(target, 2.0f);

        public void FocusOn(Transform target, float duration, Vector3? cameraOffset = null, Vector3? targetOffset = null)
        {
            if (target == null) return;
            if (duration <= 0) duration = 2.0f;

            _focusTarget = target;
            _focusTimeRemaining = duration;
            _totalFocusDuration = duration;
            _activeFocusCameraOffset = cameraOffset ?? _defaultFocusCameraOffset;
            _activeFocusTargetOffset = targetOffset ?? _defaultFocusTargetOffset;
        }

        public void SetIgnoreTarget(Transform target) => _ignoreTarget = target;

        public void SetTarget(Transform newTarget, CameraMode mode = CameraMode.Orbit, float? overrideDistance = null, Vector3? overrideOffset = null, float? overrideMinAngle = null, float? overrideMaxAngle = null)
        {
            if (newTarget == null) return;

            // ... (transition logic)
            _transitionStartPos = transform.position;
            _transitionStartRot = transform.rotation;
            _transitionTimer = _transitionTime;

            _target = newTarget;
            _currentMode = mode;

            if (overrideDistance.HasValue) _distance = overrideDistance.Value;
            if (overrideOffset.HasValue) _offset = overrideOffset.Value;
            
            if (overrideMinAngle.HasValue) _activeMinAngle = overrideMinAngle.Value;
            else _activeMinAngle = _verticalMinAngle;

            if (overrideMaxAngle.HasValue) _activeMaxAngle = overrideMaxAngle.Value;
            else _activeMaxAngle = _verticalMaxAngle;

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
            // Déclenchement de la transition
            _transitionStartPos = transform.position;
            _transitionStartRot = transform.rotation;
            _transitionTimer = _transitionTime;

            _target = _originalTarget;
            _distance = _originalDistance;
            _offset = _originalOffset;
            _activeMinAngle = _verticalMinAngle;
            _activeMaxAngle = _verticalMaxAngle;
            _currentMode = CameraMode.Orbit;
        }

        public void SnapToTarget()
        {
            if (_target != null)
            {
                _smoothPosition = _target.position + _offset;
                _currentVelocity = Vector3.zero;
                _transitionTimer = 0f; // Annuler toute transition en cours
            }
        }

        private void CalculateCameraPosition()
        {
            Vector3 targetPos;
            Quaternion targetRot;

            // === CALCUL DE LA DESTINATION ===
            if (_currentMode == CameraMode.DirectFollow)
            {
                Vector3 desiredPos = _target.position - _target.forward * _distance + _target.up * _offset.y;
                
                if (_focusTarget != null && _focusTimeRemaining > 0)
                {
                    Vector3 focusPoint = _focusTarget.position + Vector3.up * _focusYOffset + _activeFocusTargetOffset;
                    targetRot = Quaternion.LookRotation(focusPoint - desiredPos);
                }
                else
                {
                    targetRot = Quaternion.LookRotation(_target.forward, _target.up);
                }

                // --- SÉCURITÉ COLLISIONS (DirectFollow) ---
                Vector3 headPos = _target.position; // On part du centre de la cible (le scarabée)
                Vector3 toCameraDir = desiredPos - headPos;
                float toCameraDist = toCameraDir.magnitude;
                
                targetPos = desiredPos;

                if (toCameraDist > 0.01f)
                {
                    RaycastHit[] hits = Physics.SphereCastAll(headPos, _collisionRadius, toCameraDir.normalized, toCameraDist, _collisionLayers);
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                    Collider originalTargetCollider = _originalTarget != null ? _originalTarget.GetComponentInChildren<Collider>() : null;

                    foreach (var h in hits)
                    {
                        // On ignore la cible actuelle (ex: le scarabée)
                        if (h.collider.transform.IsChildOf(_target)) continue;
                        
                        // On ignore la cible originale (le joueur)
                        if (originalTargetCollider != null && (h.collider == originalTargetCollider || h.collider.transform.IsChildOf(_originalTarget))) continue;

                        targetPos = headPos + toCameraDir.normalized * Mathf.Max(h.distance, _minCollisionDistance);
                        break;
                    }
                }
            }
            else // ORBITE
            {
                Vector3 rotatedOffset = Quaternion.Euler(0, _currentX, 0) * _offset;
                Vector3 desiredPivot = _target.position + rotatedOffset;

                // --- SÉCURITÉ PIVOT ---
                // Si le pivot décalé est dans un mur, on le ramène vers le joueur
                Vector3 pivotDir = desiredPivot - _target.position;
                float pivotDist = pivotDir.magnitude;
                if (pivotDist > 0.01f)
                {
                    if (Physics.Raycast(_target.position, pivotDir.normalized, out RaycastHit pivotHit, pivotDist, _collisionLayers))
                    {
                        desiredPivot = pivotHit.point - pivotDir.normalized * 0.1f;
                    }
                }

                if (_focusTarget != null && _focusTimeRemaining > 0)
                {
                    desiredPivot += _activeFocusCameraOffset;
                }

                _smoothPosition = Vector3.SmoothDamp(_smoothPosition, desiredPivot, ref _currentVelocity, _smoothTime);
                Quaternion orbitRotation = Quaternion.Euler(_currentY, _currentX, 0);

                Vector3 desiredDirection = orbitRotation * new Vector3(0, 0, -_distance);
                Vector3 desiredPosition = _smoothPosition + desiredDirection;

                // --- LOGIQUE COLLISIONS DURCIE (Joueur -> Caméra) ---
                // On vérifie le chemin entre la tête du joueur et la position voulue de la caméra
                Vector3 headPos = _target.position + Vector3.up * _offset.y;
                Vector3 toCameraDir = desiredPosition - headPos;
                float toCameraDist = toCameraDir.magnitude;

                targetPos = desiredPosition; // Valeur par défaut pour éviter CS0165

                if (toCameraDist > 0.01f)
                {
                    // On part directement de la tête (plus de recul qui bug sous les arches)
                    RaycastHit[] hits = Physics.SphereCastAll(headPos, _collisionRadius, toCameraDir.normalized, toCameraDist, _collisionLayers);
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                    Collider playerCollider = _target.GetComponentInChildren<Collider>();

                    foreach (var h in hits)
                    {
                        // On ignore le joueur lui-même
                        if (playerCollider != null && (h.collider == playerCollider || h.collider.transform.IsChildOf(_target)))
                            continue;
                            
                        // On ignore également le scarabée
                        if (_ignoreTarget != null && (h.collider.transform == _ignoreTarget || h.collider.transform.IsChildOf(_ignoreTarget)))
                            continue;

                        // La distance est maintenant directe
                        targetPos = headPos + toCameraDir.normalized * Mathf.Max(h.distance, _minCollisionDistance);
                        break;
                    }
                }

                // Sécurité finale "Cylindrique" : on empêche d'entrer dans le corps du joueur
                Vector3 playerFeet = _target.position;
                Vector3 camToFeet = targetPos - playerFeet;
                float horizontalDist = new Vector2(camToFeet.x, camToFeet.z).magnitude;
                float verticalDist = Mathf.Abs(targetPos.y - (playerFeet.y + _offset.y * 0.5f));

                // Si on est trop proche horizontalement ET dans la zone de hauteur du corps
                if (horizontalDist < _minCollisionDistance && verticalDist < _offset.y * 0.8f)
                {
                    Vector3 pushDir = new Vector3(camToFeet.x, 0, camToFeet.z).normalized;
                    if (pushDir.sqrMagnitude < 0.001f) pushDir = -_target.forward;
                    targetPos.x = playerFeet.x + pushDir.x * _minCollisionDistance;
                    targetPos.z = playerFeet.z + pushDir.z * _minCollisionDistance;
                }

                Vector3 lookTarget = _smoothPosition;
                if (_focusTarget != null && _focusTimeRemaining > 0)
                {
                    Vector3 focusPoint = _focusTarget.position + Vector3.up * _focusYOffset + _activeFocusTargetOffset;
                    lookTarget = Vector3.Lerp(_smoothPosition, focusPoint, _focusLookAtWeight);
                }
                
                targetRot = Quaternion.LookRotation(lookTarget - targetPos);
            }

            // === APPLICATION AVEC SÉCURITÉ COLLISION (Même pendant transition) ===
            if (_transitionTimer > 0)
            {
                _transitionTimer -= Time.deltaTime;
                float t = 1f - (_transitionTimer / _transitionTime);
                t = t * t * (3f - 2f * t); 

                Vector3 lerpedPos = Vector3.Lerp(_transitionStartPos, targetPos, t);
                
                // On vérifie si le chemin du Lerp traverse un mur
                Vector3 playerOrigin = _target.position + Vector3.up * _offset.y;
                Vector3 toLerp = lerpedPos - playerOrigin;
                float toLerpDist = toLerp.magnitude;

                if (toLerpDist > 0.01f)
                {
                    RaycastHit[] tHits = Physics.SphereCastAll(playerOrigin, _collisionRadius, toLerp.normalized, toLerpDist, _collisionLayers);
                    System.Array.Sort(tHits, (a, b) => a.distance.CompareTo(b.distance));

                    foreach (var h in tHits)
                    {
                        if (h.collider.transform.IsChildOf(_target)) continue;
                        if (_ignoreTarget != null && (h.collider.transform == _ignoreTarget || h.collider.transform.IsChildOf(_ignoreTarget))) continue;

                        lerpedPos = h.point + h.normal * 0.1f;
                        break;
                    }
                }

                // Sécurité Min Distance aussi pendant la transition
                float distToHead = Vector3.Distance(lerpedPos, playerOrigin);
                if (distToHead < _minCollisionDistance)
                {
                    lerpedPos = playerOrigin + (lerpedPos - playerOrigin).normalized * _minCollisionDistance;
                }

                transform.position = lerpedPos;
                transform.rotation = Quaternion.Slerp(_transitionStartRot, targetRot, t);
            }
            else
            {
                transform.position = targetPos;
                transform.rotation = targetRot;
            }
        }
    }
}
