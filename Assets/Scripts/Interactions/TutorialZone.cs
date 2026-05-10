using UnityEngine;
using PuzzleDungeon.Systems.Save;
using PuzzleDungeon.UI;

namespace PuzzleDungeon.Interactions
{
    [RequireComponent(typeof(Collider))]
    public class TutorialZone : MonoBehaviour
    {
        [Header("Tutorial Data")]
        [SerializeField] private string _tutorialID;
        [SerializeField, TextArea(3, 5)] private string _message;
        [SerializeField] private Sprite _image;
        [SerializeField] private float _displayDuration = 4f;

        private void Awake()
        {
            // Ensure the collider is a trigger
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if it's the player
            if (other.CompareTag("Player"))
            {
                TriggerTutorial();
            }
        }

        private void TriggerTutorial()
        {
            if (PersistenceManager.Instance == null) return;

            // Check if already seen
            if (PersistenceManager.Instance.CurrentData.seenTutorials.Contains(_tutorialID))
            {
                return;
            }

            // Show UI
            if (TutorialUIManager.Instance != null)
            {
                TutorialUIManager.Instance.ShowTutorial(_message, _image, _displayDuration);
            }

            // Mark as seen and save
            PersistenceManager.Instance.CurrentData.seenTutorials.Add(_tutorialID);
            PersistenceManager.Instance.SaveGame();
            
            Debug.Log($"[TutorialZone] Tutorial {_tutorialID} shown and marked as seen.");
        }
    }
}
