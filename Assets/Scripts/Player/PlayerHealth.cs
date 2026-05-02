using System;
using System.Collections;
using UnityEngine;

namespace PuzzleDungeon.Player
{
    public enum DamageType
    {
        Enemy,  // Dégâts d'un ennemi (GetHit classique)
        Void,   // Chute dans le vide
        Trap    // Piège (pour le futur)
    }

    [RequireComponent(typeof(PlayerController))]
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField, Tooltip("Nombre maximum de cœurs")] 
        private int _maxHealth = 3;
        
        [SerializeField, Tooltip("Temps d'invulnérabilité après avoir pris un coup (en secondes)")] 
        private float _invulnerabilityDuration = 1.5f;

        [Header("Respawn Settings")]
        [SerializeField, Tooltip("Durée du blocage pendant l'animation de réapparition")]
        private float _respawnAnimDuration = 2f;

        [Header("Visual Settings")]
        [SerializeField, Tooltip("Racine du modèle 3D pour le clignotement. Si vide, cherchera automatiquement.")]
        private Transform _visualRoot;

        private int _currentHealth;
        private bool _isInvulnerable = false;
        private bool _isRespawning = false;
        private PlayerController _playerController;
        private Renderer[] _blinkRenderers;

        // Points de sauvegarde / respawn
        private Vector3 _respawnPosition;
        private Quaternion _respawnRotation;

        // Événements pour mettre à jour l'UI ou déclencher des animations
        public event Action<int, int> OnHealthChanged; // Current, Max
        public event Action<DamageType> OnTakeDamage;
        public event Action OnDeath;
        public event Action OnRespawn;
        public event Action OnRespawnStart; // Les ennemis gèlent
        public event Action OnRespawnEnd;   // Les ennemis reprennent

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public bool IsInvulnerable => _isInvulnerable;
        public bool IsRespawning => _isRespawning;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
        }

        private void Start()
        {
            _currentHealth = _maxHealth;
            
            // Le point de respawn par défaut est la position de départ
            SetRespawnPoint(transform.position, transform.rotation);

            // Cache les renderers du modèle pour le clignotement
            CacheRenderers();
        }

        private void CacheRenderers()
        {
            Transform root = _visualRoot != null ? _visualRoot : transform;
            Renderer[] allRenderers = root.GetComponentsInChildren<Renderer>(true);

            // On ne garde que les MeshRenderer et SkinnedMeshRenderer
            // (exclut ParticleSystemRenderer, TrailRenderer, LineRenderer, etc.)
            var filtered = new System.Collections.Generic.List<Renderer>();
            foreach (var rend in allRenderers)
            {
                if (rend is MeshRenderer || rend is SkinnedMeshRenderer)
                {
                    filtered.Add(rend);
                }
            }
            _blinkRenderers = filtered.ToArray();
            Debug.Log($"[PlayerHealth] {_blinkRenderers.Length} renderer(s) cachés pour le clignotement.");
        }

        /// <summary>
        /// Définit le point où le joueur réapparaîtra s'il meurt.
        /// (À appeler lorsqu'on passe une porte ou active une stèle/checkpoint)
        /// </summary>
        public void SetRespawnPoint(Vector3 position, Quaternion rotation)
        {
            _respawnPosition = position;
            _respawnRotation = rotation;
        }

        public void TakeDamage(int damage, DamageType type = DamageType.Enemy)
        {
            if (_isInvulnerable || _currentHealth <= 0) return;

            _currentHealth -= damage;
            
            // Sécurité pour ne pas descendre en dessous de 0
            _currentHealth = Mathf.Max(_currentHealth, 0); 

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnTakeDamage?.Invoke(type);

            if (_currentHealth <= 0)
            {
                Die();
            }
            else
            {
                StartCoroutine(InvulnerabilityRoutine());
            }
        }

        public void Heal(int amount)
        {
            if (_currentHealth <= 0) return; // Ne peut pas soigner un joueur mort avec cette méthode

            _currentHealth += amount;
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
            
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void Die()
        {
            OnDeath?.Invoke();
            
            // Ici, on pourrait jouer une animation de mort, attendre quelques secondes, etc.
            // Pour l'instant, on respawn immédiatement :
            Respawn();
        }

        private void Respawn()
        {
            // On restaure la vie
            _currentHealth = _maxHealth;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            // On retire l'invulnérabilité si elle était en cours
            _isInvulnerable = false;
            _isRespawning = false;
            StopAllCoroutines(); 

            // On téléporte au dernier point de sauvegarde
            _playerController.Teleport(_respawnPosition, _respawnRotation);
            
            OnRespawn?.Invoke();

            // On lance la séquence de blocage + animation de réapparition
            StartRespawnSequence();
            StartCoroutine(InvulnerabilityRoutine());
        }

        /// <summary>
        /// Lance la séquence de réapparition : bloque le joueur et signale aux ennemis de geler.
        /// Appelé automatiquement par Respawn(), ou manuellement par VoidTrigger si le joueur survit.
        /// </summary>
        public void StartRespawnSequence()
        {
            if (_isRespawning) return; // Déjà en cours
            StartCoroutine(RespawnLockRoutine());
        }

        private IEnumerator RespawnLockRoutine()
        {
            _isRespawning = true;
            _playerController.IsLocked = true;
            OnRespawnStart?.Invoke();

            yield return new WaitForSeconds(_respawnAnimDuration);

            _playerController.IsLocked = false;
            _isRespawning = false;
            OnRespawnEnd?.Invoke();
        }

        private IEnumerator InvulnerabilityRoutine()
        {
            _isInvulnerable = true;
            
            float elapsedTime = 0f;
            float blinkInterval = 0.15f; // Vitesse de clignotement
            bool isHidden = false;

            while (elapsedTime < _invulnerabilityDuration)
            {
                isHidden = !isHidden;
                foreach (var rend in _blinkRenderers)
                {
                    if (rend != null) rend.forceRenderingOff = isHidden;
                }

                yield return new WaitForSeconds(blinkInterval);
                elapsedTime += blinkInterval;
            }

            // S'assurer que les renderers sont bien réactivés à la fin
            foreach (var rend in _blinkRenderers)
            {
                if (rend != null) rend.forceRenderingOff = false;
            }
            
            _isInvulnerable = false;
        }
    }
}
