using UnityEngine;

namespace PuzzleDungeon.Interactions
{
    /// <summary>
    /// Add this to a child GameObject of a PressurePlate with a Trigger Collider.
    /// It notifies the parent PressurePlate to trigger an auto-jump when the player enters.
    /// </summary>
    public class PressurePlateJumpZone : MonoBehaviour
    {
        private PressurePlate _parentPlate;

        private void Awake()
        {
            _parentPlate = GetComponentInParent<PressurePlate>();
            if (_parentPlate == null)
            {
                Debug.LogError($"[PressurePlateJumpZone] {name} must be a child of a PressurePlate!", this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_parentPlate != null)
            {
                _parentPlate.NotifyAutoJumpZone(other);
            }
        }
    }
}
