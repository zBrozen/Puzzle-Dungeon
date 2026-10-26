using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace PuzzleDungeon.EditorTools
{
    /// <summary>
    /// Outils du portage Web/mobile, menu Vestige > Web.
    /// Passe par l'API de l'éditeur pour que Unity enregistre lui-même les réglages.
    /// </summary>
    public static class WebBuildTools
    {
        private const string WebTemplate = "PROJECT:Mobile";
        private const int WebMaxMemoryMB = 1024;
        private const string WebDefaultQuality = "Medium";
        private const string WebPlatform = "WebGL";
        private const int WebMaxTextureSize = 1024;

        [MenuItem("Vestige/Web/Appliquer les réglages Web")]
        private static void ApplyWebSettings()
        {
            var report = new StringBuilder();

            // Template, compression et mémoire
            PlayerSettings.WebGL.template = WebTemplate;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true; // marche même si le serveur n'envoie pas les bons en-têtes
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.maximumMemorySize = WebMaxMemoryMB;
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            report.AppendLine($"Template {WebTemplate}, Brotli + repli, mémoire max {WebMaxMemoryMB} Mo, IL2CPP optimisé pour la taille.");

            // Textures en ASTC : format natif des téléphones
            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC;
            report.AppendLine("Compression des textures : ASTC.");

            // Qualité par défaut du Web (le PC remonte en High au lancement, cf. WebQualitySelector)
            int mediumIndex = System.Array.IndexOf(QualitySettings.names, WebDefaultQuality);
            if (mediumIndex >= 0 && SetDefaultQualityForPlatform(WebPlatform, mediumIndex))
            {
                report.AppendLine($"Qualité par défaut du Web : {WebDefaultQuality}.");
                if (DisableAdditionalLightShadows(mediumIndex))
                    report.AppendLine($"Ombres des lumières additionnelles coupées en {WebDefaultQuality}.");
            }
            else
            {
                report.AppendLine($"⚠ Qualité \"{WebDefaultQuality}\" introuvable : qualité par défaut du Web inchangée.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[WebBuildTools] Réglages Web appliqués :\n" + report);
            EditorUtility.DisplayDialog("Réglages Web", report.ToString(), "OK");
        }

        [MenuItem("Vestige/Web/Optimiser les textures pour le Web")]
        private static void OptimizeTexturesForWeb()
        {
            List<TextureImporter> toChange = FindBuildTextures()
                .Where(importer => GetWebMaxSize(importer) > WebMaxTextureSize)
                .ToList();

            if (toChange.Count == 0)
            {
                EditorUtility.DisplayDialog("Textures Web", $"Toutes les textures du build sont déjà à {WebMaxTextureSize} px ou moins sur le Web.", "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog("Textures Web",
                $"{toChange.Count} textures utilisées par les scènes du build dépassent {WebMaxTextureSize} px.\n\n" +
                $"Ajouter un réglage Web à {WebMaxTextureSize} px ? Les autres plateformes (PC) ne changent pas.\n" +
                "La réimportation peut prendre quelques minutes.",
                "Optimiser", "Annuler");
            if (!confirmed) return;

            var log = new StringBuilder();
            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < toChange.Count; i++)
                {
                    TextureImporter importer = toChange[i];
                    EditorUtility.DisplayProgressBar("Textures Web", importer.assetPath, (float)i / toChange.Count);

                    TextureImporterPlatformSettings web = importer.GetPlatformTextureSettings(WebPlatform);
                    log.AppendLine($"{importer.assetPath} : {GetWebMaxSize(importer)} -> {WebMaxTextureSize}");

                    web.overridden = true;
                    web.maxTextureSize = WebMaxTextureSize;
                    web.format = TextureImporterFormat.Automatic; // suit la compression du build (ASTC)
                    web.textureCompression = importer.textureCompression;
                    importer.SetPlatformTextureSettings(web);
                    importer.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"[WebBuildTools] {toChange.Count} textures limitées à {WebMaxTextureSize} px sur le Web :\n{log}");
            EditorUtility.DisplayDialog("Textures Web", $"{toChange.Count} textures limitées à {WebMaxTextureSize} px sur le Web.\nLe détail est dans la Console.", "OK");
        }

        // --- Helpers ---

        /// <summary>Textures dont dépendent les scènes actives du build et les dossiers Resources.</summary>
        private static IEnumerable<TextureImporter> FindBuildTextures()
        {
            var roots = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();

            string[] resourceFolders = AssetDatabase.GetAllAssetPaths()
                .Where(p => p.StartsWith("Assets/") && AssetDatabase.IsValidFolder(p) && p.EndsWith("/Resources"))
                .ToArray();
            if (resourceFolders.Length > 0)
                roots.AddRange(AssetDatabase.FindAssets("", resourceFolders).Select(AssetDatabase.GUIDToAssetPath));

            return AssetDatabase.GetDependencies(roots.ToArray(), true)
                .Distinct()
                .Select(path => AssetImporter.GetAtPath(path) as TextureImporter)
                .Where(importer => importer != null);
        }

        private static int GetWebMaxSize(TextureImporter importer)
        {
            TextureImporterPlatformSettings web = importer.GetPlatformTextureSettings(WebPlatform);
            return web.overridden ? web.maxTextureSize : importer.maxTextureSize;
        }

        private static bool SetDefaultQualityForPlatform(string platform, int qualityIndex)
        {
            Object qualitySettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").FirstOrDefault();
            if (qualitySettings == null) return false;

            var serialized = new SerializedObject(qualitySettings);
            SerializedProperty perPlatform = serialized.FindProperty("m_PerPlatformDefaultQuality");
            if (perPlatform == null) return false;

            for (int i = 0; i < perPlatform.arraySize; i++)
            {
                SerializedProperty entry = perPlatform.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first").stringValue != platform) continue;

                entry.FindPropertyRelative("second").intValue = qualityIndex;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return true;
            }
            return false;
        }

        private static bool DisableAdditionalLightShadows(int qualityIndex)
        {
            Object pipelineAsset = QualitySettings.GetRenderPipelineAssetAt(qualityIndex);
            if (pipelineAsset == null) return false;

            var serialized = new SerializedObject(pipelineAsset);
            SerializedProperty shadows = serialized.FindProperty("m_AdditionalLightShadowsSupported");
            if (shadows == null) return false;

            shadows.boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipelineAsset);
            return true;
        }
    }
}
