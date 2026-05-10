using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PuzzleDungeon.Player;
using PuzzleDungeon.Interactions;
using PuzzleDungeon.Systems.Runs;
using UnityEngine.SceneManagement;

namespace PuzzleDungeon.Systems.Save
{
    public class PersistenceManager : MonoBehaviour
    {
        public static PersistenceManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private List<ItemData> _allPossibleItems = new List<ItemData>();
        [SerializeField] private List<RunConfiguration> _availableConfigs = new List<RunConfiguration>();

        [Header("Status")]
        [SerializeField] private int _currentSlot = -1;
        [SerializeField] private GameData _currentData;

        public GameData CurrentData => _currentData;
        public int CurrentSlot => _currentSlot;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // On n'applique les données que si on est dans une scène de jeu (pas le menu principal)
            if (scene.name != "MainMenu" && _currentSlot != -1)
            {
                Debug.Log($"[PersistenceManager] Scene {scene.name} loaded. Applying data...");
                ApplyDataToGame();
            }
        }

        private void Start()
        {
            // Note: Auto-loading removed to allow Main Menu slot selection.
            // if (SaveSystem.HasSave(0)) { ... }
        }

        public void NewGame(int slotIndex, string configID = "random")
        {
            _currentSlot = slotIndex;
            _currentData = new GameData();
            _currentData.slotIndex = slotIndex;
            
            // Randomisation
            if (configID == "random")
            {
                // On essaye d'abord via le RunManager (si on est déjà en jeu)
                if (RunManager.Instance != null && RunManager.Instance.AvailableConfigs.Count > 0)
                {
                    int randomIndex = Random.Range(0, RunManager.Instance.AvailableConfigs.Count);
                    _currentData.runConfigurationID = RunManager.Instance.AvailableConfigs[randomIndex].ConfigurationID;
                }
                // Sinon on utilise la liste locale (utile depuis le Menu Principal)
                else if (_availableConfigs != null && _availableConfigs.Count > 0)
                {
                    int randomIndex = Random.Range(0, _availableConfigs.Count);
                    _currentData.runConfigurationID = _availableConfigs[randomIndex].ConfigurationID;
                    Debug.Log($"[PersistenceManager] Selected random config from local list: {_currentData.runConfigurationID}");
                }
                else
                {
                    _currentData.runConfigurationID = "default";
                    Debug.LogWarning("[PersistenceManager] No configurations available for randomization! Falling back to 'default'.");
                }
            }
            else
            {
                _currentData.runConfigurationID = configID;
            }

            Debug.Log($"[PersistenceManager] New Game in slot {slotIndex} with Config: {_currentData.runConfigurationID}");
            
            // On sauvegarde et on APPLIQUE immédiatement pour voir le changement
            SaveGame();
            ApplyDataToGame();
        }

        public void LoadGame(int slotIndex)
        {
            _currentSlot = slotIndex;
            _currentData = SaveSystem.Load(slotIndex);

            if (_currentData != null)
            {
                ApplyDataToGame();
                Debug.Log($"[PersistenceManager] Loaded slot {slotIndex}");
            }
        }

        public void SaveGame()
        {
            if (_currentSlot < 0) return;
            if (_currentData == null) _currentData = new GameData();

            CollectDataFromGame();
            SaveSystem.Save(_currentSlot, _currentData);
        }

        private void CollectDataFromGame()
        {
            // 1. Joueur
            PlayerHealth health = FindObjectOfType<PlayerHealth>();
            if (health != null)
            {
                _currentData.currentHealth = health.CurrentHealth;
                _currentData.playerPosition[0] = health.transform.position.x;
                _currentData.playerPosition[1] = health.transform.position.y;
                _currentData.playerPosition[2] = health.transform.position.z;
                
                _currentData.playerRotation[0] = health.transform.rotation.x;
                _currentData.playerRotation[1] = health.transform.rotation.y;
                _currentData.playerRotation[2] = health.transform.rotation.z;
                _currentData.playerRotation[3] = health.transform.rotation.w;
            }

            // 2. Inventaire
            PlayerInventory inventory = FindObjectOfType<PlayerInventory>();
            if (inventory != null)
            {
                _currentData.inventoryItemNames = inventory.GetAllItems().Select(i => i.ItemName).ToList();
            }

            // 3. Objets persistants (ISaveable)
            var saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>();
            foreach (var s in saveables)
            {
                s.PopulateSaveData(_currentData);
            }
        }

        private void ApplyDataToGame()
        {
            // 1. Appliquer la configuration de Run en premier
            if (RunManager.Instance != null && !string.IsNullOrEmpty(_currentData.runConfigurationID))
            {
                RunManager.Instance.ApplyRunConfiguration(_currentData.runConfigurationID);
            }

            // 2. Joueur
            PlayerHealth health = FindObjectOfType<PlayerHealth>();
            if (health != null)
            {
                Vector3 pos = new Vector3(_currentData.playerPosition[0], _currentData.playerPosition[1], _currentData.playerPosition[2]);
                Quaternion rot = new Quaternion(_currentData.playerRotation[0], _currentData.playerRotation[1], _currentData.playerRotation[2], _currentData.playerRotation[3]);
                
                PlayerController pc = health.GetComponent<PlayerController>();
                if (pc != null)
                {
                    // Si c'est une nouvelle partie, on cherche le point de spawn
                    if (_currentData.isNewGame)
                    {
                        PlayerSpawnPoint[] spawnPoints = FindObjectsOfType<PlayerSpawnPoint>();
                        PlayerSpawnPoint startPoint = null;
                        
                        if (spawnPoints.Length > 0)
                        {
                            startPoint = spawnPoints.FirstOrDefault(s => s.IsDefault) ?? spawnPoints[0];
                        }

                        if (startPoint != null)
                        {
                            pos = startPoint.transform.position;
                            rot = startPoint.transform.rotation;
                            pc.StartIntro(pos, rot);
                        }
                        else
                        {
                            Debug.LogWarning("[PersistenceManager] No PlayerSpawnPoint found in scene! Spawning at default (0,0,0).");
                            pc.Teleport(pos, rot);
                        }

                        _currentData.isNewGame = false;
                        SaveGame(); // Marquer que ce n'est plus une nouvelle partie
                    }
                    else
                    {
                        pc.Teleport(pos, rot);
                    }
                }

                health.RestoreHealth(_currentData.currentHealth);
            }

            // 3. Inventaire
            PlayerInventory inventory = FindObjectOfType<PlayerInventory>();
            if (inventory != null)
            {
                // On vide l'inventaire actuel (nécessite une méthode ou un reset)
                // Pour l'instant on ajoute juste les items
                foreach (string itemName in _currentData.inventoryItemNames)
                {
                    ItemData item = _allPossibleItems.Find(i => i.ItemName == itemName);
                    if (item != null) inventory.AddItem(item);
                }
            }

            // 4. Objets persistants (ISaveable)
            var saveables = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>();
            foreach (var s in saveables)
            {
                s.LoadFromSaveData(_currentData);
            }
        }
    }
}
