using UnityEngine;
using UnityEngine.UI;

namespace PuzzleDungeon.Player
{
    public class ScarabReticleUI : MonoBehaviour
    {
        [SerializeField] private GameObject _reticleRoot;

        private void Awake()
        {
            if (_reticleRoot == null) _reticleRoot = gameObject;
            
            // Sécurité : On s'assure que le Canvas parent est bien visible par dessus tout
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 999; 
            }
            
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (_reticleRoot != null)
            {
                _reticleRoot.SetActive(visible);
            }
        }
    }
}
