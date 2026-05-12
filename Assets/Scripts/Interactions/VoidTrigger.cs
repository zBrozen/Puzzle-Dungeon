using UnityEngine;
using PuzzleDungeon.Player;
using UnityEngine.Events;
using PuzzleDungeon.Systems;

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

        [Header("Events")]
        public UnityEvent onVoidEnter;

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
            // Log pour vérifier que le trigger s'active bien en build
            Debug.Log($"[VoidTrigger] {gameObject.name} touchée par {other.name} (Layer: {other.gameObject.layer})");

            if (other.TryGetComponent(out PlayerController player))
            {
                // Trigger the reset event
                if (onVoidEnter != null) onVoidEnter.Invoke();

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
                Debug.Log($"[VoidTrigger] Tentative de lecture du son : {_respawnSound.name}");

                if (_audioSource != null && _audioSource.gameObject.activeInHierarchy)
                {
                    _audioSource.PlayOneShot(_respawnSound);
                }
                else if (AudioManager.Instance != null)
                {
                    // On passe par le Singleton global via la méthode persistante
                    AudioManager.Instance.PlayGlobalSFX(_respawnSound);
                }
                else
                {
                    // Fallback ultime : PlayClipAtPoint (crée un objet temporaire)
                    // On le joue à la position de la caméra pour être sûr qu'on l'entende (2D-ish)
                    Vector3 soundPos = Camera.main != null ? Camera.main.transform.position : transform.position;
                    AudioSource.PlayClipAtPoint(_respawnSound, soundPos);
                }
            }
            else
            {
                Debug.LogWarning($"[VoidTrigger] Aucun son de respawn assigné sur {gameObject.name}");
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
