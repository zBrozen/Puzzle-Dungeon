using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace PuzzleDungeon.Systems
{
    /// <summary>
    /// Allège le rendu sur mobile : le Bloom (post-process HDR) coûte cher sur un GPU de téléphone.
    /// Ne modifie pas les assets : on travaille sur une copie du profil de chaque Volume.
    /// </summary>
    public static class MobileGraphics
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (!Application.isMobilePlatform) return;

            // -= puis += : pas de double abonnement si le "domain reload" est désactivé
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            DisableBloom();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => DisableBloom();

        private static void DisableBloom()
        {
            foreach (Volume volume in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include))
            {
                if (volume.sharedProfile == null || !volume.sharedProfile.Has<Bloom>()) continue;

                // volume.profile crée une copie propre à ce Volume
                if (volume.profile.TryGet(out Bloom bloom)) bloom.active = false;
            }
        }
    }
}
