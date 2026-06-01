using System.Collections.Generic;
using UnityEngine;

namespace PuzzleDungeon.Systems.Save
{
    [System.Serializable]
    public class ObjectStateData
    {
        public float[] position = new float[3];
        public float[] rotation = new float[4];

        public ObjectStateData() { }

        public ObjectStateData(Vector3 pos, Quaternion rot)
        {
            position = new float[] { pos.x, pos.y, pos.z };
            rotation = new float[] { rot.x, rot.y, rot.z, rot.w };
        }

        public Vector3 GetPosition() => new Vector3(position[0], position[1], position[2]);
        public Quaternion GetRotation() => new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]);
    }

    [System.Serializable]
    public class GameData
    {
        [Header("Metadata")]
        public long lastUpdated;
        public int slotIndex;
        public string runConfigurationID; // Pour l'aspect aléatoire de la run
        public bool isNewGame = true;

        [Header("Player State")]
        public int currentHealth;
        public float[] playerPosition = new float[3];
        public float[] playerRotation = new float[4];
        
        public float[] respawnPosition = new float[3];
        public float[] respawnRotation = new float[4];
        public bool hasCheckpoint = false;

        [Header("Inventory")]
        public List<string> inventoryItemNames = new List<string>();

        [Header("World Progress")]
        public List<string> openedChests = new List<string>(); // Liste des IDs de coffres ouverts
        public List<string> solvedPuzzles = new List<string>(); // Liste des IDs de puzzles résolus
        public List<string> seenTutorials = new List<string>(); // Liste des IDs de tutos déjà vus
        
        [Header("Dynamic Objects")]
        public SerializableDictionary<string, ObjectStateData> objectStates = new SerializableDictionary<string, ObjectStateData>();

        // Pour plus de flexibilité sans changer la structure à chaque fois
        public SerializableDictionary<string, string> customFlags = new SerializableDictionary<string, string>();

        public GameData()
        {
            // Valeurs par défaut pour une nouvelle partie
            currentHealth = 3;
            runConfigurationID = "default";
            lastUpdated = System.DateTime.Now.ToBinary();
        }
    }

    /// <summary>
    /// Une petite classe utilitaire pour sérialiser des dictionnaires en JSON
    /// car Unity ne supporte pas nativement la sérialisation des Dictionary.
    /// </summary>
    [System.Serializable]
    public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys = new List<TKey>();
        [SerializeField] private List<TValue> values = new List<TValue>();

        private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

        public Dictionary<TKey, TValue> ToDictionary() => dictionary;

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var pair in dictionary)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            dictionary.Clear();
            for (int i = 0; i < keys.Count; i++)
            {
                dictionary[keys[i]] = values[i];
            }
        }
        
        public void Set(TKey key, TValue value) => dictionary[key] = value;
        public TValue Get(TKey key) => dictionary.ContainsKey(key) ? dictionary[key] : default;
        public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);
    }
}
