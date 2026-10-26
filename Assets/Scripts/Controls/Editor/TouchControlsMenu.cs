using UnityEditor;
using PuzzleDungeon.Controls;

namespace PuzzleDungeon.Controls.EditorTools
{
    /// <summary>
    /// Menu Vestige > Contrôles tactiles dans l'éditeur : affiche les contrôles mobiles
    /// en Play Mode sans dépendre du Device Simulator.
    /// </summary>
    public static class TouchControlsMenu
    {
        private const string MenuPath = "Vestige/Contrôles tactiles dans l'éditeur";

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            bool enabled = !EditorPrefs.GetBool(GameInput.ForceTouchEditorPrefKey, false);
            EditorPrefs.SetBool(GameInput.ForceTouchEditorPrefKey, enabled);
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(GameInput.ForceTouchEditorPrefKey, false));
            return !EditorApplication.isPlaying; // à changer hors Play Mode, pris en compte au prochain lancement
        }
    }
}
