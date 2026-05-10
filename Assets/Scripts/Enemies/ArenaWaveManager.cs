using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using PuzzleDungeon.Player;

namespace PuzzleDungeon.Enemies
{
    [System.Serializable]
    public class ArenaWave
    {
        [Tooltip("Nombre total d'ennemis à faire apparaître dans cette vague.")]
        public int enemyCount = 3;

        [Tooltip("Les types d'ennemis qui peuvent apparaître dans cette vague.")]
        public GameObject[] enemyPrefabs;

        [Tooltip("Points d'apparition spécifiques pour cette vague (optionnel, sinon utilise ceux de l'arène).")]
        public Transform[] waveSpawnPoints;

        [Tooltip("Délai en secondes entre chaque apparition d'ennemi (0 pour tout faire apparaître d'un coup).")]
        public float spawnDelay = 0.5f;

        [Tooltip("Si vrai, chaque ennemi utilisera un point de spawn unique (tant qu'il y en a de disponibles).")]
        public bool useUniqueSpawnPoints = true;
    }

    public class ArenaWaveManager : MonoBehaviour
    {
        [Header("Arena Settings")]
        [SerializeField, Tooltip("Points d'apparition par défaut de l'arène.")]
        private Transform[] _defaultSpawnPoints;
        
        [SerializeField, Tooltip("Liste des vagues à affronter.")]
        private ArenaWave[] _waves;

        [SerializeField, Tooltip("L'arène démarre-t-elle dès que le joueur entre dans le trigger ?")]
        private bool _startOnTriggerEnter = true;

        [Header("Events")]
        public UnityEvent OnArenaStarted;
        public UnityEvent<int> OnWaveStarted; // int = index de la vague
        public UnityEvent<int> OnWaveCompleted;
        public UnityEvent OnArenaCompleted;
        public UnityEvent OnArenaReset;

        private int _currentWaveIndex = 0;
        private int _aliveEnemiesCount = 0;
        private bool _arenaIsActive = false;
        private bool _arenaIsCompleted = false;
        private List<EnemyHealth> _spawnedEnemies = new List<EnemyHealth>();
        private PlayerHealth _playerHealth;

        private void Start()
        {
            // Trouve le joueur dans la scène et s'abonne à son événement de réapparition
            _playerHealth = FindObjectOfType<PlayerHealth>();
            if (_playerHealth != null)
            {
                _playerHealth.OnRespawn += ResetArena;
            }
        }

