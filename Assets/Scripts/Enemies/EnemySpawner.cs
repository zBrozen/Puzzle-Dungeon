using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using PuzzleDungeon.Enemies;

namespace PuzzleDungeon.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject[] _enemyPrefabs;
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField, Tooltip("Si vrai, les ennemis apparaissent dès que le joueur entre dans le trigger.")]
        private bool _spawnOnTriggerEnter = true;
        [SerializeField, Tooltip("Peut-on déclencher le spawn plusieurs fois ?")]
        private bool _oneTimeOnly = true;

        [Header("Events")]
        public UnityEvent OnSpawnStarted;
        public UnityEvent OnAllEnemiesKilled;

        private List<EnemyHealth> _spawnedEnemies = new List<EnemyHealth>();
        private bool _hasSpawned = false;
        private int _remainingEnemies = 0;

        private void OnTriggerEnter(Collider other)
        {
            // Debug pour voir ce qui touche la zone
            Debug.Log($"[Spawner] {gameObject.name} détecte une entrée : {other.name} | Tag: {other.tag} | Layer: {LayerMask.LayerToName(other.gameObject.layer)}");

            if (_spawnOnTriggerEnter)
            {
                if (other.CompareTag("Player"))
                {
                    Debug.Log("[Spawner] C'est le joueur ! Lancement du spawn...");
                    SpawnEnemies();
                }
                else
                {
                    Debug.Log($"[Spawner] Objet ignoré car le tag '{other.tag}' n'est pas 'Player'.");
                }
            }
        }

        public void SpawnEnemies()
        {
            if (_hasSpawned && _oneTimeOnly) return;
            if (_enemyPrefabs.Length == 0 || _spawnPoints.Length == 0) return;

            _hasSpawned = true;
            _spawnedEnemies.Clear();
            _remainingEnemies = 0;

            for (int i = 0; i < _spawnPoints.Length; i++)
            {
                // Choix d'un prefab au hasard ou dans l'ordre
                GameObject prefab = _enemyPrefabs[i % _enemyPrefabs.Length];
                Transform spawnPoint = _spawnPoints[i];

                GameObject newEnemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
                
                if (newEnemy.TryGetComponent(out EnemyHealth health))
                {
                    _spawnedEnemies.Add(health);
                    _remainingEnemies++;
                    health.OnDeath += HandleEnemyDeath;
                }
            }

            OnSpawnStarted?.Invoke();
            Debug.Log($"[Spawner] {gameObject.name} a fait apparaître {_remainingEnemies} ennemis.");
        }

        private void HandleEnemyDeath()
        {
            _remainingEnemies--;
            
            if (_remainingEnemies <= 0)
            {
                Debug.Log($"[Spawner] {gameObject.name} : Tous les ennemis sont vaincus !");
                OnAllEnemiesKilled?.Invoke();
            }
        }

        private void OnDrawGizmos()
        {
            // Dessine les points de spawn dans l'éditeur
            if (_spawnPoints != null)
            {
                Gizmos.color = Color.cyan;
                foreach (var sp in _spawnPoints)
                {
                    if (sp != null)
                    {
                        Gizmos.DrawWireSphere(sp.position, 0.5f);
                        Gizmos.DrawLine(sp.position, sp.position + sp.forward * 1f);
                    }
                }
            }

            // Dessine la zone de trigger si applicable
            BoxCollider box = GetComponent<BoxCollider>();
            if (box != null && box.isTrigger)
            {
                Gizmos.color = new Color(0, 1, 0, 0.2f);
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
        }
    }
}
