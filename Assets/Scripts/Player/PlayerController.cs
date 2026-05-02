using UnityEngine;

namespace PuzzleDungeon.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public enum PlayerState { Idle, Move, Jump, Fall, Land, Climb }

        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 10f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _jumpForce = 7f;

        [Header("Auto Jump Settings")]
        [SerializeField] private float _edgeCheckDistance = 0.5f;
        [SerializeField] private float _edgeCheckHeight = 0.1f;
        [SerializeField] private float _minRunTimeToJump = 0.3f;
        [SerializeField] private float _jumpObstacleCheckDistance = 1.0f;
        [SerializeField, Range(0.1f, 1.5f), Tooltip("Multiplier for the player radius when checking for gaps. Higher = ignores larger gaps.")] 
        private float _jumpGapCheckRadiusMultiplier = 0.8f;
        [SerializeField] private LayerMask _groundLayer;
        
        [Header("Climbing Settings")]
        [SerializeField] private KeyCode _climbKey = KeyCode.Space;
        [SerializeField] private float _climbCheckDistance = 1.0f;
        [SerializeField] private float _climbMaxHeight = 2.5f;
        [SerializeField] private float _climbLedgeDetectionHeight = 1.2f;
        [SerializeField] private float _climbHorizontalOffset = 0.6f;
        [SerializeField] private float _climbHangOffsetY = 1.8f;
        [SerializeField] private float _climbVerticalOffset = 0f;
        [SerializeField] private LayerMask _climbableLayer;

        [Header("Climbing Durations")]
        [SerializeField] private float _grabDuration = 0.4f;
        [SerializeField] private float _climbWaitTime = 0.2f;
        [SerializeField] private float _ledgeWaitTime = 0.2f;
        [SerializeField] private float _liftDuration = 0.8f;
        [SerializeField] private float _forwardDuration = 0.4f;

        private CharacterController _controller;
        private Vector3 _velocity;
        private Vector3 _currentMoveDirection;
        private Vector2 _inputDirection;
        private bool _isGrounded;
        private float _currentRunTime;
        private PlayerState _currentState = PlayerState.Idle;

        public PlayerState CurrentState => _currentState;
        public float MovementSpeed => new Vector2(_controller.velocity.x, _controller.velocity.z).magnitude;
        public bool IsLocked { get; set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            // Default ground layer to Everything if not set
            if (_groundLayer == 0) _groundLayer = ~0;
            
            // Sécurité : si la nouvelle variable est à 0 (valeur par défaut d'Unity), on lui donne une valeur correcte
            if (_climbHangOffsetY == 0f) _climbHangOffsetY = 1.8f;
        }

        private void Update()
        {
            HandleGroundedStatus();
            
            if (!IsLocked && _currentState != PlayerState.Climb)
            {
                HandleClimbInput(); // Priorité à la grimpe
                
                if (_currentState != PlayerState.Climb)
                {
                    HandleMovement();
                    HandleAutoJump();
                }
            }
            else if (IsLocked)
            {
                _inputDirection = Vector2.zero;
                // On garde la direction actuelle mais on ne bouge plus via l'input
                if (_isGrounded) _currentMoveDirection = Vector3.zero;
            }

            if (_currentState != PlayerState.Climb)
            {
                ApplyGravity();
                UpdateState();
            }
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
                // On incrémente le temps de course si on bouge au sol
                if (_isGrounded) _currentRunTime += Time.deltaTime;

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
            else
            {
                _currentRunTime = 0f;
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

        public void Teleport(Vector3 newPosition, Quaternion newRotation)
        {
            // Il faut désactiver le CharacterController pour modifier le transform directement
            _controller.enabled = false;
            transform.position = newPosition;
            transform.rotation = newRotation;
            Physics.SyncTransforms(); // Assure que la physique est mise à jour instantanément
            _velocity = Vector3.zero;
            _currentMoveDirection = Vector3.zero;
            _isAutoJumpingToTarget = false;
            _currentState = PlayerState.Idle;
            _controller.enabled = true;

            // Fait sauter la caméra instantanément à la nouvelle position
            if (Camera.main != null && Camera.main.TryGetComponent(out CameraController cam))
            {
                cam.SnapToTarget();
            }
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
            if (!_isGrounded || CurrentState == PlayerState.Jump || _currentRunTime < _minRunTimeToJump || _inputDirection.magnitude < 0.1f) return;

            Vector3 moveDirection = new Vector3(_controller.velocity.x, 0, _controller.velocity.z).normalized;
            if (moveDirection.magnitude < 0.1f) return;

            if (Vector3.Dot(moveDirection, _currentMoveDirection) < 0.5f) return;

            // --- GESTION AMELIOREE DES MARCHES ET DU VIDE ---
            LayerMask obstacleMask = _groundLayer | _climbableLayer | 1;

            // 1. Vérifier si un obstacle (mur, bloc, grosse marche) est devant nous.
            // On utilise SphereCastAll pour détecter les murs même en diagonale, tout en ignorant le sol.
            Vector3 waistOrigin = transform.position + Vector3.up * 0.8f; 
            float wallCheckRadius = _controller.radius * 0.8f;
            
            RaycastHit[] wallHits = Physics.SphereCastAll(waistOrigin, wallCheckRadius, moveDirection, _jumpObstacleCheckDistance, obstacleMask);
            foreach (var hit in wallHits)
            {
                // On ignore les collisions avec soi-même (distance 0) et le sol/pentes douces (normale vers le haut)
                if (hit.distance > 0.01f && hit.normal.y < 0.5f)
                {
                    return; // Mur ou gros bloc détecté -> pas d'auto-saut, le CharacterController va buter contre.
                }
            }

            // 2. Évaluer la présence d'un vide (trou) devant nous
            // On utilise un Raycast tombant de haut pour éviter qu'il ne commence "à l'intérieur" d'une petite marche
            float castHeight = 1.5f; 
            Vector3 rayStart = transform.position + Vector3.up * castHeight + moveDirection * _edgeCheckDistance;
            
            bool isGap = false;
            
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, castHeight + 1.0f, obstacleMask))
            {
                float heightDiff = groundHit.point.y - transform.position.y;
                // On ne saute que si la différence de hauteur est importante (gouffre)
                if (heightDiff < -0.8f) 
                {
                    isGap = true;
                }
            }
            else
            {
                isGap = true; // Aucun sol touché (gouffre très profond)
            }

            // 3. Double vérification pour ignorer les micro-fissures (ex: planches d'un pont)
            if (isGap)
            {
                Vector3 rayStartBehind = rayStart - moveDirection * 0.2f; // On recule légèrement le rayon
                if (Physics.Raycast(rayStartBehind, Vector3.down, out RaycastHit hitBehind, castHeight + 1.0f, obstacleMask))
                {
                    if (hitBehind.point.y >= transform.position.y - 0.8f)
                    {
                        isGap = false; // C'était juste une micro-fissure, le sol plat continue derrière
                    }
                }
            }

            if (isGap)
            {
                TriggerJump();
            }
        }

        private void HandleClimbInput()
        {
            // Pour éviter les bugs où le CharacterController se croit au sol sur une micro-corniche du mur,
            // on vérifie avec un Raycast strict vers le bas.
            bool trueGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f, _groundLayer);

            // On peut grimper si :
            // 1. On appuie sur la touche (manuel, souvent utilisé au sol)
            // 2. On n'est pas "vraiment" au sol (automatique, pour attraper un rebord pendant un saut/chute)
            bool shouldCheck = Input.GetKeyDown(_climbKey) || !trueGrounded;

            if (shouldCheck)
            {
                if (CheckForClimbableLedge(out Vector3 targetPos, out Vector3 wallNormal))
                {
                    StartCoroutine(ClimbRoutine(targetPos, wallNormal));
                }
            }
        }

        private bool CheckForClimbableLedge(out Vector3 targetPos, out Vector3 wallNormal)
        {
            targetPos = Vector3.zero;
            wallNormal = -transform.forward; // Normale par défaut si on ne trouve pas la face du mur
            Vector3 direction = transform.forward;
            Vector3 flatDirection = new Vector3(direction.x, 0, direction.z).normalized;
            
            // On teste plusieurs distances en avant du joueur pour trouver le rebord
            float[] forwardDistances = { 0.5f, 0.75f, 1.0f };
            bool foundLedge = false;
            
            foreach (float dist in forwardDistances)
            {
                // On part du haut (hauteur max) et on descend directement
                Vector3 checkPoint = transform.position + Vector3.up * _climbMaxHeight + flatDirection * dist;
                
                if (Physics.Raycast(checkPoint, Vector3.down, out RaycastHit ledgeHit, _climbMaxHeight + 0.5f, _climbableLayer))
                {
                    foundLedge = true;
                    float height = ledgeHit.point.y - transform.position.y;
                    
                    // Si on a trouvé un rebord à une hauteur valide
                    if (height > 0.5f && height <= _climbMaxHeight)
                    {
                        // On vérifie qu'il n'y a pas de plafond au-dessus du rebord pour se tenir debout
                        if (!Physics.Raycast(ledgeHit.point + Vector3.up * 0.1f, Vector3.up, 1.8f, _groundLayer | _climbableLayer))
                        {
                            targetPos = ledgeHit.point;
                            
                            // On cherche la normale exacte du mur pour que le personnage s'aligne bien
                            // On tire un rayon depuis le rebord vers le joueur (légèrement en dessous du rebord)
                            Vector3 wallCheckStart = ledgeHit.point + Vector3.down * 0.2f + flatDirection * 0.2f;
                            if (Physics.Raycast(wallCheckStart, -flatDirection, out RaycastHit wallHit, 1.0f, _climbableLayer))
                            {
                                wallNormal = wallHit.normal;
                            }
                            
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        private System.Collections.IEnumerator ClimbRoutine(Vector3 targetPos, Vector3 wallNormal)
        {
            _currentState = PlayerState.Climb;
            _velocity = Vector3.zero;
            
            Debug.Log($"[Climb] Start. Target Ledge Y: {targetPos.y} | Player Y: {transform.position.y}");

            // On désactive le controller pour manipuler le transform librement
            _controller.enabled = false;

            // Orientation du joueur face au mur
            Vector3 inwardDir = -wallNormal;
            inwardDir.y = 0;
            inwardDir.Normalize();
            Quaternion targetRotation = Quaternion.LookRotation(inwardDir);

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            
            // On se place à une distance de sécurité du mur en utilisant la NORMALE du mur
            Vector3 grabPos = new Vector3(targetPos.x, 0, targetPos.z) + wallNormal * _climbHorizontalOffset;
            
            // Ajustement précis de la hauteur pour correspondre à l'animation d'accroche
            grabPos.y = targetPos.y - _climbHangOffsetY;
            
            // Phase 1 : Accroche (Rapide déplacement vers le mur et rotation)
            float elapsed = 0;
            while (elapsed < _grabDuration)
            {
                float t = elapsed / _grabDuration;
                transform.position = Vector3.Lerp(startPos, grabPos, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRotation, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = grabPos;
            transform.rotation = targetRotation;
            
            // Attente pour laisser l'animation se jouer
            yield return new WaitForSeconds(_climbWaitTime);

            // Phase 2 : Montée (Décomposition en Elévation puis Avancée)
            Vector3 endPos = targetPos + Vector3.up * 0.05f; 
            Vector3 horizontalPos = new Vector3(targetPos.x, 0, targetPos.z) + wallNormal * _climbHorizontalOffset;
            Vector3 peakPos = new Vector3(horizontalPos.x, endPos.y + _climbVerticalOffset, horizontalPos.z);

            Debug.Log($"[Climb] Phase 2. Peak Y: {peakPos.y} | End Y: {endPos.y}");

            // 2a : Elévation (Verticale)
            elapsed = 0;
            while (elapsed < _liftDuration)
            {
                float t = elapsed / _liftDuration;
                float smoothT = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(grabPos, peakPos, smoothT);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = peakPos;

            // Pause au sommet du rebord avant d'avancer
            yield return new WaitForSeconds(_ledgeWaitTime);

            // 2b : Avancée (Horizontale uniquement)
            elapsed = 0;
            Vector3 horizontalEndPos = new Vector3(endPos.x, peakPos.y, endPos.z);
            while (elapsed < _forwardDuration)
            {
                float t = elapsed / _forwardDuration;
                float smoothT = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(peakPos, horizontalEndPos, smoothT);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = endPos;

            _controller.enabled = true;
            _currentState = PlayerState.Idle;
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
                // Si on vient de tomber ou de sauter et qu'on touche le sol, on réinitialise le temps de course
                if (_currentState == PlayerState.Fall || _currentState == PlayerState.Jump)
                {
                    _currentRunTime = 0f;
                }

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

            // Visualize climb detection
            Gizmos.color = Color.blue;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Gizmos.DrawRay(origin, transform.forward * _climbCheckDistance);
            
            Gizmos.color = Color.cyan;
            Vector3 highOrigin = transform.position + Vector3.up * _climbMaxHeight;
            Gizmos.DrawRay(highOrigin, transform.forward * _climbCheckDistance);

            // Visualisation du point cible détecté
            if (CheckForClimbableLedge(out Vector3 target, out Vector3 wallNormal))
            {
                // Haut du rebord (vert)
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(target, 0.15f);

                // Position finale du personnage (grabPos)
                Vector3 grabPos = new Vector3(target.x, target.y - _climbHangOffsetY, target.z) + wallNormal * _climbHorizontalOffset;
                
                // Dessiner une ligne entre le rebord et le personnage
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(target, grabPos);

                // Dessiner une "boîte" approximative représentant le joueur (hauteur ~2 unités, pivot aux pieds)
                Gizmos.DrawWireCube(grabPos + Vector3.up * 1.0f, new Vector3(0.6f, 2.0f, 0.6f));
                
                // Dessiner une petite sphère rouge pour les pieds (le pivot exact)
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(grabPos, 0.1f);
            }
        }

        // --- Interaction avec les blocs ---
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (_currentState == PlayerState.Climb) return;

            Rigidbody body = hit.collider.attachedRigidbody;

            // Si on touche un Rigidbody qui n'est pas cinématique
            if (body == null || body.isKinematic) return;

            // On ne pousse pas vers le bas
            if (hit.moveDirection.y < -0.3f) return;

            // Calcul de la direction basée sur la position relative (Joueur -> Bloc)
            // Cela garantit qu'on pousse le bloc "loin de soi" sur l'axe le plus logique
            Vector3 directionToBlock = hit.transform.position - transform.position;
            directionToBlock.y = 0; // On ignore la hauteur

            // Conversion de la direction en espace local du bloc pour respecter sa rotation
            Vector3 localDir = hit.transform.InverseTransformDirection(directionToBlock);

            Vector3 localPushDir = Vector3.zero;

            // On trouve l'axe local le plus fort
            if (Mathf.Abs(localDir.x) > Mathf.Abs(localDir.z))
            {
                localPushDir = new Vector3(localDir.x > 0 ? 1 : -1, 0, 0);
            }
            else
            {
                localPushDir = new Vector3(0, 0, localDir.z > 0 ? 1 : -1);
            }

            // On repasse en espace global
            Vector3 pushDir = hit.transform.TransformDirection(localPushDir);
            pushDir.y = 0; // Sécurité
            pushDir.Normalize();

            // On applique la force sur l'axe choisi
            body.linearVelocity = pushDir * (_moveSpeed * 0.5f);
        }
    }
}
