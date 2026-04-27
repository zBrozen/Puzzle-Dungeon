using UnityEngine;

namespace PuzzleDungeon.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public enum PlayerState { Idle, Move, Jump, Fall, Land }

        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 10f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _jumpForce = 7f;

        [Header("Auto Jump Settings")]
        [SerializeField] private float _edgeCheckDistance = 0.5f;
        [SerializeField] private float _edgeCheckHeight = 0.1f;
        [SerializeField] private LayerMask _groundLayer;

        private CharacterController _controller;
        private Vector3 _velocity;
        private Vector3 _currentMoveDirection;
        private Vector2 _inputDirection;
        private bool _isGrounded;
        private PlayerState _currentState = PlayerState.Idle;

        public PlayerState CurrentState => _currentState;
        public float MovementSpeed => new Vector2(_controller.velocity.x, _controller.velocity.z).magnitude;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            // Default ground layer to Everything if not set
            if (_groundLayer == 0) _groundLayer = ~0;
        }

        private void Update()
        {
            HandleGroundedStatus();
            HandleMovement();
            HandleAutoJump();
            ApplyGravity();
            UpdateState();
        }

        private void HandleGroundedStatus()
        {
            // On combine la détection native avec un Raycast de sécurité sous les pieds (0.2f de distance)
            bool raycastGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.25f, _groundLayer);
            
            _isGrounded = _controller.isGrounded || raycastGrounded;

            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Force le contact avec le sol
            }
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            _inputDirection = new Vector2(horizontal, vertical);
            
            // On ne change la direction de mouvement QUE si on est au sol
            if (_isGrounded)
            {
                Transform camTransform = Camera.main.transform;
                Vector3 camForward = camTransform.forward;
                Vector3 camRight = camTransform.right;
                
                camForward.y = 0;
                camRight.y = 0;
                camForward.Normalize();
                camRight.Normalize();

                _currentMoveDirection = (camForward * vertical + camRight * horizontal).normalized;
            }

            if (_currentMoveDirection.magnitude >= 0.01f)
            {
                // Si on est en auto-saut, on utilise la vitesse calculée directement
                // Sinon, on utilise la vitesse de mouvement normale
                float speed = _isAutoJumpingToTarget ? 1f : (_isGrounded ? _moveSpeed : _moveSpeed * 0.8f); 
                _controller.Move(_currentMoveDirection * speed * Time.deltaTime);

                // Rotation : on ne tourne que si on est au sol (ou très peu en l'air)
                if (_isGrounded)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(_currentMoveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
                }
            }
        }

        private Vector3 _autoJumpTarget;
        private bool _isAutoJumpingToTarget;

        public void TriggerJump()
        {
            if (!_isGrounded) return;
            
            _velocity.y = Mathf.Sqrt(_jumpForce * -2f * _gravity);
            _currentState = PlayerState.Jump;
        }

        public void JumpTo(Vector3 targetPosition)
        {
            if (!_isGrounded || _isAutoJumpingToTarget) return;

            _autoJumpTarget = targetPosition;
            _isAutoJumpingToTarget = true;
            
            // On déclenche le saut normal
            TriggerJump();

            // On calcule la vitesse horizontale nécessaire pour atteindre la cible
            // Temps de vol = 2 * (vy / g)
            float vy = _velocity.y;
            float timeToLand = 2f * (vy / -_gravity);

            Vector3 diff = targetPosition - transform.position;
            diff.y = 0;
            
            // On remplace la direction actuelle par celle vers la cible
            _currentMoveDirection = diff / timeToLand; 
        }

        private void HandleAutoJump()
        {
            if (_isAutoJumpingToTarget)
            {
                // On calcule la distance horizontale restante
                Vector3 horizontalPos = new Vector3(transform.position.x, 0, transform.position.z);
                Vector3 horizontalTarget = new Vector3(_autoJumpTarget.x, 0, _autoJumpTarget.z);
                
                float distance = Vector3.Distance(horizontalPos, horizontalTarget);
                
                if (distance < 0.05f || _isGrounded && _velocity.y < 0)
                {
                    _isAutoJumpingToTarget = false;
                    _currentMoveDirection = Vector3.zero;
                }
                else
                {
                    // On force la direction vers la cible avec une vitesse proportionnelle
                    // pour arriver exactement au centre.
                    Vector3 direction = (horizontalTarget - horizontalPos).normalized;
                    _currentMoveDirection = direction * (distance / 0.15f); // Ajustement fluide
                }
                return;
            }

            // Auto jump logic: check if there's no ground ahead while moving
            if (!_isGrounded || CurrentState == PlayerState.Jump) return;

            Vector3 moveDirection = new Vector3(_controller.velocity.x, 0, _controller.velocity.z).normalized;
            if (moveDirection.magnitude < 0.1f) return;

            // Cast ray slightly ahead and down
            Vector3 rayStart = transform.position + Vector3.up * _edgeCheckHeight + moveDirection * _edgeCheckDistance;
            
            if (!Physics.Raycast(rayStart, Vector3.down, 1.0f, _groundLayer))
            {
                // No ground ahead! Trigger jump
                TriggerJump();
            }
        }

        private void ApplyGravity()
        {
            _velocity.y += _gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
        }

        private void UpdateState()
        {
            if (_isGrounded)
            {
                if (_inputDirection.magnitude > 0.1f)
                    _currentState = PlayerState.Move;
                else
                    _currentState = PlayerState.Idle;
            }
            else
            {
                if (_velocity.y > 0)
                    _currentState = PlayerState.Jump;
                else
                    _currentState = PlayerState.Fall;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize the edge check ray
            Vector3 moveDirection = transform.forward;
            Vector3 rayStart = transform.position + Vector3.up * _edgeCheckHeight + moveDirection * _edgeCheckDistance;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(rayStart, rayStart + Vector3.down * 1.0f);
        }

        // --- Interaction avec les blocs ---
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;

            // Si on touche un Rigidbody qui n'est pas cinématique
            if (body == null || body.isKinematic) return;

            // On ne pousse pas vers le bas
            if (hit.moveDirection.y < -0.3f) return;

            // Calcul de la direction basée sur la position relative (Joueur -> Bloc)
            // Cela garantit qu'on pousse le bloc "loin de soi" sur l'axe le plus logique
            Vector3 directionToBlock = hit.collider.bounds.center - transform.position;
            directionToBlock.y = 0; // On ignore la hauteur

            Vector3 pushDir = Vector3.zero;

            if (Mathf.Abs(directionToBlock.x) > Mathf.Abs(directionToBlock.z))
            {
                pushDir = new Vector3(directionToBlock.x > 0 ? 1 : -1, 0, 0);
            }
            else
            {
                pushDir = new Vector3(0, 0, directionToBlock.z > 0 ? 1 : -1);
            }

            // On applique la force sur l'axe choisi
            body.linearVelocity = pushDir * (_moveSpeed * 0.5f);
        }
    }
}