        private void OnDestroy()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnRespawn -= ResetArena;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_startOnTriggerEnter && !_arenaIsActive && !_arenaIsCompleted)
            {
                if (other.CompareTag("Player"))
                {
                    StartArena();
                }
            }
        }

        public void StartArena()
        {
            if (_arenaIsActive || _arenaIsCompleted || _waves.Length == 0) return;

            _arenaIsActive = true;
            _currentWaveIndex = 0;
            
            Debug.Log($"[ArenaWaveManager] {gameObject.name} : Arène démarrée !");
            OnArenaStarted?.Invoke();
            
            StartCoroutine(StartWave(_currentWaveIndex));
        }

        private IEnumerator StartWave(int waveIndex)
        {
            if (waveIndex >= _waves.Length)
            {
                CompleteArena();
                yield break;
            }

            ArenaWave currentWave = _waves[waveIndex];
            _aliveEnemiesCount = 0;
            
            Debug.Log($"[ArenaWaveManager] Vague {waveIndex + 1}/{_waves.Length} démarrée.");
            OnWaveStarted?.Invoke(waveIndex);

            Transform[] spawnsToUse = (currentWave.waveSpawnPoints != null && currentWave.waveSpawnPoints.Length > 0) 
                ? currentWave.waveSpawnPoints 
                : _defaultSpawnPoints;

            if (spawnsToUse == null || spawnsToUse.Length == 0 || currentWave.enemyPrefabs == null || currentWave.enemyPrefabs.Length == 0)
            {
                Debug.LogWarning("[ArenaWaveManager] Pas de points de spawn ou de prefabs configurés pour cette vague !");
                yield break;
            }

            // Préparer les points de spawn si on veut qu'ils soient uniques
            List<Transform> spawnList = new List<Transform>(spawnsToUse);
            if (currentWave.useUniqueSpawnPoints)
            {
                // Mélange de Fisher-Yates pour la liste des points de spawn
                for (int i = 0; i < spawnList.Count; i++)
                {
                    Transform temp = spawnList[i];
                    int randomIndex = Random.Range(i, spawnList.Count);
                    spawnList[i] = spawnList[randomIndex];
                    spawnList[randomIndex] = temp;
                }
            }

            // Faire apparaître les ennemis un par un
            for (int i = 0; i < currentWave.enemyCount; i++)
            {
                Transform selectedSpawn;
                if (currentWave.useUniqueSpawnPoints)
                {
                    // On prend le point correspondant à l'index (avec modulo si plus d'ennemis que de points)
                    selectedSpawn = spawnList[i % spawnList.Count];
                }
                else
                {
                    // Comportement aléatoire classique (peut se répéter)
                    selectedSpawn = spawnsToUse[Random.Range(0, spawnsToUse.Length)];
                }

                SpawnSingleEnemy(currentWave, selectedSpawn);
                
                if (currentWave.spawnDelay > 0f)
                {
                    yield return new WaitForSeconds(currentWave.spawnDelay);
                }
            }
        }

        private void SpawnSingleEnemy(ArenaWave wave, Transform spawnPoint)
        {
            // Choisir un prefab aléatoirement
            GameObject prefab = wave.enemyPrefabs[Random.Range(0, wave.enemyPrefabs.Length)];

            GameObject newEnemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            
            if (newEnemy.TryGetComponent(out EnemyHealth health))
            {
                _aliveEnemiesCount++;
                _spawnedEnemies.Add(health);
                health.OnDeath += HandleEnemyDeath;
            }
            else
            {
                Debug.LogWarning($"[ArenaWaveManager] L'ennemi {prefab.name} n'a pas de composant EnemyHealth ! L'arène risque d'être bloquée.");
            }
        }

        private void HandleEnemyDeath()
        {
            _aliveEnemiesCount--;
            
            if (_aliveEnemiesCount <= 0)
            {
                Debug.Log($"[ArenaWaveManager] Vague {_currentWaveIndex + 1} terminée !");
                OnWaveCompleted?.Invoke(_currentWaveIndex);
                
                _currentWaveIndex++;
                
                // On peut ajouter un petit délai avant la prochaine vague ici
                StartCoroutine(WaitAndStartNextWave(2f));
            }
        }

        private IEnumerator WaitAndStartNextWave(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartCoroutine(StartWave(_currentWaveIndex));
        }

        private void CompleteArena()
        {
            _arenaIsActive = false;
            _arenaIsCompleted = true;
            Debug.Log($"[ArenaWaveManager] {gameObject.name} : Toutes les vagues sont terminées. Arène complétée !");
            OnArenaCompleted?.Invoke();
        }

        public void ResetArena()
        {
            StopAllCoroutines();
            
            // Détruire tous les ennemis encore en vie générés par l'arène
            foreach (var enemy in _spawnedEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }
            _spawnedEnemies.Clear();

            _arenaIsActive = false;
            _arenaIsCompleted = false;
            _currentWaveIndex = 0;
            _aliveEnemiesCount = 0;
            
            Debug.Log($"[ArenaWaveManager] {gameObject.name} : Arène réinitialisée.");
            OnArenaReset?.Invoke();
        }

        private void OnDrawGizmos()
        {
            // Dessine la zone de trigger si applicable
            BoxCollider box = GetComponent<BoxCollider>();
            if (box != null && box.isTrigger)
            {
                Gizmos.color = new Color(1, 0, 0, 0.2f); // Rouge transparent pour l'arène
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }

            // Dessine les points de spawn par défaut dans l'éditeur
            if (_defaultSpawnPoints != null)
            {
                Gizmos.color = Color.red;
                foreach (var sp in _defaultSpawnPoints)
                {
                    if (sp != null)
                    {
                        Gizmos.DrawWireSphere(sp.position, 0.5f);
                        Gizmos.DrawLine(sp.position, sp.position + sp.forward * 1f);
                    }
                }
            }
        }
    }
}
