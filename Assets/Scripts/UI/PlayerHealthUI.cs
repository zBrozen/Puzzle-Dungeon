using UnityEngine;
using UnityEngine.UI;
using PuzzleDungeon.Player;

namespace PuzzleDungeon.UI
{
    public class PlayerHealthUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Référence au script PlayerHealth du joueur dans la scène")]
        private PlayerHealth _playerHealth;

        [Header("UI Elements")]
        [SerializeField, Tooltip("Les images UI représentant les coeurs (dans l'ordre)")]
        private Image[] _heartImages;

        [Header("Sprites")]
        [SerializeField, Tooltip("Le sprite utilisé quand le coeur est plein")]
        private Sprite _fullHeartSprite;
        
        [SerializeField, Tooltip("Le sprite utilisé quand le coeur est vide")]
        private Sprite _emptyHeartSprite;

        private void Start()
        {
            if (_playerHealth == null)
            {
                // Tente de le trouver automatiquement s'il n'est pas assigné
                _playerHealth = FindObjectOfType<PlayerHealth>();
                
                if (_playerHealth == null)
                {
                    Debug.LogError("[PlayerHealthUI] Impossible de trouver le script PlayerHealth !");
                    return;
                }
            }

            // On s'abonne à l'événement du joueur pour être notifié quand sa vie change
            _playerHealth.OnHealthChanged += UpdateHearts;

            // On initialise l'affichage au démarrage
            UpdateHearts(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
        }

        private void OnDestroy()
        {
            // Il est important de se désabonner des événements quand l'objet est détruit
            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged -= UpdateHearts;
            }
        }

        /// <summary>
        /// Met à jour l'apparence des coeurs en fonction de la vie actuelle.
        /// </summary>
        private void UpdateHearts(int currentHealth, int maxHealth)
        {
            for (int i = 0; i < _heartImages.Length; i++)
            {
                // Si l'index du coeur est inférieur à la vie actuelle, on le met plein, sinon vide
                if (i < currentHealth)
                {
                    _heartImages[i].sprite = _fullHeartSprite;
                }
                else
                {
                    _heartImages[i].sprite = _emptyHeartSprite;
                }

                // Optionnel : si le nombre de coeurs max peut changer en cours de jeu,
                // on active/désactive les images en fonction du maxHealth
                if (i < maxHealth)
                {
                    _heartImages[i].enabled = true;
                }
                else
                {
                    _heartImages[i].enabled = false;
                }
            }
        }
    }
}
