using UnityEngine;
using System;

namespace PuzzleDungeon.Interactions
{
    public class ScarabRing : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private MeshRenderer _ringRenderer;
        [SerializeField] private Collider _ringCollider;
        
        public event Action<ScarabRing, Scarab> OnPassedThrough;
        private bool _isActive = false;

        private void Awake()
        {
            if (_ringRenderer == null) _ringRenderer = GetComponentInChildren<MeshRenderer>();
            if (_ringCollider == null) _ringCollider = GetComponentInChildren<Collider>();
        }

        public void SetState(bool isNext, bool isUpcoming, Material nextMat, Material upcomingMat)
        {
            if (_ringRenderer == null) return;

            if (isNext)
            {
                _ringRenderer.enabled = true;
                if (_ringCollider != null) _ringCollider.enabled = true;
                _ringRenderer.material = nextMat;
                _isActive = true;
            }
            else if (isUpcoming)
            {
                _ringRenderer.enabled = true;
                if (_ringCollider != null) _ringCollider.enabled = true;
                _ringRenderer.material = upcomingMat;
                _isActive = false;
            }
            else
            {
                _ringRenderer.enabled = false;
                if (_ringCollider != null) _ringCollider.enabled = false;
                _isActive = false;
            }
        }

        public void TriggerPassage(Scarab scarab)
        {
            if (scarab != null && _isActive)
            {
                OnPassedThrough?.Invoke(this, scarab);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // On vérifie si c'est le scarabée
            Scarab scarab = other.GetComponentInParent<Scarab>();
            TriggerPassage(scarab);
        }
    }
}
