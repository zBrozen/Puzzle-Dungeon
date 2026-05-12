using UnityEngine;
using UnityEngine.SceneManagement;
using PuzzleDungeon.Systems.Save;
using PuzzleDungeon.Player;

namespace PuzzleDungeon.Systems
{
    /// <summary>
    /// Gère la pause du jeu. Autorité unique sur le curseur et l'état du joueur.
    /// Le panel de pause utilise le même prefab SettingsMenu que le Main Menu.
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        public static PauseManager Instance { get; private set; }

        [Header("UI")]
        [Tooltip("Le panel de pause (prefab partagé avec le Main Menu, contenant SettingsMenu + boutons supplémentaires)")]
        [SerializeField] private GameObject _pausePanel;

        private bool _isPaused = false;
        public bool IsPaused => _isPaused;

        private PlayerController _playerController;
        private CameraController _cameraController;

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

        private void Start()
        {
            if (_pausePanel != null) _pausePanel.SetActive(false);

            // Vérifier qu'un EventSystem existe, sinon en créer un
            EnsureEventSystem();

            // Récupérer les références
            _playerController = FindObjectOfType<PlayerController>();
            _cameraController = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;

            // L'état initial : jeu actif, curseur verrouillé
            SetCursorState(false);
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                Debug.Log("[PauseManager] No EventSystem found. Creating one automatically.");
                GameObject esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_isPaused) Resume();
                else Pause();
            }
        }

        public void Pause()
        {
            _isPaused = true;
            Time.timeScale = 0f;

            if (_pausePanel != null) _pausePanel.SetActive(true);

            // Verrouiller le joueur et la caméra
            if (_playerController != null) _playerController.IsLocked = true;
            if (_cameraController != null) _cameraController.IsInputDisabled = true;

            // Le curseur doit être appliqué au frame suivant pour que l'EventSystem l'accepte
            StartCoroutine(ApplyCursorNextFrame(true));
        }

        public void Resume()
        {
            _isPaused = false;
            Time.timeScale = 1f;

            if (_pausePanel != null) _pausePanel.SetActive(false);

            // Déverrouiller le joueur et la caméra
            if (_playerController != null) _playerController.IsLocked = false;
            if (_cameraController != null) _cameraController.IsInputDisabled = false;

            StartCoroutine(ApplyCursorNextFrame(false));
        }

        private System.Collections.IEnumerator ApplyCursorNextFrame(bool visible)
        {
            // WaitForSecondsRealtime fonctionne même avec timeScale = 0
            yield return new WaitForSecondsRealtime(0.02f);
            SetCursorState(visible);
        }

        private void SetCursorState(bool visible)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }

        /// <summary>
        /// Sauvegarde la partie en cours (bouton "Enregistrer").
        /// </summary>
        public void SaveGame()
        {
            if (PersistenceManager.Instance != null)
            {
                PersistenceManager.Instance.SaveGame();
                Debug.Log("[PauseManager] Game saved via pause menu.");
            }
            else
            {
                Debug.LogWarning("[PauseManager] PersistenceManager not found, cannot save.");
            }
        }

        /// <summary>
        /// Sauvegarde puis retourne au Main Menu.
        /// </summary>
        public void SaveAndQuitToMainMenu()
        {
            SaveGame();
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        /// <summary>
        /// Quitte sans sauvegarder.
        /// </summary>
        public void QuitToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
