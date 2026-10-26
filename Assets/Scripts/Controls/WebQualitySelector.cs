using UnityEngine;

namespace PuzzleDungeon.Controls
{
    /// <summary>
    /// Le build WebGL démarre en qualité "Medium" (pensée pour mobile).
    /// Sur un navigateur de bureau, on remonte en "High".
    /// </summary>
    public static class WebQualitySelector
    {
        private const string DesktopQualityName = "High";

#if UNITY_WEBGL && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void SelectQuality()
        {
            if (Application.isMobilePlatform) return;

            int index = System.Array.IndexOf(QualitySettings.names, DesktopQualityName);
            if (index >= 0) QualitySettings.SetQualityLevel(index, true);
        }
#endif
    }
}
