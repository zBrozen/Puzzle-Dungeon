using UnityEngine;

namespace PuzzleDungeon.Systems
{
    /// <summary>
    /// Niveaux de qualité proposés au joueur selon la plateforme, et changement de niveau.
    /// </summary>
    public static class QualityLevels
    {
        /// <summary>
        /// Sur le Web, "Very High" et "Ultra" activent le MSAA 2x : il faudrait recréer le contexte WebGL,
        /// ce que le navigateur ne permet pas. On s'arrête donc à "High".
        /// </summary>
        private const string WebMaxLevel = "High";

        private static bool IsWeb => Application.platform == RuntimePlatform.WebGLPlayer;

        /// <summary>Nombre de niveaux proposés (les premiers de QualitySettings.names).</summary>
        public static int Count
        {
            get
            {
                string[] names = QualitySettings.names;
                if (!IsWeb) return names.Length;

                int max = System.Array.IndexOf(names, WebMaxLevel);
                return max >= 0 ? max + 1 : names.Length;
            }
        }

        public static int Clamp(int index) => Mathf.Clamp(index, 0, Count - 1);

        /// <summary>
        /// Change le niveau de qualité et renvoie le niveau réellement appliqué.
        /// Sur le Web, on n'applique pas les changements « coûteux » (anti-aliasing) : les niveaux proposés
        /// ont tous le MSAA désactivé, il n'y a rien à recréer.
        /// Attention : ça ne suffit pas contre l'écran noir. Test du 17/11 sur vestige.brozen.fr (Chrome,
        /// Windows) : en Medium et Very Low, la scène de jeu fait planter le processus graphique du
        /// navigateur (CONTEXT_LOST_WEBGL), même au chargement direct d'une sauvegarde. High fonctionne.
        /// </summary>
        public static int Apply(int index)
        {
            index = Clamp(index);
            QualitySettings.SetQualityLevel(index, applyExpensiveChanges: !IsWeb);
            return index;
        }
    }
}
