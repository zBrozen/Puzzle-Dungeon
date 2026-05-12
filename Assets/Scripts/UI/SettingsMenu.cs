using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PuzzleDungeon.Systems;
using System.Collections.Generic;

namespace PuzzleDungeon.UI
{
    public class SettingsMenu : MonoBehaviour
    {
        [Header("Graphics")]
        [SerializeField] private TMP_Dropdown _qualityDropdown;

        [Header("Audio")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private AudioClip _sfxPreviewClip;
        [SerializeField] private float _previewCooldown = 0.2f;

        private float _lastPreviewTime;

        private void OnEnable()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            // Quality
            if (_qualityDropdown != null)
            {
                _qualityDropdown.ClearOptions();
                List<string> options = new List<string>(QualitySettings.names);
                _qualityDropdown.AddOptions(options);
                
                // If first time, we might want to default to Ultra (usually the last index)
                int currentQuality = QualitySettings.GetQualityLevel();
                _qualityDropdown.value = currentQuality;
                
                _qualityDropdown.onValueChanged.RemoveAllListeners();
                _qualityDropdown.onValueChanged.AddListener(SetQuality);
            }

            // Audio
            if (AudioManager.Instance != null)
            {
                if (_masterSlider != null)
                {
                    _masterSlider.value = AudioManager.Instance.MasterVolume;
                    _masterSlider.onValueChanged.RemoveAllListeners();
                    _masterSlider.onValueChanged.AddListener(SetMasterVolume);
                }
                if (_musicSlider != null)
                {
                    _musicSlider.value = AudioManager.Instance.MusicVolume;
                    _musicSlider.onValueChanged.RemoveAllListeners();
                    _musicSlider.onValueChanged.AddListener(SetMusicVolume);
                }
                if (_sfxSlider != null)
                {
                    _sfxSlider.value = AudioManager.Instance.SFXVolume;
                    _sfxSlider.onValueChanged.RemoveAllListeners();
                    _sfxSlider.onValueChanged.AddListener(SetSFXVolume);
                }
            }
        }

        public void SetQuality(int index)
        {
            QualitySettings.SetQualityLevel(index, true);
            PlayerPrefs.SetInt("QualityLevel", index);
            PlayerPrefs.Save();
            Debug.Log($"[Settings] Quality set to: {QualitySettings.names[index]}");
        }

        public void SetMasterVolume(float value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.MasterVolume = value;
        }

        public void SetMusicVolume(float value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.MusicVolume = value;
        }

        public void SetSFXVolume(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SFXVolume = value;
                
                // Play preview sound with cooldown to avoid spamming
                if (_sfxPreviewClip != null)
                {
                    if (Time.unscaledTime - _lastPreviewTime > _previewCooldown)
                    {
                        AudioManager.Instance.PlaySFX(_sfxPreviewClip);
                        _lastPreviewTime = Time.unscaledTime;
                    }
                }
                else
                {
                    Debug.LogWarning("[SettingsMenu] No Sfx Preview Clip assigned in the inspector!");
                }
            }
            else
            {
                Debug.LogWarning("[SettingsMenu] AudioManager.Instance is null! Make sure an AudioManager exists in the scene.");
            }
        }
    }
}
