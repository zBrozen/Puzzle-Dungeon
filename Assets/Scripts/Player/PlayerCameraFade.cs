using UnityEngine;
using System.Collections.Generic;

namespace PuzzleDungeon.Player
{
    public class PlayerCameraFade : MonoBehaviour
    {
        [Header("Fade Settings")]
        [SerializeField] private float _fadeStartDistance = 2.0f;
        [SerializeField] private float _fadeEndDistance = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _minAlpha = 0f;

        [Header("References")]
        [SerializeField] private Renderer[] _renderers;

        private Transform _cameraTransform;
        private List<Material> _materials = new List<Material>();
        private static readonly int _ColorProp = Shader.PropertyToID("_Color");
        private static readonly int _BaseColorProp = Shader.PropertyToID("_BaseColor");

        private void Start()
        {
            if (Camera.main != null) 
            {
                _cameraTransform = Camera.main.transform;
            }
            else 
            {
                // Secours si le tag MainCamera est manquant
                Camera cam = FindFirstObjectByType<Camera>();
                if (cam != null) _cameraTransform = cam.transform;
            }

            if (_cameraTransform == null)
            {
                Debug.LogWarning("[PlayerCameraFade] Aucune caméra trouvée ! Assurez-vous que votre caméra a le tag 'MainCamera'.");
            }

            if (_renderers == null || _renderers.Length == 0)
            {
                _renderers = GetComponentsInChildren<Renderer>();
            }

            foreach (var r in _renderers)
            {
                // Utiliser .materials crée des instances uniques (évite de modifier le fichier de base)
                foreach (var mat in r.materials)
                {
                    _materials.Add(mat);
                }
            }
            
            Debug.Log($"[PlayerCameraFade] Initialisé avec {_materials.Count} matériaux sur {_renderers.Length} renderers.");
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null || _materials.Count == 0) return;

            // Calcul de distance horizontale (cylindre)
            Vector3 playerPos = transform.position;
            Vector3 camPos = _cameraTransform.position;
            playerPos.y = 0;
            camPos.y = 0;
            float distance = Vector3.Distance(camPos, playerPos);
            
            // 1. Gestion de l'Alpha (si supporté par le shader)
            float alpha = 1f;
            if (distance < _fadeStartDistance)
            {
                alpha = Mathf.InverseLerp(_fadeEndDistance, _fadeStartDistance, distance);
                alpha = Mathf.Clamp(alpha, _minAlpha, 1f);
            }
            ApplyAlpha(alpha);

            // 2. Sécurité absolue : on désactive carrément les renderers si on est "dans" le joueur
            bool shouldShow = distance > _fadeEndDistance;
            
            // On ajoute une marge verticale pour ne pas masquer si la caméra est très haut au dessus
            float verticalDiff = Mathf.Abs(_cameraTransform.position.y - transform.position.y);
            if (verticalDiff > 2.5f) shouldShow = true;

            foreach (var r in _renderers)
            {
                if (r != null && r.enabled != shouldShow) r.enabled = shouldShow;
            }
        }

        private void ApplyAlpha(float alpha)
        {
            foreach (var mat in _materials)
            {
                if (mat == null) continue;

                // 1. Test des couleurs classiques (Standard, URP, HDRP)
                if (mat.HasProperty(_ColorProp)) mat.SetColor(_ColorProp, SetAlpha(mat.GetColor(_ColorProp), alpha));
                else if (mat.HasProperty(_BaseColorProp)) mat.SetColor(_BaseColorProp, SetAlpha(mat.GetColor(_BaseColorProp), alpha));
                else if (mat.HasProperty("_MainColor")) mat.SetColor("_MainColor", SetAlpha(mat.GetColor("_MainColor"), alpha));

                // 2. Test des propriétés spécifiques aux Toon Shaders (souvent des Floats séparés)
                if (mat.HasProperty("_Alpha")) mat.SetFloat("_Alpha", alpha);
                if (mat.HasProperty("_Opacity")) mat.SetFloat("_Opacity", alpha);
                if (mat.HasProperty("_Transparency")) mat.SetFloat("_Transparency", 1f - alpha);
                
                // 3. Cas spécifique du Unity Toon Shader (UTS)
                if (mat.HasProperty("_Tweak_Transparency")) mat.SetFloat("_Tweak_Transparency", 1f - alpha);
                if (mat.HasProperty("_Transparency_Level")) mat.SetFloat("_Transparency_Level", 1f - alpha);
                if (mat.HasProperty("_TransparencyLevel")) mat.SetFloat("_TransparencyLevel", 1f - alpha);
            }
        }

        private Color SetAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        private void OnDestroy()
        {
            // Nettoyage des instances de matériaux pour éviter les fuites mémoire
            foreach (var mat in _materials)
            {
                if (mat != null) Destroy(mat);
            }
        }
    }
}
