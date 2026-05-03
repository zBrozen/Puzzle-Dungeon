using UnityEngine;

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
            // Si l'ID est vide, on peut proposer le nom de l'objet par défaut
            // ou générer un GUID si on veut être très strict.
            if (string.IsNullOrEmpty(_id))
            {
                _id = name;
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
        private void GenerateID()
        {
            _id = System.Guid.NewGuid().ToString().Substring(0, 8);
        }
    }
}
