using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PuzzleDungeon.Player;
using PuzzleDungeon.Systems;
using PuzzleDungeon.UI;

namespace PuzzleDungeon.Controls
{
    /// <summary>
    /// Construit l'interface tactile au lancement (joystick, zone caméra, boutons)
    /// et fige les inputs tactiles en début de frame.
    /// Créé automatiquement sur mobile ; dans l'éditeur, on peut le poser dans la scène
    /// avec "Force In Editor" pour tester, ou utiliser le Device Simulator.
    /// Les contrôles de jeu sont masqués hors gameplay (menu principal, pause, fin de démo)
    /// pour laisser les menus recevoir les touches.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class MobileControls : MonoBehaviour
    {
        [SerializeField] private bool _forceInEditor = false;

        [Header("Layout (unités du Canvas, référence 1920x1080)")]
        [SerializeField] private float _joystickSize = 260f;
        [SerializeField] private Vector2 _joystickRestPosition = new Vector2(260f, 260f);
        [SerializeField] private float _attackButtonSize = 190f;
        [SerializeField] private float _actionButtonSize = 150f;
        [SerializeField] private float _smallButtonSize = 130f;
        [SerializeField] private float _pauseButtonSize = 90f;
        [SerializeField] private Color _tint = new Color(1f, 1f, 1f, 0.35f);

        private static MobileControls _instance;
        private static Sprite _circleSprite;

        private RectTransform _safeArea;
        private GameObject _gameplayRoot;
        private GameObject _portraitOverlay;
        private readonly List<TouchButton> _buttons = new List<TouchButton>();
        private PlayerController _player;
        private Rect _appliedSafeArea;
        private Vector2Int _appliedScreenSize;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!GameInput.TouchControlsActive)
            {
#if UNITY_EDITOR
                Debug.Log("[MobileControls] Contrôles tactiles désactivés : plateforme non mobile " +
                          $"(isMobilePlatform = {Application.isMobilePlatform}). " +
                          "Pour les forcer : menu Vestige > Contrôles tactiles dans l'éditeur.");
#endif
                return;
            }
            if (FindAnyObjectByType<MobileControls>() != null) return;

