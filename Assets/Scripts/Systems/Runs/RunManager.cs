using UnityEngine;
using System.Collections.Generic;
using PuzzleDungeon.Systems.Save;

namespace PuzzleDungeon.Systems.Runs
{
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        [SerializeField] private List<RunConfiguration> _availableConfigs = new List<RunConfiguration>();

        public List<RunConfiguration> AvailableConfigs => _availableConfigs;

        private Dictionary<string, UniqueIdentifier> _sceneObjects = new Dictionary<string, UniqueIdentifier>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // On force une première recherche pour être sûr de ne rien rater
                RefreshSceneRegistry();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void RefreshSceneRegistry()
        {
            _sceneObjects.Clear();
            UniqueIdentifier[] identifiers = FindObjectsOfType<UniqueIdentifier>(true);
            foreach (var id in identifiers)
            {
                if (!string.IsNullOrEmpty(id.Id))
                {
                    _sceneObjects[id.Id] = id;
                }
            }
            Debug.Log($"[RunManager] Registry refreshed: {_sceneObjects.Count} objects found.");
        }

        public void RegisterObject(UniqueIdentifier identifier)
        {
            if (identifier == null || string.IsNullOrEmpty(identifier.Id)) return;
            _sceneObjects[identifier.Id] = identifier;
        }

        public void UnregisterObject(string id)
        {
            if (_sceneObjects.ContainsKey(id)) _sceneObjects.Remove(id);
        }

        public void ApplyRunConfiguration(string configID)
        {
            RunConfiguration config = _availableConfigs.Find(c => c.ConfigurationID == configID);
            
            if (config == null)
            {
                Debug.LogWarning($"[RunManager] Configuration {configID} not found!");
                return;
            }

            // Si le registre est vide (cas rare au démarrage), on rafraîchit
            if (_sceneObjects.Count == 0) RefreshSceneRegistry();

            Debug.Log($"[RunManager] Applying Configuration: {config.ConfigurationID}");

            // 1. Appliquer les variations de la configuration principale
            foreach (var variation in config.Variations)
            {
                ApplyVariation(variation);
            }

            // 2. Appliquer les configurations de décor liées
            if (config.DecorConfigs != null)
            {
                foreach (var decorConfig in config.DecorConfigs)
                {
                    if (decorConfig == null) continue;

                    Debug.Log($"[RunManager] Applying Decor Configuration: {decorConfig.name}");
                    foreach (var variation in decorConfig.Variations)
                    {
                        ApplyVariation(variation);
                    }
                }
            }
        }

        private void ApplyVariation(ObjectVariation variation)
        {
            if (_sceneObjects.TryGetValue(variation.ObjectID, out UniqueIdentifier identifier))
            {
                GameObject obj = identifier.gameObject;

                if (variation.ChangePosition)
                    obj.transform.position = variation.Position;

                if (variation.ChangeRotation)
                    obj.transform.rotation = Quaternion.Euler(variation.Rotation);

                if (variation.ChangeActiveState)
                    obj.SetActive(variation.IsActive);

                Debug.Log($"[RunManager] Variation applied to {variation.ObjectID}");
            }
            else
            {
                Debug.LogWarning($"[RunManager] Object with ID {variation.ObjectID} not found in scene registry.");
            }
        }
    }
}
