using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuzzleDungeon.Systems
{
    /// <summary>
    /// Sur le Web, remplace le Unity Toon Shader (com.unity.toonshader) par "Vestige/Toon Lite".
    ///
    /// Pourquoi : sur vestige.brozen.fr (Chrome, Windows), la variante WebGL du Toon Shader fait planter le
    /// processus graphique du navigateur (CONTEXT_LOST_WEBGL, écran noir) en Medium, Low et Very Low.
    /// Confirmé le 17/11 : avec le Toon Shader remplacé, ces niveaux fonctionnent.
    ///
    /// Les fichiers .mat ne changent pas (l'éditeur et les autres plateformes gardent le Toon Shader) :
    /// on change le shader des matériaux chargés en mémoire, à chaque chargement de scène, avant le premier
    /// rendu, en reprenant les réglages du Toon (seuil et couleur d'ombre, émission, contour, transparence).
    /// </summary>
    public static class WebToonFallback
    {
        private const string ToonShaderName = "Toon/Toon";
        private const string LiteShaderPath = "Shaders/VestigeToonLite"; // Assets/Resources/Shaders
        private const string OutlinePass = "SRPDefaultUnlit";

        // Propriétés du Unity Toon Shader
        private static readonly int UtsMainTex = Shader.PropertyToID("_MainTex");
        private static readonly int UtsShadeColor = Shader.PropertyToID("_1st_ShadeColor");
        private static readonly int UtsShadeStep = Shader.PropertyToID("_BaseColor_Step");
        private static readonly int UtsShadeFeather = Shader.PropertyToID("_BaseShade_Feather");
        private static readonly int UtsEmission = Shader.PropertyToID("_Emissive_Color");
        private static readonly int UtsOutlineWidth = Shader.PropertyToID("_Outline_Width");
        private static readonly int UtsOutlineColor = Shader.PropertyToID("_Outline_Color");
        private static readonly int UtsTransparent = Shader.PropertyToID("_TransparentEnabled");
        private static readonly int UtsCullMode = Shader.PropertyToID("_CullMode");
        private static readonly int UtsTweakTransparency = Shader.PropertyToID("_Tweak_transparency");

        // Propriétés de Vestige/Toon Lite
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ShadeColor = Shader.PropertyToID("_ShadeColor");
        private static readonly int ShadeStep = Shader.PropertyToID("_ShadeStep");
        private static readonly int ShadeFeather = Shader.PropertyToID("_ShadeFeather");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int OutlineWidth = Shader.PropertyToID("_OutlineWidth");
        private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
        private static readonly int Cull = Shader.PropertyToID("_Cull");
        private static readonly int ToonOpacity = Shader.PropertyToID("_ToonOpacity");

#if UNITY_WEBGL && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // -= puis += : pas de double abonnement si le "domain reload" est désactivé
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }
#endif

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => ReplaceToonMaterials(scene.name);

        private static void ReplaceToonMaterials(string sceneName)
        {
            Shader lite = Resources.Load<Shader>(LiteShaderPath);
            if (lite == null || !lite.isSupported)
            {
                Debug.LogError($"[WebToonFallback] Shader \"{LiteShaderPath}\" absent ou non supporté : Toon conservé.");
                return;
            }

            int count = 0;
            // Tous les matériaux chargés : ceux de la scène, des prefabs qu'elle référence et les instances.
            foreach (Material material in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (material == null || material.shader == null || material.shader.name != ToonShaderName) continue;
                Convert(material, lite);
                count++;
            }

            if (count > 0)
                Debug.Log($"[WebToonFallback] {sceneName} : {count} matériaux Toon passés en Vestige/Toon Lite.");
        }

        private static void Convert(Material material, Shader lite)
        {
            // Lecture des réglages Toon avant de changer de shader
            Texture texture = material.GetTexture(UtsMainTex);
            Vector2 scale = material.GetTextureScale(UtsMainTex);
            Vector2 offset = material.GetTextureOffset(UtsMainTex);
            Color baseColor = material.GetColor(BaseColor);
            Color shadeColor = Read(material, UtsShadeColor, new Color(0.5f, 0.5f, 0.5f, 1f));
            float shadeStep = Read(material, UtsShadeStep, 0.5f);
            float shadeFeather = Read(material, UtsShadeFeather, 0.0001f);
            Color emission = Read(material, UtsEmission, Color.black);
            float outlineWidth = Read(material, UtsOutlineWidth, 0f);
            Color outlineColor = Read(material, UtsOutlineColor, Color.black);
            // UTS (mode transparent, sans masque de découpe) : opacité = 1 + _Tweak_transparency.
            // Fiole : -0.72, donc 0.28 (verre). Personnage : 0, donc opaque.
            float opacity = Mathf.Clamp01(1f + Read(material, UtsTweakTransparency, 0f));
            bool transparent = Read(material, UtsTransparent, 0f) > 0.5f && opacity < 0.999f;
            float zWrite = Read(material, ZWrite, 1f);
            float cull = Read(material, UtsCullMode, 2f);

            material.shader = lite;
            material.shaderKeywords = System.Array.Empty<string>(); // mots-clés du Toon : sans objet ici

            material.SetTexture(BaseMap, texture);
            material.SetTextureScale(BaseMap, scale);
            material.SetTextureOffset(BaseMap, offset);
            material.SetColor(ShadeColor, shadeColor);
            material.SetFloat(ShadeStep, shadeStep);
            material.SetFloat(ShadeFeather, Mathf.Max(shadeFeather, 0.0001f));
            material.SetColor(EmissionColor, emission);
            material.SetFloat(OutlineWidth, outlineWidth);
            material.SetColor(OutlineColor, outlineColor);
            material.SetShaderPassEnabled(OutlinePass, outlineWidth > 0f); // pas de passe inutile sans contour
            material.SetFloat(Cull, cull);

            material.SetFloat(ToonOpacity, transparent ? opacity : 1f);

            if (transparent)
            {
                // Fiole (verre semi-transparent)
                material.SetFloat(SrcBlend, (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat(DstBlend, (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat(ZWrite, zWrite);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                // Opaque, personnage compris : comme dans l'éditeur, le fondu de PlayerCameraFade
                // (alpha de _BaseColor) ne le rend pas transparent ; il masque juste le modèle de très près.
                baseColor.a = 1f;
                material.SetFloat(SrcBlend, (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat(DstBlend, (float)UnityEngine.Rendering.BlendMode.Zero);
                material.SetFloat(ZWrite, 1f);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            }
            material.SetColor(BaseColor, baseColor);
        }

        private static float Read(Material material, int property, float fallback) =>
            material.HasProperty(property) ? material.GetFloat(property) : fallback;

        private static Color Read(Material material, int property, Color fallback) =>
            material.HasProperty(property) ? material.GetColor(property) : fallback;
    }
}
