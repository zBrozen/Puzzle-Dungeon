using UnityEngine;
using System.Collections;
using PuzzleDungeon.Player;
using PuzzleDungeon.UI;

namespace PuzzleDungeon.Interactions
{
    public class DemoEndManager : MonoBehaviour
    {
        public static DemoEndManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float _delayBeforeCinematic = 0.5f;
        [SerializeField] private float _cinematicDuration = 3.0f;
        [SerializeField] private float _fadeDuration = 1.5f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void TriggerDemoEnd()
        {
            StartCoroutine(EndSequenceRoutine(null));
        }

        public void StartEndSequence(PuzzleDoor door)
        {
            StartCoroutine(EndSequenceRoutine(door));
        }

        private IEnumerator EndSequenceRoutine(PuzzleDoor door)
        {
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null) player.IsLocked = true;

            yield return new WaitForSeconds(_delayBeforeCinematic);

            // Let the door open if one is provided
            if (door != null)
            {
                door.Open();
            }


            yield return new WaitForSeconds(_cinematicDuration);

            // Fade to black
            if (ScreenFadeManager.Instance != null)
            {
                ScreenFadeManager.Instance.FadeToBlack(_fadeDuration);
            }

            yield return new WaitForSeconds(_fadeDuration);

            // Show End UI
            if (DemoEndUI.Instance != null)
            {
                DemoEndUI.Instance.Show();
            }
            else
            {
                Debug.LogError("[DemoEndManager] DemoEndUI instance not found!");
            }
        }
    }
}
