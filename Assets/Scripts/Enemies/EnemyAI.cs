using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using PuzzleDungeon.Player;

namespace PuzzleDungeon.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Animator))]
    public class EnemyAI : MonoBehaviour
    {
        public enum EnemyState { Idle, Patrol, Chase, Attack, Hurt, Dead, Paused }
        public enum AttackType { Sphere, Fan }

        [Header("References")]
        [SerializeField, Tooltip("Référence au Transform du joueur. Sera trouvée automatiquement via le tag Player si vide.")] 
        private Transform _playerTransform;
        
        [Header("Detection Settings")]
        [SerializeField] private float _chaseRange = 10f;
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _loseSightRange = 15f;

        [Header("Movement Settings")]
        [SerializeField] private float _patrolSpeed = 2f;
        [SerializeField] private float _chaseSpeed = 4.5f;
        
        [Header("Stability & Stun Settings")]
        [SerializeField, Tooltip("Durée d'immobilisation après avoir reçu un coup qui stun.")]
        private float _hurtStunDuration = 1f;

        [SerializeField, Tooltip("Délai minimum entre deux interruptions (stun) possibles.")]
        private float _stunCooldown = 0.5f;
        
        [SerializeField, Tooltip("Nombre de coups nécessaires pour déclencher un stun (Poise).")]
        private int _hitsToStun = 1;
        
        [SerializeField, Tooltip("Si vrai, l'ennemi ne peut pas être interrompu pendant son attaque.")]
        private bool _superArmorDuringAttack = false;

        [Header("Attack Settings")]
        [SerializeField] private AttackType _attackType = AttackType.Sphere;
        [SerializeField, Range(0, 360)] private float _attackFanAngle = 90f;
        [SerializeField] private float _attackCooldown = 2f;
        [SerializeField] private int _attackDamage = 1;
        [SerializeField] private float _attackDuration = 1f;
        [SerializeField, Tooltip("Délai avant la toute première attaque après l'apparition.")] 
        private float _initialAttackDelay = 1f;
        [SerializeField, Tooltip("L'ennemi peut-il pivoter vers le joueur pendant qu'il attaque ?")] 
        private bool _rotateDuringAttack = true;

        [SerializeField, Tooltip("Multiplicateur de vitesse pour l'animation d'attaque.")]
        private float _attackAnimationSpeed = 1f;

        [Header("Patrol Settings")]
        [SerializeField, Tooltip("Points de passage de la patrouille. L'ennemi restera sur place si la liste est vide.")] 
        private Transform[] _waypoints;
        private int _currentWaypointIndex = 0;
        [SerializeField] private float _waitTimeAtWaypoint = 2f;

        [Header("Death Settings")]
        [SerializeField, Tooltip("Délai avant de détruire l'objet après la mort (laisser le temps à l'anim)")] 
        private float _destroyDelay = 3f;
        
        [Header("Animation Parameters")]
        [SerializeField] private string _speedParam = "Speed";
        [SerializeField] private string _attackTrigger = "Attack";
        [SerializeField] private string _hurtTrigger = "GetHit";
        [SerializeField] private string _deathTrigger = "Die";
        
        [Header("Attack Visual Feedback")]
        [SerializeField] private int _whiteFlashCount = 2;
        [SerializeField] private float _whiteFlashDuration = 0.4f;

        private NavMeshAgent _agent;
        private EnemyHealth _health;
        private Animator _animator;
        private PlayerHealth _playerHealth;
        private EnemyAudio _enemyAudio;
        
        private EnemyState _currentState = EnemyState.Idle;
        private EnemyState _stateBeforePause = EnemyState.Idle;
        private float _lastAttackTime;
        private float _lastStunTime = -1f;
        private int _currentStunHits = 0;
        private Coroutine _stateCoroutine;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<EnemyHealth>();
            _animator = GetComponent<Animator>();
            _enemyAudio = GetComponent<EnemyAudio>();

            // Chercher le joueur si non assigné
            if (_playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _playerTransform = player.transform;
            }
            
            if (_playerTransform != null)
            {
                _playerHealth = _playerTransform.GetComponent<PlayerHealth>();
            }
        }

        private void OnEnable()
        {
            _health.OnHurt += HandleHurt;
            _health.OnDeath += HandleDeath;
            
            if (_playerHealth != null)
            {
                // Quand le joueur réapparaît (chute dans le vide ou mort), l'IA s'arrête
                _playerHealth.OnRespawnStart += PauseAI;
                _playerHealth.OnRespawnEnd += ResumeAI;
            }
        }

        private void OnDisable()
        {
            _health.OnHurt -= HandleHurt;
            _health.OnDeath -= HandleDeath;
            
            if (_playerHealth != null)
            {
                _playerHealth.OnRespawnStart -= PauseAI;
                _playerHealth.OnRespawnEnd -= ResumeAI;
            }
        }

        private void Start()
        {
            // Initialiser le temps de la dernière attaque pour respecter le délai initial
            _lastAttackTime = Time.time + _initialAttackDelay - _attackCooldown;
            ChangeState(EnemyState.Idle);
        }

        private void Update()
        {
            if (_currentState == EnemyState.Dead || _currentState == EnemyState.Hurt || _currentState == EnemyState.Paused || _playerTransform == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            switch (_currentState)
            {
                case EnemyState.Idle:
                case EnemyState.Patrol:
                    if (distanceToPlayer <= _chaseRange)
                    {
                        ChangeState(EnemyState.Chase);
                    }
                    break;
                case EnemyState.Chase:
                    // On n'attaque que si on est à portée ET que le cooldown est expiré
                    if (distanceToPlayer <= _attackRange && Time.time - _lastAttackTime >= _attackCooldown)
                    {
                        ChangeState(EnemyState.Attack);
                    }
                    else if (distanceToPlayer > _loseSightRange)
                    {
                        ChangeState(EnemyState.Patrol);
                    }
                    else
                    {
                        if (_agent.isOnNavMesh) _agent.SetDestination(_playerTransform.position);
                    }
                    break;
                case EnemyState.Attack:
                    // Tourner doucement vers le joueur pendant l'attaque
                    if (_rotateDuringAttack)
                    {
                        Vector3 direction = (_playerTransform.position - transform.position).normalized;
                        direction.y = 0;
                        if (direction.magnitude > 0.1f)
                        {
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 5f * Time.deltaTime);
                        }
                    }
                    break;
            }

            UpdateAnimator();
        }

        private void UpdateAnimator()
        {
            // Transmettre la vitesse au contrôleur d'animation pour les transitions Idle -> Run
            if (!string.IsNullOrEmpty(_speedParam))
                _animator.SetFloat(_speedParam, _agent.velocity.magnitude);
        }

        private void ChangeState(EnemyState newState)
        {
            if (_currentState == EnemyState.Dead) return;

            if (_stateCoroutine != null)
            {
                StopCoroutine(_stateCoroutine);
                _stateCoroutine = null;
            }

            _currentState = newState;

            switch (_currentState)
            {
                case EnemyState.Idle:
                    if (_agent.isOnNavMesh) _agent.isStopped = true;
                    _stateCoroutine = StartCoroutine(IdleRoutine());
                    break;
                case EnemyState.Patrol:
                    _agent.speed = _patrolSpeed;
                    if (_agent.isOnNavMesh) _agent.isStopped = false;
                    _stateCoroutine = StartCoroutine(PatrolRoutine());
                    break;
                case EnemyState.Chase:
                    _agent.speed = _chaseSpeed;
                    if (_agent.isOnNavMesh) _agent.isStopped = false;
                    break;
                case EnemyState.Attack:
                    if (_agent.isOnNavMesh)
                    {
                        _agent.isStopped = true;
                        _agent.velocity = Vector3.zero; // Force l'arrêt immédiat pour éviter le glissement
                    }
                    _stateCoroutine = StartCoroutine(AttackRoutine());
                    break;
                case EnemyState.Hurt:
                    if (_agent.isOnNavMesh) _agent.isStopped = true;
                    SetTriggerIfExists(_hurtTrigger);
                    _stateCoroutine = StartCoroutine(HurtRoutine());
                    break;
                case EnemyState.Dead:
                    if (_agent.isOnNavMesh) _agent.isStopped = true;
                    _agent.enabled = false;
                    SetTriggerIfExists(_deathTrigger);
                    
                    var collider = GetComponent<Collider>();
                    if (collider != null) collider.enabled = false;
                    break;
                case EnemyState.Paused:
                    if (_agent.isOnNavMesh) _agent.isStopped = true;
                    break;
            }
        }

        private IEnumerator IdleRoutine()
        {
            yield return new WaitForSeconds(_waitTimeAtWaypoint);
            
            if (_waypoints != null && _waypoints.Length > 0)
            {
                ChangeState(EnemyState.Patrol);
            }
        }

        private IEnumerator PatrolRoutine()
        {
            if (_waypoints == null || _waypoints.Length == 0) yield break;

            Transform targetWaypoint = _waypoints[_currentWaypointIndex];
            _agent.SetDestination(targetWaypoint.position);

            while (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance)
            {
                yield return null;
            }

            _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
            ChangeState(EnemyState.Idle);
        }

        private IEnumerator AttackRoutine()
        {
            // S'assurer de faire face au joueur au moment où on déclenche l'attaque
            LookAtPlayer();

            if (_enemyAudio != null) _enemyAudio.PlayAttackSFX();

            // Ajuster la vitesse de l'animator pour l'attaque
            float originalSpeed = _animator.speed;
            _animator.speed = _attackAnimationSpeed;

            SetTriggerIfExists(_attackTrigger);
            
            // On attend la durée de l'attaque ajustée par la vitesse d'animation
            yield return new WaitForSeconds(_attackDuration / _attackAnimationSpeed);
            
            // Restaurer la vitesse de l'animator
            _animator.speed = originalSpeed;

            // On marque la fin de l'attaque pour démarrer le cooldown
            _lastAttackTime = Time.time;

            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
            
            // Après l'attaque, on repasse en Chase pour pouvoir bouger pendant le cooldown
            ChangeState(EnemyState.Chase);
        }

        private IEnumerator HurtRoutine()
        {
            yield return new WaitForSeconds(_hurtStunDuration);
            ChangeState(EnemyState.Chase);
        }

        private void HandleHurt()
        {
            if (_currentState == EnemyState.Dead) return;

            // 1. Super Armor : Ignorer le stun si on attaque
            if (_superArmorDuringAttack && _currentState == EnemyState.Attack)
            {
                return;
            }

            // 2. Stun Cooldown : Ignorer si on a été stun trop récemment
            if (Time.time - _lastStunTime < _stunCooldown)
            {
                return;
            }

            // 3. Poise : Incrémenter le compteur de coups
            _currentStunHits++;
            if (_currentStunHits >= _hitsToStun)
            {
                _currentStunHits = 0;
                _lastStunTime = Time.time;
                ChangeState(EnemyState.Hurt);
            }
        }

        private void HandleDeath()
        {
            ChangeState(EnemyState.Dead);
            
            // Détruire l'objet après un délai pour laisser l'animation de mort se jouer
            Destroy(gameObject, _destroyDelay);
        }

        private void PauseAI()
        {
            if (_currentState == EnemyState.Dead) return;
            
            _stateBeforePause = _currentState;
            ChangeState(EnemyState.Paused);
        }

        private void ResumeAI()
        {
            if (_currentState == EnemyState.Dead) return;
            
            ChangeState(_stateBeforePause);
        }

        private void OnDrawGizmosSelected()
        {
            // Rayons de détection
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _chaseRange);
            
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, _loseSightRange);

            // Zone d'attaque
            Gizmos.color = Color.red;
            if (_attackType == AttackType.Sphere)
            {
                Gizmos.DrawWireSphere(transform.position, _attackRange);
            }
            else if (_attackType == AttackType.Fan)
            {
                // Dessiner les deux bords du cône d'attaque
                Vector3 leftLimit = Quaternion.AngleAxis(-_attackFanAngle / 2f, Vector3.up) * transform.forward;
                Vector3 rightLimit = Quaternion.AngleAxis(_attackFanAngle / 2f, Vector3.up) * transform.forward;
                
                Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, transform.position + Vector3.up * 0.5f + leftLimit * _attackRange);
                Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, transform.position + Vector3.up * 0.5f + rightLimit * _attackRange);
                
                // Note: Pour un arc de cercle parfait, il faudrait utiliser UnityEditor.Handles, 
                // mais ces deux lignes donnent déjà une excellente idée de la zone.
            }
        }

        /// <summary>
        /// Méthode à appeler via un Animation Event depuis l'animation d'attaque de l'ennemi.
        /// </summary>
        public void AnimEvent_FlashWhite()
        {
            if (_health != null)
            {
                _health.Blink(Color.white, _whiteFlashCount, _whiteFlashDuration);
            }
        }

        public void AnimEvent_DealDamage()
        {
            if (_playerTransform == null || _currentState == EnemyState.Dead) return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
            
            // On vérifie d'abord si le joueur est à portée globale (distance)
            if (distanceToPlayer <= _attackRange + 0.5f) 
            {
                bool isHit = false;

                if (_attackType == AttackType.Sphere)
                {
                    // Type Sphère : Touche tout ce qui est dans le rayon
                    isHit = true;
                }
                else if (_attackType == AttackType.Fan)
                {
                    // Type Éventail : On vérifie l'angle devant l'ennemi
                    Vector3 directionToPlayer = (_playerTransform.position - transform.position).normalized;
                    float angle = Vector3.Angle(transform.forward, directionToPlayer);
                    
                    if (angle <= _attackFanAngle / 2f)
                    {
                        isHit = true;
                    }
                }

                if (isHit && _playerHealth != null)
                {
                    _playerHealth.TakeDamage(_attackDamage, DamageType.Enemy);
                }
            }
        }

        private void LookAtPlayer()
        {
            if (_playerTransform == null) return;
            
            Vector3 direction = (_playerTransform.position - transform.position).normalized;
            direction.y = 0; // On ne veut pas que l'ennemi penche en avant/arrière
            
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void SetTriggerIfExists(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName) || _animator == null) return;

            foreach (AnimatorControllerParameter param in _animator.parameters)
            {
                if (param.name == triggerName)
                {
                    _animator.SetTrigger(triggerName);
                    return;
                }
            }
        }
    }
}
