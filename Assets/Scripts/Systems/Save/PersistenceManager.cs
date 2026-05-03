using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PuzzleDungeon.Player;
using PuzzleDungeon.Interactions;
using PuzzleDungeon.Systems.Runs;

namespace PuzzleDungeon.Systems.Save
{
    public class PersistenceManager : MonoBehaviour
    {
        public static PersistenceManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private List<ItemData> _allPossibleItems = new List<ItemData>();

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

        private void Start()
        {
            // Au lancement du jeu, on s'assure qu'un slot est toujours actif.
            // On utilise le slot 0 par défaut.
            if (SaveSystem.HasSave(0))
            {
                Debug.Log("[PersistenceManager] Auto-loading Slot 0 on startup.");
                LoadGame(0);
            }
            else
            {
                Debug.Log("[PersistenceManager] No save found. Creating default Save in Slot 0.");
                NewGame(0, "random");
            }
        }

        public void NewGame(int slotIndex, string configID = "random")
        {
            _currentSlot = slotIndex;
            _currentData = new GameData();
            _currentData.slotIndex = slotIndex;
            
            // Randomisation
            if (configID == "random")
            {
                if (RunManager.Instance != null && RunManager.Instance.AvailableConfigs.Count > 0)
                {
                    int randomIndex = Random.Range(0, RunManager.Instance.AvailableConfigs.Count);
                    _currentData.runConfigurationID = RunManager.Instance.AvailableConfigs[randomIndex].ConfigurationID;
                }
                else
                {
                    _currentData.runConfigurationID = "default";
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
                if (pc != null) pc.Teleport(pos, rot);

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
