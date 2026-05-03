using UnityEngine;
using PuzzleDungeon.Systems.Save;
using PuzzleDungeon.Systems.Runs;
using System.Collections.Generic;

namespace PuzzleDungeon.Systems.DebugTools
{
    public class SaveDebugMenu : MonoBehaviour
    {
        [SerializeField] private bool _showMenu = false;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F12;
        
        private string _targetConfigID = "";
        private Vector2 _scrollPosition;

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                _showMenu = !_showMenu;
            }
        }

        private void OnGUI()
        {
            if (!_showMenu) return;

            // Calcul de la taille de l'UI (60% de l'écran)
            float width = Screen.width * 0.4f;
            float height = Screen.height * 0.8f;
            float x = (Screen.width - width) / 2;
            float y = (Screen.height - height) / 2;

            // Styles personnalisés
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUIStyle subHeaderStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16 };
            GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle) { fontSize = 16 };

            GUILayout.BeginArea(new Rect(x, y, width, height), GUI.skin.box);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Space(20);
            GUILayout.Label("SAVE & RUN DEBUG SYSTEM", headerStyle);
            GUILayout.Space(30);

            // --- SECTION SLOTS ---
            GUILayout.Label("💾 GESTION DES SLOTS", subHeaderStyle);
            GUILayout.Space(10);
            for (int i = 0; i < 3; i++)
            {
                bool exists = SaveSystem.HasSave(i);
                GUILayout.BeginHorizontal(GUI.skin.box);
                
                string status = exists ? "<color=green>OCCUPÉ</color>" : "<color=grey>VIDE</color>";
                GUILayout.Label($"Slot {i} : {status}", new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true });
                
                if (GUILayout.Button("CHARGER", buttonStyle, GUILayout.Width(120), GUILayout.Height(40)))
                {
                    PersistenceManager.Instance.LoadGame(i);
                }
                
                if (exists && GUILayout.Button("SUPPRIMER", buttonStyle, GUILayout.Width(120), GUILayout.Height(40)))
                {
                    SaveSystem.Delete(i);
                }
                
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }

            GUILayout.Space(30);

            // --- SECTION RUNS ---
            GUILayout.Label("🎲 CONFIGURATIONS DE RUN", subHeaderStyle);
            GUILayout.Space(10);

            if (RunManager.Instance != null)
            {
                var configs = RunManager.Instance.AvailableConfigs;
                
                foreach (var config in configs)
                {
                    if (string.IsNullOrEmpty(_targetConfigID)) _targetConfigID = config.ConfigurationID;

                    if (GUILayout.Toggle(_targetConfigID == config.ConfigurationID, $"ID: {config.ConfigurationID} ({config.name})", toggleStyle))
                    {
                        _targetConfigID = config.ConfigurationID;
                    }
                    GUILayout.Space(5);
                }
            }
            else
            {
                GUILayout.Label("<color=red>ERREUR: RunManager non trouvé !</color>", new GUIStyle(GUI.skin.label) { richText = true });
            }

            GUILayout.Space(20);
            if (GUILayout.Button("CRÉER NOUVELLE SAUVEGARDE (SLOT 0)", buttonStyle, GUILayout.Height(50)))
            {
                PersistenceManager.Instance.NewGame(0, _targetConfigID);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("FORCER SAUVEGARDE SLOT ACTUEL", buttonStyle, GUILayout.Height(50)))
            {
                PersistenceManager.Instance.SaveGame();
            }

            GUILayout.Space(30);

            // --- STATUT ---
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("📊 STATUT ACTUEL", subHeaderStyle);
            int currentSlot = PersistenceManager.Instance.CurrentSlot;
            GUILayout.Label("Slot actif : " + (currentSlot == -1 ? "Aucun" : currentSlot.ToString()), toggleStyle);
            
            if (PersistenceManager.Instance.CurrentData != null)
            {
                GUILayout.Label("Config active : " + PersistenceManager.Instance.CurrentData.runConfigurationID, toggleStyle);
            }
            GUILayout.EndVertical();

            GUILayout.Space(20);
            if (GUILayout.Button("📂 OUVRIR LE DOSSIER DES SAUVEGARDES", buttonStyle, GUILayout.Height(40)))
            {
                string path = Application.persistentDataPath + "/Saves/";
                if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
                System.Diagnostics.Process.Start("explorer.exe", path.Replace("/", "\\"));
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
