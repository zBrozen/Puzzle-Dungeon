using UnityEngine;
using TMPro;
using PuzzleDungeon.Systems.Save;
using System;

namespace PuzzleDungeon.UI
{
    public class SaveSlotUI : MonoBehaviour
    {
        [Header("Slot Configuration")]
        [SerializeField] private int slotIndex;
        [SerializeField] private MainMenuManager menuManager;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI statusText; // Main title (Slot X or Create New)
        [SerializeField] private TextMeshProUGUI detailText; // Subtitle (Date or description)
        [SerializeField] private GameObject hasDataVisuals;
        [SerializeField] private GameObject emptyDataVisuals;
        [SerializeField] private GameObject deleteButton;

        public void Refresh()
        {
            Debug.Log($"[SaveSlotUI] Refreshing slot {slotIndex}");
            bool hasSave = SaveSystem.HasSave(slotIndex);

            if (hasSave)
            {
                GameData data = SaveSystem.Load(slotIndex);
                if (data != null)
                {
                    DateTime lastSaved = DateTime.FromBinary(data.lastUpdated);
                    
                    if (statusText != null) 
                        statusText.text = $"Slot {slotIndex + 1}";
                    
                    if (detailText != null) 
                        detailText.text = $"Sauvegardé le {lastSaved:dd/MM/yyyy HH:mm}";
                    
                    if (hasDataVisuals != null) hasDataVisuals.SetActive(true);
                    if (emptyDataVisuals != null) emptyDataVisuals.SetActive(false);
                    if (deleteButton != null) deleteButton.SetActive(true);
                }
                else
                {
                    Debug.LogError($"[SaveSlotUI] Slot {slotIndex}: Save found but data is null!");
                }
            }
            else
            {
                if (statusText != null) statusText.text = "Nouvelle partie";
                if (detailText != null) detailText.text = "Nouvelle aventure";
                
                if (hasDataVisuals != null) hasDataVisuals.SetActive(false);
                if (emptyDataVisuals != null) emptyDataVisuals.SetActive(true);
                if (deleteButton != null) deleteButton.SetActive(false);
            }
        }

        public void OnSlotClicked()
        {
            if (menuManager != null)
            {
                menuManager.StartGameWithSlot(slotIndex);
            }
            else
            {
                Debug.LogError($"[SaveSlotUI] Slot {slotIndex}: menuManager reference is missing in the Inspector!");
            }
        }

        public void OnDeleteClicked()
        {
            Debug.Log($"[SaveSlotUI] Deleting slot {slotIndex}");
            SaveSystem.Delete(slotIndex);
            Refresh();
        }
    }
}
