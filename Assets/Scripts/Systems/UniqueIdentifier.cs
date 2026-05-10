using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PuzzleDungeon.Systems
{
    /// <summary>
    /// Permet d'identifier de manière unique un objet dans la scène pour le système de sauvegarde
    /// et de configuration de run.
    /// </summary>
    [DisallowMultipleComponent]
    public class UniqueIdentifier : MonoBehaviour
    {
        [SerializeField] private string _id;

        public string Id => _id;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                GenerateID();
            }
            else
            {
                CheckForDuplicates();
            }
        }

        private void Awake()
        {
            Register();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        private void Register()
        {
            if (string.IsNullOrEmpty(_id)) return;
            
            if (Runs.RunManager.Instance != null)
            {
                Runs.RunManager.Instance.RegisterObject(this);
            }
        }

        private void Unregister()
        {
            if (Runs.RunManager.Instance != null && !string.IsNullOrEmpty(_id))
            {
                Runs.RunManager.Instance.UnregisterObject(_id);
            }
        }

        [ContextMenu("Generate New ID")]
        public void GenerateID()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RecordObject(this, "Generate Unique ID");
            }
#endif
            
            _id = System.Guid.NewGuid().ToString().Substring(0, 8);
            
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                // Pour les instances de prefab, on enregistre la modification de propriété
                PrefabUtility.RecordPrefabInstancePropertyModifications(this);
                Debug.Log($"[UniqueIdentifier] Generated new ID: {_id} for {gameObject.name}", gameObject);
            }
#endif
        }

        private void CheckForDuplicates()
        {
#if UNITY_EDITOR
            if (Application.isPlaying || string.IsNullOrEmpty(_id)) return;

            UniqueIdentifier[] allIds = Object.FindObjectsByType<UniqueIdentifier>(FindObjectsSortMode.None);
            foreach (var other in allIds)
            {
                if (other != this && other.Id == _id)
                {
                    Debug.LogWarning($"[UniqueIdentifier] Duplicate ID '{_id}' detected on '{gameObject.name}' and '{other.gameObject.name}'! Use the Context Menu to generate a new unique ID.", gameObject);
                    break;
                }
            }
#endif
        }
    }
}

