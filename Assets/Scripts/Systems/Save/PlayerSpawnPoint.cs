using UnityEngine;

namespace PuzzleDungeon.Systems.Save
{
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [SerializeField] private bool _isDefault = true;
        
        public bool IsDefault => _isDefault;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1f);
            
            // Draw a simple player silhouette
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, new Vector3(0.5f, 2f, 0.5f));
        }
    }
}
