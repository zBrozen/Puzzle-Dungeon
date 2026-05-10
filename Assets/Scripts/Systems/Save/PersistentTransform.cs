using UnityEngine;

namespace PuzzleDungeon.Systems.Save
{
    /// <summary>
    /// Composant à ajouter à n'importe quel objet dont on souhaite sauvegarder la position et la rotation.
    /// Nécessite un UniqueIdentifier sur le même GameObject.
    /// </summary>
    [RequireComponent(typeof(UniqueIdentifier))]
    public class PersistentTransform : MonoBehaviour, ISaveable
    {
        private UniqueIdentifier _uid;

        public string UniqueID => _uid != null ? _uid.Id : string.Empty;

        private void Awake()
        {
            _uid = GetComponent<UniqueIdentifier>();
        }

        public void PopulateSaveData(GameData data)
        {
            if (string.IsNullOrEmpty(UniqueID)) return;

            data.objectStates.Set(UniqueID, new ObjectStateData(transform.position, transform.rotation));
        }

        public void LoadFromSaveData(GameData data)
        {
            if (string.IsNullOrEmpty(UniqueID)) return;

            if (data.objectStates.ContainsKey(UniqueID))
            {
                ObjectStateData state = data.objectStates.Get(UniqueID);
                
                // On téléporte via Rigidbody si présent pour éviter les problèmes de physique
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.position = state.GetPosition();
                    rb.rotation = state.GetRotation();
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                else
                {
                    transform.position = state.GetPosition();
                    transform.rotation = state.GetRotation();
                }
                
                Debug.Log($"[PersistentTransform] Restored {name} to {transform.position}");
            }
        }
    }
}
