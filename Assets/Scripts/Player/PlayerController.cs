using UnityEngine;
using PuzzleDungeon.Interactions;
using System;

namespace PuzzleDungeon.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public event Action OnDrawWeapon;
        public event Action OnSheatheWeapon;
        public event Action<int> OnAttackAction;
        
        public enum PlayerState { Idle, Move, Jump, Fall, Land, Climb, Push, BigPush, HardLand, Roll, Attack, Hurt, Treasure }

        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 10f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _jumpForce = 7f;
        [SerializeField, Tooltip("Multiplicateur de vitesse horizontale en l'air (pour saut en longueur)")]
        private float _jumpForwardSpeedMultiplier = 1.2f;
        [SerializeField, Tooltip("Hauteur max des marches franchissables au sol")] 
        private float _stepHeight = 0.3f;

        [Header("Hard Landing Settings")]
        [SerializeField, Tooltip("Hauteur de chute minimale pour déclencher la roulade d'atterrissage")]
        private float _hardLandHeightThreshold = 5f;
        [SerializeField, Tooltip("Durée de l'état HardLand (temps de la roulade)")]
        private float _hardLandDuration = 1.0f;

        [Header("Roll Settings")]
        [SerializeField, Tooltip("Durée de la roulade (en secondes)")]
        private float _rollDuration = 0.5f;
        [SerializeField, Tooltip("Multiplicateur de vitesse pendant la roulade")]
        private float _rollSpeedMultiplier = 2.0f;
        [SerializeField, Tooltip("Fenêtre de temps après une roulade pour obtenir le bonus de saut (en secondes)")]
        private float _rollJumpBoostWindow = 0.4f;
        [SerializeField, Tooltip("Multiplicateur de jumpForwardSpeed supplémentaire si on saute juste après une roulade")]
        private float _rollJumpBoostMultiplier = 1.3f;
        [SerializeField, Tooltip("Temps de recharge entre deux roulades (en secondes)")]
        private float _rollCooldown = 0.8f;
        [SerializeField, Tooltip("Durée de la décélération progressive après la roulade (en secondes)")]
        private float _rollDecelerationDuration = 0.2f;

        [Header("Weapon & Combat Settings")]
        [SerializeField] private KeyCode _attackKey = KeyCode.Mouse0;
        [SerializeField, Tooltip("Temps avant de rengainer automatiquement")] private float _autoSheatheDelay = 5f;
        [SerializeField, Tooltip("L'épée attachée à la main")] private GameObject _swordInHand;
        [SerializeField, Tooltip("L'épée attachée dans le dos ou à la ceinture (optionnel)")] private GameObject _swordOnBack;

        [Header("Attack Combo Settings")]
        [SerializeField, Tooltip("Nombre maximum de coups dans le combo")] private int _maxComboStep = 3;
        [SerializeField, Tooltip("Temps maximum entre deux attaques pour continuer le combo")] private float _comboWindowDuration = 0.8f;
        [SerializeField, Tooltip("Durée de l'attaque bloquant les autres actions")] private float _attackDuration = 0.4f;
        [SerializeField, Tooltip("Force de propulsion vers l'avant lors d'une attaque")] private float _attackForwardBoost = 4.0f;
        [SerializeField, Tooltip("Délai pour laisser l'animation de dégainage jouer avant d'attaquer automatiquement")] private float _drawToAttackDelay = 0.2f;

        [Header("Attack Damage Settings")]
        [SerializeField, Tooltip("Dégâts infligés par l'épée")] private int _attackDamage = 1;
        [SerializeField, Tooltip("Portée de l'attaque devant le joueur")] private float _attackHitRange = 1.5f;
        [SerializeField, Tooltip("Largeur de la zone d'impact (rayon)")] private float _attackHitRadius = 1.0f;
        [SerializeField, Tooltip("Layer contenant les ennemis")] private LayerMask _enemyLayer;
        
        [Header("Damage & Stun Settings")]
        [SerializeField, Tooltip("Durée d'immobilisation après avoir pris un coup")] 
        private float _hurtStunDuration = 0.5f;

        [Header("Lantern Settings")]
        [SerializeField, Tooltip("La lanterne dans la main gauche")] private GameObject _lanternInHand;
        [SerializeField, Tooltip("La lanterne accrochée à la ceinture/sac (optionnel)")] private GameObject _lanternOnBelt;

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
        [SerializeField, Tooltip("Hauteur max attrapable depuis le sol")] private float _climbMaxHeight = 2.5f;
        [SerializeField, Tooltip("Hauteur max attrapable pendant un saut/chute")] private float _airClimbMaxHeight = 1.8f;
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
        
        [SerializeField] private float _pushStrength = 0.5f;

        [Header("Interaction Settings")]
        [SerializeField] private KeyCode _interactKey = KeyCode.F;
        [SerializeField] private float _interactRange = 2f;
        [SerializeField] private LayerMask _interactableLayer;

        private CharacterController _controller;
        private Vector3 _velocity;
        private Vector3 _currentMoveDirection;
        private Vector2 _inputDirection;
        private bool _isGrounded;
        private float _pushTimer;
        private float _currentRunTime;
        private PlayerState _currentState = PlayerState.Idle;
        private bool _isWeaponDrawn = false;
        private float _sheatheTimer = 0f;
        private bool _isLanternDrawn = false;
        private float _fallPeakY;
        private float _rollJumpBoostTimer = 0f;  // Temps restant dans la fenêtre de boost de saut post-roulade
        private float _rollCooldownTimer = 0f;   // Temps restant avant de pouvoir rouler à nouveau
        private Vector3 _rollDirection;          // Direction de la dernière roulade (pour le saut post-roulade)
        private float _rollDecelerationTimer = 0f; // Temps restant dans la phase de décélération post-roulade
        private float _lastClimbTime;
        [SerializeField, Tooltip("Temps de recharge entre deux grimpes (évite les répétitions accidentelles)")]
        private float _climbCooldown = 0.5f;

        private int _currentComboStep = 0;
        private float _comboTimer = 0f;
        private Coroutine _attackCoroutine;
        private Coroutine _hurtCoroutine;
        private bool _isDrawingWeapon = false;
        private PlayerHealth _playerHealth;
        private PlayerInventory _inventory;

        public PlayerState CurrentState => _currentState;
        public float MovementSpeed => new Vector2(_controller.velocity.x, _controller.velocity.z).magnitude;
        public bool IsLocked { get; set; }
        public bool IsWeaponDrawn => _isWeaponDrawn;
        public bool IsLanternDrawn => _isLanternDrawn;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            // Default ground layer to Everything if not set
            if (_groundLayer == 0) _groundLayer = ~0;
            
            if (_interactableLayer == 0)
            {
                Debug.LogWarning("[PlayerController] Interactable Layer is not set! Interactions with chests will not work.");
            }
            
            // Sécurité : si la nouvelle variable est à 0 (valeur par défaut d'Unity), on lui donne une valeur correcte
            if (_climbHangOffsetY == 0f) _climbHangOffsetY = 1.8f;

            _controller.stepOffset = _stepHeight;

            // Par défaut, l'arme est rengainée au démarrage
            if (_swordInHand != null) _swordInHand.SetActive(false);
            if (_swordOnBack != null) _swordOnBack.SetActive(true);

            // Par défaut, la lanterne est dégainée
            _isLanternDrawn = true;
            if (_lanternInHand != null) _lanternInHand.SetActive(true);
            if (_lanternOnBelt != null) _lanternOnBelt.SetActive(false);

            _playerHealth = GetComponent<PlayerHealth>();
            _inventory = GetComponent<PlayerInventory>();
            
            if (_playerHealth != null) _playerHealth.OnTakeDamage += HandleTakeDamage;
            if (_inventory != null) _inventory.OnInventoryChanged += RefreshVisualItems;
        }

        private void Start()
        {
            RefreshVisualItems();
        }

        private void OnDisable()
        {
            if (_playerHealth != null) _playerHealth.OnTakeDamage -= HandleTakeDamage;
            if (_inventory != null) _inventory.OnInventoryChanged -= RefreshVisualItems;
        }

        private void Update()
        {
            // Permet l'ajustement en temps réel de la hauteur de marche depuis l'éditeur
            if (_controller != null && Mathf.Abs(_controller.stepOffset - _stepHeight) > 0.001f)
            {
                _controller.stepOffset = _stepHeight;
            }

            HandleGroundedStatus();

            // Décrémenter les timers de roulade
            if (_rollJumpBoostTimer > 0f)
                _rollJumpBoostTimer -= Time.deltaTime;
            if (_rollCooldownTimer > 0f)
                _rollCooldownTimer -= Time.deltaTime;
            if (_rollDecelerationTimer > 0f)
                _rollDecelerationTimer -= Time.deltaTime;
            
            // Décrémenter le timer de combo
            if (_comboTimer > 0f)
            {
                _comboTimer -= Time.deltaTime;
                if (_comboTimer <= 0f)
                    _currentComboStep = 0;
            }
            
            if (!IsLocked && _currentState != PlayerState.Climb && _currentState != PlayerState.Hurt)
            {
                HandleClimbInput(); // Priorité à la grimpe
                
                if (_currentState != PlayerState.Climb)
                {
                    HandleRollInput(); // Roulade (si pas de rebord à attraper)
                    HandleMovement();
                    HandleAutoJump();
                    HandleWeapon();
                    HandleInteraction();
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

            // Empêche de rester sur la tête des ennemis
            HandleEnemySliding();

            // On vérifie l'état de la lanterne à chaque frame en fonction du PlayerState
            HandleLantern();
        }

        private void HandleGroundedStatus()
        {
            // Détection pour l'état visuel (animations) : on veut rester "grounded" sur les petites marches
            // pour éviter de déclencher l'anim de chute à chaque petit dénivelé.
            // On inclut _climbableLayer pour éviter de passer en état "Fall" quand on est sur un rebord.
            float visualCheckDist = (_velocity.y > 0.1f) ? 0.2f : (_stepHeight + 0.15f);
            bool raycastVisualGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, visualCheckDist, _groundLayer | _climbableLayer);
            
            // Détection pour la physique : on ne reset la vélocité que si on est réellement sur un sol horizontal.
            // On ignore le isGrounded du controller si on tombe vite (évite de considérer les murs comme du sol).
            bool raycastPhysicalGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.25f, _groundLayer | _climbableLayer);
            bool isPhysicallyGrounded = raycastPhysicalGrounded || (_controller.isGrounded && _velocity.y > -1.0f);

            _isGrounded = isPhysicallyGrounded || raycastVisualGrounded;

            if (!_isGrounded)
            {
                _currentRunTime = 0f;
            }

            if (isPhysicallyGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Force le contact avec le sol seulement quand on est très proche
            }
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            _inputDirection = new Vector2(horizontal, vertical);
            
            if (_currentState == PlayerState.Attack) return;

            Transform camTransform = Camera.main.transform;
            Vector3 camForward = camTransform.forward;
            Vector3 camRight = camTransform.right;
            
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 targetDir = (camForward * vertical + camRight * horizontal).normalized;

            // On ne change la direction de mouvement QUE si on est au sol ET qu'on ne roule/saute/tombe pas
            // (pendant la roulade, le saut ou la chute, la direction est verrouillée pour simuler l'absence de air control)
            if (_isGrounded && _currentState != PlayerState.Roll && _currentState != PlayerState.Jump && _currentState != PlayerState.Fall)
            {
                _currentMoveDirection = targetDir;
            }

            if (_currentMoveDirection.magnitude >= 0.01f)
            {
                // On incrémente le temps de course si on bouge au sol
                if (_isGrounded) _currentRunTime += Time.deltaTime;

                // Si on est en auto-saut, on utilise la vitesse calculée directement
                // Sinon, on utilise la vitesse normale — boostée pendant la roulade au sol,
                // ou décélération progressive après la roulade,
                // ou boostée en l'air si on a roulé juste avant de sauter.
                float groundMultiplier;
                if (_currentState == PlayerState.Roll)
                {
                    groundMultiplier = _rollSpeedMultiplier;
                }
                else if (_rollDecelerationTimer > 0f)
                {
                    float t = _rollDecelerationTimer / _rollDecelerationDuration;
                    groundMultiplier = Mathf.Lerp(1f, _rollSpeedMultiplier, t);
                }
                else
                {
                    groundMultiplier = 1f;
                }
                float groundSpeed = _moveSpeed * groundMultiplier;
                float airMultiplier = _jumpForwardSpeedMultiplier * (_rollJumpBoostTimer > 0f ? _rollJumpBoostMultiplier : 1f);
                float speed = _isAutoJumpingToTarget ? 1f : (_isGrounded ? groundSpeed : _moveSpeed * airMultiplier);
                _controller.Move(_currentMoveDirection * speed * Time.deltaTime);

                // Rotation : on ne tourne que si on est au sol et qu'on a le contrôle
                if (_isGrounded && _currentState != PlayerState.Roll && _currentState != PlayerState.Jump && _currentState != PlayerState.Fall)
                {
                    if (_currentMoveDirection.magnitude >= 0.01f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(_currentMoveDirection);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
                    }
                }
            }
            else
            {
                _currentRunTime = 0f;
            }
        }

        private void HandleEnemySliding()
        {
            // On ne glisse que si on est au sol (sur l'ennemi)
            if (!_isGrounded) return;

            // Raycast vers le bas pour détecter si on marche sur un ennemi
            // On commence un peu plus haut pour être sûr de traverser le pied du joueur
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 0.8f, _enemyLayer))
            {
                // Direction d'éjection : du centre de l'ennemi vers le joueur
                Vector3 pushDir = transform.position - hit.transform.position;
                pushDir.y = 0;
                
                // Si on est pile au milieu, on choisit une direction par défaut
                if (pushDir.magnitude < 0.01f) pushDir = transform.forward;
                
                pushDir.Normalize();

                // On applique un mouvement forcé vers l'extérieur
                _controller.Move(pushDir * 5f * Time.deltaTime);
            }
        }

        private void HandleWeapon()
        {
            // On ne peut dégainer que si on possède l'épée dans l'inventaire
            if (_inventory != null && !_inventory.HasItem("Sword")) return;

            if (Input.GetKeyDown(_attackKey))
            {
                if (!_isWeaponDrawn)
                {
                    if (!_isDrawingWeapon)
                    {
                        StartCoroutine(DrawAndAttackRoutine());
                    }
                }
                else
                {
                    TriggerAttack();
                }
            }

            // Gestion du timer de rengainement automatique
            if (_isWeaponDrawn && _currentState != PlayerState.Attack && !_isDrawingWeapon)
            {
                _sheatheTimer -= Time.deltaTime;
                if (_sheatheTimer <= 0f)
                {
                    SheatheWeapon();
                }
            }
        }

        private void HandleInteraction()
        {
            if (Input.GetKeyDown(_interactKey))
            {
                Debug.Log($"[Interaction] Key {_interactKey} pressed. Range: {_interactRange}, Layer: {_interactableLayer.value}");
                
                // On utilise un SphereCast plutôt qu'un Raycast pour être plus tolérant sur la visée
                Vector3 origin = transform.position + Vector3.up * 1f;
                float radius = 0.5f;

                if (Physics.SphereCast(origin, radius, transform.forward, out RaycastHit hit, _interactRange, _interactableLayer))
                {
                    Debug.Log($"[Interaction] Hit: {hit.collider.name} on layer {hit.collider.gameObject.layer}");
                    
                    if (hit.collider.TryGetComponent(out Chest chest))
                    {
                        Debug.Log("[Interaction] Chest component found, opening...");
                        chest.Open(this);
                    }
                    else if (hit.collider.GetComponentInParent<Chest>())
                    {
                        Debug.Log("[Interaction] Chest component found in parent, opening...");
                        hit.collider.GetComponentInParent<Chest>().Open(this);
                    }
                    else
                    {
                        Debug.Log("[Interaction] No Chest component found on hit object.");
                    }
                }
                else
                {
                    Debug.Log("[Interaction] Raycast/SphereCast hit nothing. Check if the Chest is on the correct Layer and has a Collider.");
                }
            }
        }

        private System.Collections.IEnumerator DrawAndAttackRoutine()
        {
            _isDrawingWeapon = true;
            DrawWeapon();
            
            // Laisse un court délai pour l'animation de dégainage (UpperBody)
            yield return new WaitForSeconds(_drawToAttackDelay);
            
            // Sécurité : on s'assure que l'épée est bien dans la main 
            // (au cas où l'animation event n'a pas eu le temps de se déclencher)
            AnimEvent_GrabSword();
            
            _isDrawingWeapon = false;
            
            // Déclenche l'attaque automatiquement après avoir dégainé
            TriggerAttack();
        }

        public void DrawWeapon()
        {
            if (_isWeaponDrawn) return;

            _isWeaponDrawn = true;
            _sheatheTimer = _autoSheatheDelay;
            
            OnDrawWeapon?.Invoke();
            
            // Switch basique des GameObjects (si vous n'utilisez pas d'Animation Events)
            // if (_swordInHand != null) _swordInHand.SetActive(true);
            if (_swordOnBack != null) _swordOnBack.SetActive(false);
        }

        public void SheatheWeapon()
        {
            if (!_isWeaponDrawn) return;

            _isWeaponDrawn = false;
            
            OnSheatheWeapon?.Invoke();

            // On force toujours le swap des GameObjects au rengainement
            // (même si l'anim est skip, ex: saut, l'épée doit disparaître de la main)
            if (_swordInHand != null) _swordInHand.SetActive(false);
            if (_swordOnBack != null) _swordOnBack.SetActive(true);
        }

        // --- Méthodes appelées par les Animation Events ---
        // Optionnel : À utiliser si vous configurez des événements dans vos animations
        public void AnimEvent_GrabSword()
        {
            if (_swordInHand != null) _swordInHand.SetActive(true);
            if (_swordOnBack != null) _swordOnBack.SetActive(false);
        }

        public void AnimEvent_StoreSword()
        {
            if (_swordInHand != null) _swordInHand.SetActive(false);
            if (_swordOnBack != null) _swordOnBack.SetActive(true);
        }

        // Appelé via un Animation Event (ou via le pont PlayerAnimator)
        public void AnimEvent_DealDamage()
        {
            // Position approximative devant le joueur pour la zone d'impact
            Vector3 hitCenter = transform.position + Vector3.up * 1f + transform.forward * (_attackHitRange / 2f);
            
            Collider[] hits = Physics.OverlapSphere(hitCenter, _attackHitRadius, _enemyLayer);
            
            // DEBUG: Décommentez pour voir si la fonction est bien appelée
            // Debug.Log($"[Combat] Coup porté ! Objets détectés dans la zone : {hits.Length}");

            foreach (var hit in hits)
            {
                // Vérifier si la cible possède le script EnemyHealth
                if (hit.TryGetComponent(out PuzzleDungeon.Enemies.EnemyHealth enemyHealth))
                {
                    enemyHealth.TakeDamage(_attackDamage);
                    Debug.Log($"[Combat] Ennemi touché : {hit.name}");
                }
            }
        }

        private void HandleLantern()
        {
            // La lanterne est maintenant toujours présente par défaut
            // Elle est dégainée quand on est au sol et libre de nos mains
            bool canHoldLantern = (_currentState == PlayerState.Idle || _currentState == PlayerState.Move);
            
            if (canHoldLantern && !_isLanternDrawn)
            {
                DrawLantern();
            }
            else if (!canHoldLantern && _isLanternDrawn)
            {
                SheatheLantern();
            }
        }

        public void DrawLantern()
        {
            _isLanternDrawn = true;
            if (_lanternInHand != null) _lanternInHand.SetActive(true);
            if (_lanternOnBelt != null) _lanternOnBelt.SetActive(false);
        }

        public void SheatheLantern()
        {
            _isLanternDrawn = false;
            if (_lanternInHand != null) _lanternInHand.SetActive(false);
            if (_lanternOnBelt != null) _lanternOnBelt.SetActive(true);
        }

        private void RefreshVisualItems()
        {
            if (_inventory == null) return;

            bool hasSword = _inventory.HasItem("Sword");

            // Si on n'a plus l'épée, on éteint tout ce qui y touche
            if (!hasSword)
            {
                if (_swordInHand != null) _swordInHand.SetActive(false);
                if (_swordOnBack != null) _swordOnBack.SetActive(false);
                _isWeaponDrawn = false;
            }
            else
            {
                // Si on a l'épée mais qu'elle n'est pas dégainée, on l'affiche sur le dos
                if (!_isWeaponDrawn && _swordOnBack != null) _swordOnBack.SetActive(true);
            }

            // La lanterne est toujours visible (soit main, soit ceinture)
            if (!_isLanternDrawn && _lanternOnBelt != null) _lanternOnBelt.SetActive(true);
        }

        public void TriggerAttack()
        {
            if (!_isGrounded) return;
            if (_currentState == PlayerState.Climb || _currentState == PlayerState.Roll || _currentState == PlayerState.HardLand || _currentState == PlayerState.Push || _currentState == PlayerState.BigPush) return;
            
            // Si on a déjà atteint la fin du combo et qu'on est encore en train d'attaquer, on ignore l'input pour l'instant
            if (_currentState == PlayerState.Attack && _currentComboStep >= _maxComboStep) return;

            _sheatheTimer = _autoSheatheDelay; // reset timer

            if (_comboTimer > 0f)
            {
                _currentComboStep++;
                if (_currentComboStep > _maxComboStep) _currentComboStep = 1;
            }
            else
            {
                _currentComboStep = 1;
            }

            _comboTimer = _comboWindowDuration;

            if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
            _attackCoroutine = StartCoroutine(AttackRoutine());
        }

        private System.Collections.IEnumerator AttackRoutine()
        {
            _currentState = PlayerState.Attack;
            
            // Si on veut taper dans la direction du mouvement courant au sol
            Vector3 attackDir = transform.forward;
            if (_inputDirection.magnitude > 0.1f)
            {
                attackDir = _currentMoveDirection;
                transform.rotation = Quaternion.LookRotation(attackDir);
            }

            OnAttackAction?.Invoke(_currentComboStep);

            float elapsed = 0f;
            while(elapsed < _attackDuration)
            {
                // Propulsion uniquement sur la première moitié de l'attaque
                if (elapsed < _attackDuration * 0.4f)
                {
                    _controller.Move(attackDir * _attackForwardBoost * Time.deltaTime);
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_currentState == PlayerState.Attack)
            {
                _currentState = PlayerState.Idle;
            }
        }

        private Vector3 _autoJumpTarget;
        private bool _isAutoJumpingToTarget;

        public void SetState(PlayerState newState)
        {
            _currentState = newState;
        }

        public void TriggerJump()
        {
            if (!_isGrounded) return;
            
            if (_isWeaponDrawn) SheatheWeapon(); // Force le rengainement
            
            _fallPeakY = transform.position.y;
            _velocity.y = Mathf.Sqrt(_jumpForce * -2f * _gravity);
            _currentState = PlayerState.Jump;

            // Si on saute pendant la fenêtre post-roulade (ou pendant la roulade) sans input,
            // on s'assure d'avoir la direction de la roulade.
            if (_rollJumpBoostTimer > 0f && _inputDirection.magnitude < 0.1f)
                _currentMoveDirection = _rollDirection;
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
            _fallPeakY = transform.position.y; // Reset de la hauteur de chute
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
            // Pendant une roulade, on ignore les checks d'input et de run time car
            // c'est la roulade elle-même qui fournit le déplacement (y compris depuis l'arrêt).
            bool isRolling = CurrentState == PlayerState.Roll;
            if (!_isGrounded || CurrentState == PlayerState.Jump) return;
            if (!isRolling && (_currentRunTime < _minRunTimeToJump || _inputDirection.magnitude < 0.1f)) return;

            Vector3 moveDirection = isRolling
                ? _currentMoveDirection.normalized  // pendant la roulade, on utilise la direction forcée
                : new Vector3(_controller.velocity.x, 0, _controller.velocity.z).normalized;
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
            // On peut grimper si :
            // 1. On n'est pas déjà en train de grimper (sécurité)
            // 2. Le cooldown est passé
            if (Time.time - _lastClimbTime < _climbCooldown) return;

            // Pour éviter les bugs où le CharacterController se croit au sol sur une micro-corniche du mur,
            // on vérifie avec un Raycast strict vers le bas. On inclut la couche climbable.
            bool trueGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f, _groundLayer | _climbableLayer);

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

        private void HandleRollInput()
        {
            // La roulade ne se déclenche qu'au sol, sur pression de Space, et hors cooldown
            if (!_isGrounded || !Input.GetKeyDown(_climbKey)) return;
            if (_currentState == PlayerState.HardLand || _currentState == PlayerState.Push || _currentState == PlayerState.BigPush) return;
            if (_rollCooldownTimer > 0f) return;

            // La roulade s'effectue dans la direction courante (ou vers l'avant si on est à l'arrêt)
            Vector3 rollDirection = _currentMoveDirection.magnitude >= 0.1f ? _currentMoveDirection : transform.forward;
            StartCoroutine(RollRoutine(rollDirection));
        }

        private System.Collections.IEnumerator RollRoutine(Vector3 direction)
        {
            _currentState = PlayerState.Roll;

            if (_isWeaponDrawn) SheatheWeapon();

            // Stocker et appliquer la direction de roulade.
            _rollDirection = direction;
            _currentMoveDirection = direction;

            // Démarrer le boost DÈS le début pour couvrir le saut pendant ET après la roulade.
            _rollJumpBoostTimer = _rollDuration + _rollJumpBoostWindow;

            yield return new WaitForSeconds(_rollDuration);

            _rollCooldownTimer = _rollCooldown;
            _rollDecelerationTimer = _rollDecelerationDuration;

            // On ne nettoie la direction que si on est encore dans l'état Roll
            // (si on a sauté entre temps, on laisse le saut gérer sa direction)
            if (_currentState == PlayerState.Roll)
            {
                if (_isGrounded && _inputDirection.magnitude < 0.1f)
                    _currentMoveDirection = Vector3.zero;
                
                _currentState = PlayerState.Idle;
            }
        }

        private bool CheckForClimbableLedge(out Vector3 targetPos, out Vector3 wallNormal)
        {
            targetPos = Vector3.zero;
            wallNormal = -transform.forward; 
            Vector3 direction = transform.forward;
            Vector3 flatDirection = new Vector3(direction.x, 0, direction.z).normalized;
            
            float currentMaxHeight = _isGrounded ? _climbMaxHeight : _airClimbMaxHeight;
            
            // On réduit légèrement les distances pour être plus précis et éviter de "traverser" des angles
            float[] forwardDistances = { 0.4f, 0.6f, 0.8f };
            
            foreach (float dist in forwardDistances)
            {
                Vector3 checkPoint = transform.position + Vector3.up * currentMaxHeight + flatDirection * dist;
                
                if (Physics.Raycast(checkPoint, Vector3.down, out RaycastHit ledgeHit, currentMaxHeight + 0.5f, _climbableLayer))
                {
                    // FIX: On vérifie que la surface du rebord est bien horizontale (normale vers le haut)
                    if (ledgeHit.normal.y < 0.9f) continue;

                    float height = ledgeHit.point.y - transform.position.y;
                    
                    // FIX: En l'air, on ne veut attraper que des rebords qui sont au-dessus de nous
                    // pour éviter de "redescendre" se suspendre à un rebord sur lequel on pourrait marcher.
                    float minHeight = _isGrounded ? _stepHeight : (_climbHangOffsetY - 0.2f);
                    
                    if (height > minHeight && height <= currentMaxHeight)
                    {
                        // On vérifie qu'il n'y a pas de plafond au-dessus
                        if (!Physics.Raycast(ledgeHit.point + Vector3.up * 0.1f, Vector3.up, 1.8f, _groundLayer | _climbableLayer))
                        {
                            // On cherche la normale exacte du mur pour valider que c'est une face verticale
                            Vector3 wallCheckStart = ledgeHit.point + Vector3.down * 0.1f - flatDirection * 0.5f;
                            if (Physics.Raycast(wallCheckStart, flatDirection, out RaycastHit wallHit, 0.7f, _climbableLayer))
                            {
                                // FIX: On vérifie que le mur est bien vertical (normale horizontale)
                                if (Mathf.Abs(wallHit.normal.y) > 0.3f) continue;

                                wallNormal = wallHit.normal;
                                targetPos = ledgeHit.point;
                                return true;
                            }
                        }
                    }
                }
            }
            
            return false;
        }

        private System.Collections.IEnumerator ClimbRoutine(Vector3 targetPos, Vector3 wallNormal)
        {
            if (_isWeaponDrawn) SheatheWeapon(); // Force le rengainement
            
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
            _lastClimbTime = Time.time;
            _currentState = PlayerState.Idle;
        }

        private void ApplyGravity()
        {
            _velocity.y += _gravity * Time.deltaTime;
            if (_controller.enabled)
            {
                _controller.Move(_velocity * Time.deltaTime);
            }
        }

        private void UpdateState()
        {
            if (_currentState == PlayerState.Climb || _currentState == PlayerState.BigPush || _currentState == PlayerState.HardLand || _currentState == PlayerState.Roll || _currentState == PlayerState.Attack || _currentState == PlayerState.Hurt || _currentState == PlayerState.Treasure) return;

            // On ne traite les états "au sol" (Idle/Move) que si on n'est pas en train de monter (saut)
            // Cela évite que l'état Jump soit écrasé par Idle au premier frame du saut.
            if (_isGrounded && _velocity.y <= 0.1f)
            {
                // Si on vient de tomber ou de sauter et qu'on touche le sol
                if (_currentState == PlayerState.Fall || _currentState == PlayerState.Jump)
                {
                    // Vérifier si la chute est assez haute pour déclencher la roulade
                    float fallDistance = _fallPeakY - transform.position.y;
                    if (fallDistance >= _hardLandHeightThreshold)
                    {
                        StartCoroutine(HardLandRoutine());
                        return;
                    }
                }

                if (_pushTimer > 0)
                {
                    _currentState = PlayerState.Push;
                    _pushTimer -= Time.deltaTime;
                }
                else if (_inputDirection.magnitude > 0.1f)
                    _currentState = PlayerState.Move;
                else
                    _currentState = PlayerState.Idle;
            }
            else
            {
                // Initialiser le suivi de la hauteur max quand on quitte le sol (sans saut)
                if (_currentState != PlayerState.Jump && _currentState != PlayerState.Fall)
                {
                    _fallPeakY = transform.position.y;
                }
                _fallPeakY = Mathf.Max(_fallPeakY, transform.position.y);

                if (_velocity.y > 0)
                    _currentState = PlayerState.Jump;
                else
                    _currentState = PlayerState.Fall;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualisation de la hauteur de marche (Step Height)
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange
            Vector3 feetOrigin = transform.position;
            // On décale le gizmo de step height légèrement vers l'avant (au bord du radius)
            float radius = _controller != null ? _controller.radius : 0.3f;
            Vector3 stepBasePos = feetOrigin + transform.forward * radius;
            Vector3 stepTopPos = stepBasePos + Vector3.up * _stepHeight;
            
            // Ligne verticale représentant la hauteur
            Gizmos.DrawLine(stepBasePos, stepTopPos);
            // Petit plateau pour bien voir la limite
            Gizmos.DrawWireCube(stepTopPos + transform.forward * 0.1f, new Vector3(0.4f, 0.02f, 0.2f));

            // Visualisation de la zone d'attaque (Hitbox)
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Rouge transparent
            Vector3 attackCenter = transform.position + Vector3.up * 1f + transform.forward * (_attackHitRange / 2f);
            Gizmos.DrawWireSphere(attackCenter, _attackHitRadius);

            // Visualize the edge check ray
            Vector3 moveDirection = transform.forward;
            Vector3 rayStart = transform.position + Vector3.up * _edgeCheckHeight + moveDirection * _edgeCheckDistance;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(rayStart, rayStart + Vector3.down * 1.0f);

            // Visualize climb detection (New Top-Down Raycasts)
            Vector3 flatDirection = new Vector3(moveDirection.x, 0, moveDirection.z).normalized;
            float[] forwardDistances = { 0.4f, 0.6f, 0.8f };
            
            foreach (float dist in forwardDistances)
            {
                // Lignes bleues pour la grimpe depuis le sol
                Gizmos.color = Color.blue;
                Vector3 groundCheckPoint = transform.position + Vector3.up * _climbMaxHeight + flatDirection * dist;
                Gizmos.DrawLine(groundCheckPoint, groundCheckPoint + Vector3.down * (_climbMaxHeight + 0.5f));

                // Lignes cyan pour la grimpe en l'air (saut)
                Gizmos.color = Color.cyan;
                Vector3 airCheckPoint = transform.position + Vector3.up * _airClimbMaxHeight + flatDirection * dist;
                Gizmos.DrawLine(airCheckPoint, airCheckPoint + Vector3.down * (_airClimbMaxHeight + 0.5f));
            }

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
            if (_currentState == PlayerState.Climb || _currentState == PlayerState.BigPush) return;

            Rigidbody body = hit.collider.attachedRigidbody;

            // Si on touche un Rigidbody qui n'est pas cinématique
            if (body == null || body.isKinematic) return;

            // On ne pousse pas vers le bas
            if (hit.moveDirection.y < -0.3f) return;

            // On rengaine l'épée automatiquement car le joueur a besoin de ses mains
            if (_isWeaponDrawn) SheatheWeapon();

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

            // Calcul de la vitesse de poussée
            float finalPushSpeed = _moveSpeed * _pushStrength;
            
            // Si le bloc a une résistance spécifique, on l'applique
            if (hit.collider.TryGetComponent(out PushableBlock block))
            {
                // Plus la résistance est haute, plus c'est lent (vitesse / résistance)
                finalPushSpeed /= block.PushResistance;
            }

            // On applique la force sur l'axe choisi
            body.linearVelocity = pushDir * finalPushSpeed;

            // Détection de bord pour le BigPush
            if (block != null && block.IsNearEdge(pushDir))
            {
                StartCoroutine(BigPushRoutine(block, pushDir));
            }
            else
            {
                // On active le timer de poussée
                _pushTimer = 0.15f;
            }
        }

        private System.Collections.IEnumerator BigPushRoutine(PushableBlock block, Vector3 direction)
        {
            _currentState = PlayerState.BigPush;
            IsLocked = true;

            // On ne touche plus à la position/rotation (plus de magnétisme)
            
            // Pause pour l'anticipation de l'animation bigPushAnim
            yield return new WaitForSeconds(0.4f);

            // Le coup puissant
            if (block != null)
            {
                float calculatedForce = block.GetBigPushForce(direction);
                block.Push(direction * calculatedForce);
            }

            // Temps de recovery après le coup
            yield return new WaitForSeconds(0.6f);

            IsLocked = false;
            _currentState = PlayerState.Idle;
        }

        private System.Collections.IEnumerator HardLandRoutine()
        {
            _currentState = PlayerState.HardLand;

            yield return new WaitForSeconds(_hardLandDuration);

            _currentState = PlayerState.Idle;
        }

        // Propriété publique pour savoir si le bonus de roulade est actif (utile pour debug ou UI)
        public bool HasRollJumpBoost => _rollJumpBoostTimer > 0f;

        private void HandleTakeDamage(DamageType type)
        {
            // Le vide (Void) gère son propre système de blocage, on n'ajoute pas de stun ici
            if (type == DamageType.Void) return;

            if (_hurtCoroutine != null) StopCoroutine(_hurtCoroutine);
            _hurtCoroutine = StartCoroutine(HurtStunRoutine());
        }

        private System.Collections.IEnumerator HurtStunRoutine()
        {
            // On interrompt les actions en cours
            if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
            
            _currentState = PlayerState.Hurt;
            _currentMoveDirection = Vector3.zero;

            // On utilise à nouveau la durée spécifique du stun (indépendante de l'invulnérabilité)
            yield return new WaitForSeconds(_hurtStunDuration);

            if (_currentState == PlayerState.Hurt)
            {
                _currentState = PlayerState.Idle;
            }
        }
    }
}
