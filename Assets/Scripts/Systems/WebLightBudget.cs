using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuzzleDungeon.Systems
{
    /// <summary>
    /// Sur le Web, garde allumées seulement les lumières visibles les plus proches de la caméra.
    ///
    /// Pourquoi : en WebGL, URP n'accepte que 32 lumières visibles à la fois (256 sur PC dans l'éditeur) et
    /// ignore les autres. La scène de jeu en compte environ 70 (25 torches, spots, lumières ponctuelles) :
    /// dans les grandes salles, des torches proches pouvaient s'éteindre au profit de lumières lointaines.
    /// On choisit nous-mêmes lesquelles garder (les plus proches), avec une marge pour les lumières des
    /// effets (explosions, coffres…), qui ne sont pas gérées ici.
    ///
    /// Ne rallume que les lumières qu'il a lui-même éteintes : un script qui éteint une lumière garde la main.
    /// Les lumières directionnelles ne sont jamais touchées.
    /// </summary>
    public class WebLightBudget : MonoBehaviour
    {
        private const int Budget = 24;              // limite URP WebGL : 32, moins une marge pour les effets
        private const float RefreshInterval = 0.25f;

        private readonly List<Light> _lights = new List<Light>();
        private readonly HashSet<Light> _turnedOffByUs = new HashSet<Light>();
        private readonly List<(Light light, float distance)> _visible = new List<(Light, float)>();
        private readonly Plane[] _frustum = new Plane[6];
        private float _nextRefresh;

#if UNITY_WEBGL && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            var host = new GameObject(nameof(WebLightBudget));
            DontDestroyOnLoad(host);
            host.AddComponent<WebLightBudget>();
        }
#endif

        private void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => CollectLights();

        private void CollectLights()
        {
            _lights.Clear();
            _turnedOffByUs.RemoveWhere(light => light == null);
            foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (light.type != LightType.Directional) _lights.Add(light);
            }
            _nextRefresh = 0f; // appliquer tout de suite
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + RefreshInterval;

            Camera camera = Camera.main;
            if (camera == null || _lights.Count == 0) return;

            GeometryUtility.CalculateFrustumPlanes(camera, _frustum);
            Vector3 cameraPosition = camera.transform.position;

            // Lumières qui comptent dans la limite d'URP : actives et dont la portée touche le champ de la caméra
            _visible.Clear();
            for (int i = _lights.Count - 1; i >= 0; i--)
            {
                Light light = _lights[i];
                if (light == null) { _lights.RemoveAt(i); continue; }

                bool candidate = light.enabled || _turnedOffByUs.Contains(light);
                if (!candidate || !light.gameObject.activeInHierarchy) continue;

                Vector3 center = light.transform.position;
                var bounds = new Bounds(center, Vector3.one * (light.range * 2f));
                if (!GeometryUtility.TestPlanesAABB(_frustum, bounds))
                {
                    // Hors champ : ne compte pas dans la limite, on la rend à son état normal
                    Restore(light);
                    continue;
                }

                // Distance au bord de la zone éclairée : une grande lumière proche passe avant une petite
                float distance = Mathf.Max(0f, Vector3.Distance(cameraPosition, center) - light.range);
                _visible.Add((light, distance));
            }

            _visible.Sort((a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < _visible.Count; i++)
            {
                Light light = _visible[i].light;
                if (i < Budget) Restore(light);
                else if (light.enabled)
                {
                    light.enabled = false;
                    _turnedOffByUs.Add(light);
                }
            }
        }

        private void Restore(Light light)
        {
            if (_turnedOffByUs.Remove(light)) light.enabled = true;
        }
    }
}
