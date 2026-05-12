using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using PuzzleDungeon.Systems.Save;

namespace PuzzleDungeon.UI
{
    public class DemoEndUI : MonoBehaviour
    {
        public static DemoEndUI Instance { get; private set; }

        [Header("UI Elements")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Image _endImage;
        [SerializeField] private Button _saveAndQuitButton;

        [Header("Content")]
        [SerializeField] private string _title = "Fin de la Démo";
        [SerializeField] private string _message = "Merci d'avoir joué à Vestige !\nVotre progression a été sauvegardée.";
        [SerializeField] private string _mainMenuSceneName = "MainMenu";

        private void Awake()
        {
            Instance = this;
            
            // Force hide immediately
            if (_panel != null) _panel.SetActive(false);
        }

        private void Start()
        {
            // Second pass safety
            if (_panel != null) _panel.SetActive(false);

            if (_saveAndQuitButton != null)
            {
                _saveAndQuitButton.onClick.AddListener(SaveAndQuit);
            }
        }




        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
            
            if (_titleText != null) _titleText.text = _title;
            if (_messageText != null) _messageText.text = _message;
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }


        private void SaveAndQuit()
        {
            if (PersistenceManager.Instance != null)
            {
                // Reset player to spawn for the next load
                if (PersistenceManager.Instance.CurrentData != null)
                {
                    PersistenceManager.Instance.CurrentData.isNewGame = true;
                }
                
                PersistenceManager.Instance.SaveGame();
            }

            SceneManager.LoadScene(_mainMenuSceneName);
        }
    }
}
