using UnityEngine;
using PuzzleDungeon.Player;

namespace PuzzleDungeon.Interactions
{
    [RequireComponent(typeof(Collider))]
    public class VoidTrigger : MonoBehaviour
    {
        [Header("Respawn Settings")]
        [SerializeField, Tooltip("Le point où le joueur sera téléporté après être tombé.")]
        private Transform _spawnPoint;
        
        [Header("Effects (Optional)")]
        [SerializeField, Tooltip("Son joué lors de la téléportation.")]
        private AudioClip _respawnSound;
        
        [SerializeField, Tooltip("AudioSource optionnel. S'il n'est pas assigné, utilisera AudioSource.PlayClipAtPoint.")]
        private AudioSource _audioSource;
        
        [SerializeField, Tooltip("Particules jouées au point d'apparition (optionnel).")]
        private ParticleSystem _respawnParticles;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true; // S'assurer que la zone est bien en trigger
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PlayerController player))
            {
                // 1. Téléporter d'abord au bord (spawn local du VoidTrigger)
                RespawnPlayer(player);

                // 2. Appliquer les dégâts (déclenche l'animation GetHitVoid)
                if (other.TryGetComponent(out PlayerHealth health))
                {
                    health.TakeDamage(1, DamageType.Void);

                    // Si le joueur a survécu, on lance la séquence de blocage manuellement.
                    // Si le joueur est mort, PlayerHealth.Respawn() l'a déjà téléporté
                    // au vrai Checkpoint et a lancé la séquence en interne.
                    if (!health.IsRespawning)
                    {
                        health.StartRespawnSequence();
                    }
                }
            }
        }

        private void RespawnPlayer(PlayerController player)
        {
            if (_spawnPoint == null)
            {
                Debug.LogWarning($"[VoidTrigger] Aucun point de spawn défini pour la zone {gameObject.name} !", this);
                return;
            }

            // Jouer le son
            if (_respawnSound != null)
            {
                if (_audioSource != null)
                {
                    _audioSource.PlayOneShot(_respawnSound);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(_respawnSound, _spawnPoint.position);
                }
            }

            // Jouer les particules
            if (_respawnParticles != null)
            {
                // Déplacer les particules au point de spawn et les jouer
                _respawnParticles.transform.position = _spawnPoint.position;
                _respawnParticles.transform.rotation = _spawnPoint.rotation;
                _respawnParticles.Play();
            }

            // Téléporter le joueur
            player.Teleport(_spawnPoint.position, _spawnPoint.rotation);
        }
    }
}
