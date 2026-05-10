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

        [Header("Auto-Save Settings")]
        [SerializeField] private bool _autoSaveEnabled = true;
        [SerializeField] private float _autoSaveInterval = 60f; // 1 minute par défaut pour plus de sécurité
        private float _autoSaveTimer;

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

        private void OnApplicationQuit()
        {
            if (_currentSlot != -1)
            {
                Debug.Log("[PersistenceManager] Application quitting, saving game...");
                SaveGame();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // On n'applique les données que si on est dans une scène de jeu (pas le menu principal)
            if (scene.name != "MainMenu")
            {
                // En éditeur, si on lance directement la scène sans passer par le menu, 
                // on initialise un slot par défaut pour que la sauvegarde fonctionne.
                if (_currentSlot == -1)
                {
                    #if UNITY_EDITOR
                    Debug.Log("[PersistenceManager] Editor mode detected. Assigning default slot 0.");
                    _currentSlot = 0;
                    if (SaveSystem.HasSave(0)) LoadGame(0);
                    else NewGame(0);
                    #endif
                }

                if (_currentSlot != -1)
                {
                    Debug.Log($"[PersistenceManager] Scene {scene.name} loaded. Applying data from slot {_currentSlot}...");
                    ApplyDataToGame();
                }
            }
        }

        private void Start()
        {
            // Note: Auto-loading removed to allow Main Menu slot selection.
            // if (SaveSystem.HasSave(0)) { ... }
            ResetAutoSaveTimer();
        }

        private void Update()
        {
            if (!_autoSaveEnabled || _currentSlot == -1) return;

            _autoSaveTimer -= Time.deltaTime;
            if (_autoSaveTimer <= 0)
            {
                Debug.Log("[PersistenceManager] Auto-saving...");
                SaveGame();
                ResetAutoSaveTimer();
            }
        }

        private void ResetAutoSaveTimer()
        {
            _autoSaveTimer = _autoSaveInterval;
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
            
            // On s'assure que currentData existe
            if (_currentData == null) 
            {
                _currentData = new GameData();
                _currentData.slotIndex = _currentSlot;
            }

            Debug.Log($"[PersistenceManager] Saving Game to slot {_currentSlot}...");
            CollectDataFromGame();
            SaveSystem.Save(_currentSlot, _currentData);
        }

        private void CollectDataFromGame()
        {
            PlayerInventory inventory = null;

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
                Debug.Log($"[PersistenceManager] Player state collected: Pos={health.transform.position}, Health={health.CurrentHealth}");
                
                // S'abonner aux changements d'inventaire si ce n'est pas déjà fait
                inventory = health.GetComponent<PlayerInventory>();
                if (inventory != null)
                {
                    inventory.OnInventoryChanged -= SaveGame; // Sécurité
                    inventory.OnInventoryChanged += SaveGame;
                }
            }
            else
            {
                Debug.LogWarning("[PersistenceManager] PlayerHealth not found, skipping player data collection.");
            }

            // 2. Inventaire
            if (inventory == null) inventory = FindObjectOfType<PlayerInventory>();
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
                inventory.ClearInventory();
                Debug.Log($"[PersistenceManager] Loading {_currentData.inventoryItemNames.Count} items into inventory.");
                foreach (string itemName in _currentData.inventoryItemNames)
                {
                    ItemData item = _allPossibleItems.Find(i => i.ItemName == itemName);
                    if (item != null) 
                    {
                        inventory.AddItem(item);
                    }
                    else
                    {
                        Debug.LogWarning($"[PersistenceManager] Item '{itemName}' not found in _allPossibleItems list!");
                    }
                }
            }

            // 4. Objets persistants (ISaveable)
            var saveables = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>();
            int count = 0;
            foreach (var s in saveables)
            {
                s.LoadFromSaveData(_currentData);
                count++;
            }
            Debug.Log($"[PersistenceManager] Applied data to {count} ISaveable objects.");
        }
    }
}
