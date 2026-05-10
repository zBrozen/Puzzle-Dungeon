using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using PuzzleDungeon.Systems.Save;

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

        [SerializeField, Tooltip("Racine du modèle 3D pour le clignotement. Si vide, cherchera automatiquement.")]
        private Transform _visualRoot;

        [Header("Damage Visuals")]
        [SerializeField] private Color _flashColor = Color.red;
        [SerializeField] private float _flashDuration = 0.15f;

        private int _currentHealth;
        private bool _isInvulnerable = false;
        private bool _isRespawning = false;
        private PlayerController _playerController;
        private Renderer[] _blinkRenderers;
        private Color[] _originalColors;
        private Coroutine _invulnerabilityCoroutine;

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
        public float InvulnerabilityDuration => _invulnerabilityDuration;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
        }

        private void Start()
        {
            _currentHealth = _maxHealth;
            
            // Chercher le point de spawn par défaut dans la scène
            PlayerSpawnPoint[] spawnPoints = FindObjectsOfType<PlayerSpawnPoint>();
            PlayerSpawnPoint startPoint = null;
            if (spawnPoints.Length > 0)
            {
                startPoint = spawnPoints.FirstOrDefault(s => s.IsDefault) ?? spawnPoints[0];
            }

            if (startPoint != null)
            {
                SetRespawnPoint(startPoint.transform.position, startPoint.transform.rotation);
            }
            else
            {
                // Fallback sur la position actuelle si aucun point de spawn n'est défini
                SetRespawnPoint(transform.position, transform.rotation);
            }

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
            
            // Sauvegarder les couleurs d'origine pour le flash (Toon Shader friendly)
            _originalColors = new Color[_blinkRenderers.Length];
            for (int i = 0; i < _blinkRenderers.Length; i++)
            {
                Material mat = _blinkRenderers[i].material;
                if (mat.HasProperty("_Color")) _originalColors[i] = mat.color;
                else if (mat.HasProperty("_BaseColor")) _originalColors[i] = mat.GetColor("_BaseColor");
                else _originalColors[i] = Color.white;
            }

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
            // Le vide outrepasse l'invulnérabilité pour forcer le respawn et les visuels,
            // mais on ne prend des dégâts que si on n'était pas déjà invulnérable.
            if (type != DamageType.Void && (_isInvulnerable || _currentHealth <= 0)) return;
            if (type == DamageType.Void && _currentHealth <= 0) return;

            bool wasInvulnerable = _isInvulnerable;
            if (!wasInvulnerable)
            {
                _currentHealth -= damage;
                _currentHealth = Mathf.Max(_currentHealth, 0); 
                OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            }

            OnTakeDamage?.Invoke(type);

            if (_currentHealth <= 0)
            {
                Die();
            }
            else
            {
                // On relance le clignotement (en arrêtant le précédent s'il existe)
                if (_invulnerabilityCoroutine != null) StopCoroutine(_invulnerabilityCoroutine);
                _invulnerabilityCoroutine = StartCoroutine(InvulnerabilityRoutine(type == DamageType.Void));
            }
        }

        public void Heal(int amount)
        {
            if (_currentHealth <= 0) return; // Ne peut pas soigner un joueur mort avec cette méthode

            _currentHealth += amount;
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
            
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        /// <summary>
        /// Restaure la vie silencieusement (utilisé par le système de sauvegarde).
        /// </summary>
        public void RestoreHealth(int amount)
        {
            _currentHealth = Mathf.Clamp(amount, 0, _maxHealth);
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
            
            // Invulnérabilité + clignotement après un respawn
            if (_invulnerabilityCoroutine != null) StopCoroutine(_invulnerabilityCoroutine);
            _invulnerabilityCoroutine = StartCoroutine(InvulnerabilityRoutine(true));
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

        private IEnumerator InvulnerabilityRoutine(bool isVoidDamage)
        {
            _isInvulnerable = true;
            
            float elapsedTime = 0f;
            float blinkInterval = 0.15f; // Vitesse de clignotement
            bool toggle = false;

            while (elapsedTime < _invulnerabilityDuration)
            {
                toggle = !toggle;

                for (int i = 0; i < _blinkRenderers.Length; i++)
                {
                    if (_blinkRenderers[i] == null) continue;

                    if (isVoidDamage)
                    {
                        // Mode classique : on disparaît/réapparaît
                        _blinkRenderers[i].forceRenderingOff = toggle;
                    }
                    else
                    {
                        // Mode combat : on clignote en rouge (Toon friendly)
                        Material mat = _blinkRenderers[i].material;
                        Color targetColor = toggle ? _flashColor : _originalColors[i];
                        
                        if (mat.HasProperty("_Color")) mat.color = targetColor;
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", targetColor);
                        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", toggle ? _flashColor * 2f : Color.black);
                    }
                }

                yield return new WaitForSeconds(blinkInterval);
                elapsedTime += blinkInterval;
            }

            // Restauration finale
            for (int i = 0; i < _blinkRenderers.Length; i++)
            {
                if (_blinkRenderers[i] == null) continue;
                
                _blinkRenderers[i].forceRenderingOff = false;
                
                Material mat = _blinkRenderers[i].material;
                if (mat.HasProperty("_Color")) mat.color = _originalColors[i];
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", _originalColors[i]);
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            }
            
            _isInvulnerable = false;
        }
    }
}
