using UnityEngine;
using UnityEngine.SceneManagement;
using PuzzleDungeon.Systems.Save;

namespace PuzzleDungeon.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Pages")]
        [SerializeField] private GameObject mainPage;
        [SerializeField] private GameObject saveSlotPage;
        [SerializeField] private GameObject settingsPage;

        [Header("Scene Loading")]
        [SerializeField] private string gameSceneName = "GameScene";

        private void Start()
        {
            // Show main page by default
            ShowMainPage();
            
            // Ensure cursor is visible
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void ShowMainPage()
        {
            mainPage.SetActive(true);
            saveSlotPage.SetActive(false);
            if (settingsPage != null) settingsPage.SetActive(false);
        }

        public void ShowSaveSlots()
        {
            mainPage.SetActive(false);
            saveSlotPage.SetActive(true);
            
            // Refresh slots when showing the page
            SaveSlotUI[] slots = saveSlotPage.GetComponentsInChildren<SaveSlotUI>();
            foreach (var slot in slots)
            {
                slot.Refresh();
            }
        }

        public void ShowSettings()
        {
            mainPage.SetActive(false);
            if (settingsPage != null) settingsPage.SetActive(true);
        }

        public void StartGameWithSlot(int slotIndex)
        {
            if (PersistenceManager.Instance == null)
            {
                Debug.LogError("PersistenceManager instance not found! Make sure you have a PersistenceManager in your scene.");
                SceneManager.LoadScene(gameSceneName); // Load anyway? Or return?
                return;
            }

            if (SaveSystem.HasSave(slotIndex))
            {
                PersistenceManager.Instance.LoadGame(slotIndex);
            }
            else
            {
                PersistenceManager.Instance.NewGame(slotIndex);
            }

            SceneManager.LoadScene(gameSceneName);
        }

        public void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}