            new GameObject("MobileControls").AddComponent<MobileControls>();
        }

        private void Awake()
        {
#if UNITY_EDITOR
            if (_forceInEditor) GameInput.ForceTouchControls = true;
#endif
            if (_instance != null || !GameInput.TouchControlsActive)
            {
                enabled = false; // Destroy n'est effectif qu'en fin de frame
                Destroy(gameObject);
                return;
            }

            _instance = this;
            transform.SetParent(null); // DontDestroyOnLoad n'accepte qu'un objet racine
            DontDestroyOnLoad(gameObject);
            BuildUI();

            SceneManager.sceneLoaded += HandleSceneLoaded;
            _player = FindAnyObjectByType<PlayerController>();
            Debug.Log($"[MobileControls] Contrôles tactiles créés (joueur trouvé : {_player != null}).");
        }

        private void OnDestroy()
        {
            if (_instance != this) return;
            _instance = null;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            GameInput.ResetTouch();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _player = FindAnyObjectByType<PlayerController>();
        }

        private void Update()
        {
            GameInput.BeginFrame();

            if (Screen.safeArea != _appliedSafeArea || Screen.width != _appliedScreenSize.x || Screen.height != _appliedScreenSize.y)
                ApplySafeArea();
            _portraitOverlay.SetActive(Screen.height > Screen.width);

            bool gameplay = IsGameplayActive();
            if (_gameplayRoot.activeSelf != gameplay) _gameplayRoot.SetActive(gameplay);

            if (gameplay)
            {
                foreach (TouchButton button in _buttons)
                {
                    bool available = GameInput.IsButtonAvailable(button.Button);
                    if (button.gameObject.activeSelf != available) button.gameObject.SetActive(available);
                }
            }

            if (EventSystem.current == null) CreateEventSystem();
        }

        private bool IsGameplayActive()
        {
            if (_player == null || !_player.isActiveAndEnabled) return false;
            if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return false;
            if (DemoEndUI.Instance != null && DemoEndUI.Instance.IsShown) return false;
            return true;
        }

        // --- Construction de l'UI ---

        private void BuildUI()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // sous le réticule du scarabée (999)

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f; // paysage : on se cale sur la hauteur

            gameObject.AddComponent<GraphicRaycaster>();

            _safeArea = CreateRect("SafeArea", transform);
            Stretch(_safeArea);
            ApplySafeArea();

            RectTransform gameplayRoot = CreateRect("Gameplay", _safeArea);
            Stretch(gameplayRoot);
            _gameplayRoot = gameplayRoot.gameObject;

            // Zone caméra : tout l'écran, derrière le reste
            RectTransform lookArea = CreateRect("LookArea", gameplayRoot);
            Stretch(lookArea);
            AddInvisibleHitbox(lookArea);
            lookArea.gameObject.AddComponent<TouchLookArea>();

            // Joystick : moitié gauche, partie basse
            RectTransform joystickArea = CreateRect("JoystickArea", gameplayRoot);
            joystickArea.anchorMin = Vector2.zero;
            joystickArea.anchorMax = new Vector2(0.45f, 0.7f);
            joystickArea.offsetMin = joystickArea.offsetMax = Vector2.zero;
            AddInvisibleHitbox(joystickArea);

            RectTransform background = CreateCircle("Background", joystickArea, _joystickSize, _tint);
            background.anchorMin = background.anchorMax = Vector2.zero;
            background.anchoredPosition = _joystickRestPosition;
            background.GetComponent<Image>().raycastTarget = false;

            Color handleTint = _tint;
            handleTint.a = Mathf.Min(1f, _tint.a * 2f);
            RectTransform handle = CreateCircle("Handle", background, _joystickSize * 0.42f, handleTint);
            handle.GetComponent<Image>().raycastTarget = false;

            joystickArea.gameObject.AddComponent<VirtualJoystick>().Setup(background, handle);

            // Boutons : grappe en bas à droite, pause en haut à droite
            Vector2 bottomRight = new Vector2(1f, 0f);
            CreateButton(gameplayRoot, "AttackButton", "Attaque", GameButton.Attack, _attackButtonSize, bottomRight, new Vector2(-180f, 180f));
            CreateButton(gameplayRoot, "ActionButton", "Action", GameButton.Action, _actionButtonSize, bottomRight, new Vector2(-400f, 120f));
            CreateButton(gameplayRoot, "InteractButton", "Interagir", GameButton.Interact, _smallButtonSize, bottomRight, new Vector2(-180f, 400f));
            CreateButton(gameplayRoot, "ScarabButton", "Scarabée", GameButton.Scarab, _smallButtonSize, bottomRight, new Vector2(-390f, 320f));
            CreateButton(gameplayRoot, "PauseButton", "II", GameButton.Pause, _pauseButtonSize, Vector2.one, new Vector2(-80f, -80f));

            _portraitOverlay = CreatePortraitOverlay();
        }

        private void CreateButton(Transform parent, string name, string label, GameButton button, float size, Vector2 anchor, Vector2 position)
        {
            RectTransform rect = CreateCircle(name, parent, size, _tint);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;

            TouchButton touchButton = rect.gameObject.AddComponent<TouchButton>();
            touchButton.Setup(button, rect.GetComponent<Image>());
            _buttons.Add(touchButton);

            Text text = CreateText(label, rect, 28);
            text.color = new Color(1f, 1f, 1f, 0.9f);
        }

        private GameObject CreatePortraitOverlay()
        {
            RectTransform overlay = CreateRect("PortraitOverlay", transform);
            Stretch(overlay);
            Image image = overlay.gameObject.AddComponent<Image>();
            image.color = Color.black; // bloque aussi les touches du jeu

            Text text = CreateText("Tournez votre appareil\nen mode paysage", overlay, 56);
            text.color = Color.white;
            return overlay.gameObject;
        }

        private void ApplySafeArea()
        {
            Rect safe = Screen.safeArea;
            _appliedSafeArea = safe;
            _appliedScreenSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            _safeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            _safeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        }

        private static void CreateEventSystem()
        {
            GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go);
        }

        // --- Helpers ---

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void AddInvisibleHitbox(RectTransform rect)
        {
            rect.gameObject.AddComponent<Image>().color = Color.clear;
        }

        private static RectTransform CreateCircle(string name, Transform parent, float size, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.sizeDelta = new Vector2(size, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = GetCircleSprite();
            image.color = color;
            return rect;
        }

        private static Text CreateText(string content, RectTransform parent, int fontSize)
        {
            RectTransform rect = CreateRect("Label", parent);
            Stretch(rect);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        // Cercle généré par code : pas besoin d'asset de sprite
        private static Sprite GetCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;

            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.5f;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    byte alpha = (byte)(Mathf.Clamp01(radius - distance) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            _circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _circleSprite;
        }
    }
}
