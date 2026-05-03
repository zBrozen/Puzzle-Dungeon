using System.IO;
using UnityEngine;

namespace PuzzleDungeon.Systems.Save
{
    public static class SaveSystem
    {
        private static readonly string SaveFolder = Application.persistentDataPath + "/Saves/";
        private static readonly string SaveExtension = ".json";

        /// <summary>
        /// Sauvegarde les données dans un slot spécifique.
        /// </summary>
        public static void Save(int slotIndex, GameData data)
        {
            if (!Directory.Exists(SaveFolder))
            {
                Directory.CreateDirectory(SaveFolder);
            }

            data.slotIndex = slotIndex;
            data.lastUpdated = System.DateTime.Now.ToBinary();

            string json = JsonUtility.ToJson(data, true);
            string path = GetPath(slotIndex);

            File.WriteAllText(path, json);
            Debug.Log($"[SaveSystem] Saved slot {slotIndex} to {path}");
        }

        /// <summary>
        /// Charge les données d'un slot spécifique. Retourne null si le fichier n'existe pas.
        /// </summary>
        public static GameData Load(int slotIndex)
        {
            string path = GetPath(slotIndex);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SaveSystem] No save file found at {path}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                GameData data = JsonUtility.FromJson<GameData>(json);
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to load slot {slotIndex}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Supprime un slot de sauvegarde.
        /// </summary>
        public static void Delete(int slotIndex)
        {
            string path = GetPath(slotIndex);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SaveSystem] Deleted slot {slotIndex}");
            }
        }

        /// <summary>
        /// Vérifie si un slot possède une sauvegarde.
        /// </summary>
        public static bool HasSave(int slotIndex)
        {
            return File.Exists(GetPath(slotIndex));
        }

        private static string GetPath(int slotIndex)
        {
            return SaveFolder + "save_" + slotIndex + SaveExtension;
        }
    }
}
