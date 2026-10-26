using UnityEngine;

namespace PuzzleDungeon.Controls
{
    public enum GameButton { Action, Scarab, Attack, Interact, Pause }

    /// <summary>
    /// Point d'entrée unique des inputs : fusionne clavier/souris (Input Manager)
    /// et contrôles tactiles (alimentés par MobileControls).
    /// </summary>
    public static class GameInput
    {
        private const int ButtonCount = 5;

        /// <summary>Préférence éditeur (menu Vestige) pour tester le tactile sans appareil.</summary>
        public const string ForceTouchEditorPrefKey = "Vestige.ForceTouchControls";

        /// <summary>Forcé à true pour tester le tactile dans l'éditeur (menu Vestige ou MobileControls).</summary>
        public static bool ForceTouchControls { get; set; }

        public static bool TouchControlsActive => Application.isMobilePlatform || ForceTouchControls;

        // Valeurs tactiles en attente (écrites par les events UI, n'importe quand dans la frame)
        private static Vector2 _pendingLook;
        private static readonly bool[] _pendingDown = new bool[ButtonCount];

        // Valeurs tactiles de la frame courante (figées par BeginFrame)
        private static Vector2 _touchMove;
        private static Vector2 _touchLook;
        private static readonly bool[] _touchDown = new bool[ButtonCount];

        // Boutons utilisables en ce moment (ex : Interagir seulement devant un objet) : MobileControls masque les autres
        private static readonly bool[] _available = { true, true, true, true, true };

        /// <summary>Déplacement (joueur / scarabée). raw = sans lissage clavier.</summary>
        public static Vector2 GetMove(bool raw)
        {
            Vector2 keyboard = raw
                ? new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
                : new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            return keyboard.sqrMagnitude > 0.0001f ? keyboard : _touchMove;
        }

        /// <summary>Rotation caméra, dans l'unité des axes "Mouse X/Y".</summary>
        public static Vector2 GetLook()
        {
            // Sur mobile, le toucher est aussi simulé en souris : on ignore la souris
            // pour que le joystick ne fasse pas tourner la caméra.
            if (TouchControlsActive) return _touchLook;
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }

        public static float GetZoom()
        {
            if (TouchControlsActive) return 0f;
            return Input.GetAxis("Mouse ScrollWheel");
        }

        public static bool GetButtonDown(GameButton button, KeyCode key)
        {
            if (_touchDown[(int)button]) return true;

            // Un toucher est aussi vu comme un clic souris : sans ce filtre,
            // chaque tap sur l'écran déclencherait une attaque.
            if (TouchControlsActive && key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6) return false;

            return Input.GetKeyDown(key);
        }

        public static void SetButtonAvailable(GameButton button, bool available) => _available[(int)button] = available;

        public static bool IsButtonAvailable(GameButton button) => _available[(int)button];

        /// <summary>
        /// Adapte un texte de tutoriel écrit pour le clavier ("touches ZQSD", "ESPACE"...)
        /// quand on joue au tactile.
        /// </summary>
        public static string AdaptText(string text)
        {
            if (!TouchControlsActive || string.IsNullOrEmpty(text)) return text;

            return text
                .Replace("les touches ZQSD", "le joystick")
                .Replace("la touche ESPACE", "le bouton Action")
                .Replace("ESPACE", "le bouton Action")
                .Replace("ZQSD", "le joystick");
        }

        // --- API pour les contrôles tactiles ---

        public static void SetTouchMove(Vector2 move) => _touchMove = Vector2.ClampMagnitude(move, 1f);

        public static void AddTouchLook(Vector2 delta) => _pendingLook += delta;

        public static void PressTouchButton(GameButton button) => _pendingDown[(int)button] = true;

        /// <summary>
        /// Appelé en tout début de frame (MobileControls a un ordre d'exécution très bas) :
        /// les appuis/glissements reçus depuis la frame précédente deviennent visibles
        /// pendant exactement une frame, quel que soit l'ordre d'exécution de l'EventSystem.
        /// </summary>
        public static void BeginFrame()
        {
            _touchLook = _pendingLook;
            _pendingLook = Vector2.zero;

            for (int i = 0; i < ButtonCount; i++)
            {
                _touchDown[i] = _pendingDown[i];
                _pendingDown[i] = false;
            }
        }

        // Remise à zéro même si le "domain reload" est désactivé dans l'éditeur
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetTouch()
        {
#if UNITY_EDITOR
            ForceTouchControls = UnityEditor.EditorPrefs.GetBool(ForceTouchEditorPrefKey, false);
#else
            ForceTouchControls = false;
#endif
            _touchMove = _touchLook = _pendingLook = Vector2.zero;
            for (int i = 0; i < ButtonCount; i++)
            {
                _touchDown[i] = _pendingDown[i] = false;
                _available[i] = true;
            }
        }
    }
}
