using UnityEngine;
using PuzzleDungeon.Player;
using PuzzleDungeon.Systems.Save;

namespace PuzzleDungeon.Interactions
{
    [RequireComponent(typeof(BoxCollider))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Feedback")]
        [SerializeField] private ParticleSystem _activationParticles;
        [SerializeField] private AudioClip _activationSound;
        [SerializeField] private AudioSource _audioSource;

        [Header("Settings")]
        [SerializeField] private bool _oneTimeUse = false;
        private bool _isActivated = false;

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_oneTimeUse && _isActivated) return;

            if (other.TryGetComponent(out PlayerHealth health))
            {
                // On ne réactive pas si c'est déjà notre point de respawn actuel
                if (health.RespawnPosition == transform.position) return;

                ActivateCheckpoint(health);
            }
        }

        private void ActivateCheckpoint(PlayerHealth health)
        {
            _isActivated = true;
            
            Debug.Log($"[Checkpoint] Activated at {transform.position}");

            // Mettre à jour le point de respawn et sauvegarder
            health.CheckpointReached(transform.position, transform.rotation);

            // Feedback visuel
            if (_activationParticles != null)
            {
                _activationParticles.Play();
            }

            // Feedback sonore
            if (_activationSound != null)
            {
                if (_audioSource != null)
                {
                    _audioSource.PlayOneShot(_activationSound);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(_activationSound, transform.position);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, Vector3.one);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);
            
            // Draw a small flag or icon
            Gizmos.DrawSphere(transform.position + Vector3.up * 1f, 0.2f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1f);
        }
    }
}
