using UnityEngine;
using System.Collections.Generic;

namespace PuzzleDungeon.Systems
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music Settings")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioClip _defaultBackgroundMusic;
        [SerializeField] private bool _playMusicOnStart = true;

        [Header("SFX Settings")]
        [SerializeField] private AudioSource _sfxSourcePrefab;
        [SerializeField] private int _sfxPoolSize = 10;

        private List<AudioSource> _sfxPool = new List<AudioSource>();

        [Header("Volume Settings")]
        private float _masterVolume = 1f;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;

        private const string MasterVolKey = "MasterVolume";
        private const string MusicVolKey = "MusicVolume";
        private const string SFXVolKey = "SFXVolume";

        public float MasterVolume 
        { 
            get => _masterVolume; 
            set { _masterVolume = Mathf.Clamp01(value); SaveVolumes(); ApplyVolumes(); } 
        }
        public float MusicVolume 
        { 
            get => _musicVolume; 
            set { _musicVolume = Mathf.Clamp01(value); SaveVolumes(); ApplyVolumes(); } 
        }
        public float SFXVolume 
        { 
            get => _sfxVolume; 
            set { _sfxVolume = Mathf.Clamp01(value); SaveVolumes(); ApplyVolumes(); } 
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadVolumes();
                
                // Set default quality to Ultra (highest index) if not set
                if (!PlayerPrefs.HasKey("QualityLevel"))
                {
                    int ultraIndex = QualitySettings.names.Length - 1;
                    QualitySettings.SetQualityLevel(ultraIndex, true);
                    PlayerPrefs.SetInt("QualityLevel", ultraIndex);
                }
                else
                {
                    QualitySettings.SetQualityLevel(PlayerPrefs.GetInt("QualityLevel"), true);
                }

                InitializePool();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            ApplyVolumes();
            if (_playMusicOnStart && _defaultBackgroundMusic != null)
            {
                PlayMusic(_defaultBackgroundMusic);
            }
        }

        private void LoadVolumes()
        {
            _masterVolume = PlayerPrefs.GetFloat(MasterVolKey, 1f);
            _musicVolume = PlayerPrefs.GetFloat(MusicVolKey, 1f);
            _sfxVolume = PlayerPrefs.GetFloat(SFXVolKey, 1f);
        }

        private void SaveVolumes()
        {
            PlayerPrefs.SetFloat(MasterVolKey, _masterVolume);
            PlayerPrefs.SetFloat(MusicVolKey, _musicVolume);
            PlayerPrefs.SetFloat(SFXVolKey, _sfxVolume);
            PlayerPrefs.Save();
        }

        private void ApplyVolumes()
        {
            if (_musicSource != null)
            {
                _musicSource.volume = _musicVolume * _masterVolume;
            }
            // For pooled SFX, volume is applied at play time
        }

        private void InitializePool()
        {
            if (_sfxSourcePrefab == null)
            {
                InitializeDefaultSFXSource();
            }

            for (int i = 0; i < _sfxPoolSize; i++)
            {
                AudioSource source = Instantiate(_sfxSourcePrefab, transform);
                source.gameObject.SetActive(false);
                _sfxPool.Add(source);
            }
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (_musicSource == null)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
            }

            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.volume = _musicVolume * _masterVolume;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            if (_musicSource != null) _musicSource.Stop();
        }

        /// <summary>
        /// Joue un son de manière persistante sur l'AudioManager lui-même (hors pool).
        /// Idéal pour les sons critiques comme la mort ou les transitions de scène.
        /// </summary>
        public void PlayGlobalSFX(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            
            // On utilise une source dédiée sur l'AudioManager qui n'est jamais désactivée
            AudioSource source = GetComponent<AudioSource>();
            if (source == null) 
            {
                source = gameObject.AddComponent<AudioSource>();
                source.spatialBlend = 0f; // Force 2D
            }
            
            source.PlayOneShot(clip, volume * _sfxVolume * _masterVolume);
            Debug.Log($"[AudioManager] Playing Global SFX: {clip.name} at volume {source.volume}");
        }

        public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetNextAvailableSFXSource();
            if (source != null)
            {
                source.gameObject.SetActive(true);
                source.enabled = true;
                source.mute = false;
                
                // On ne force plus le 2D ici, car certains SFX peuvent être 3D.
                // Par défaut le prefab/template devrait être en 2D pour les sons UI.
                
                source.clip = clip;
                source.volume = volume * _sfxVolume * _masterVolume;
                source.pitch = pitch;
                source.Play();
                Debug.Log($"[AudioManager] Playing SFX: {clip.name} at volume {source.volume}");
                StartCoroutine(DisableSourceAfterPlay(source));
            }
        }

        private AudioSource GetNextAvailableSFXSource()
        {
            // First, cleanup null entries and look for an available source
            for (int i = _sfxPool.Count - 1; i >= 0; i--)
            {
                AudioSource source = _sfxPool[i];
                if (source == null)
                {
                    _sfxPool.RemoveAt(i);
                    continue;
                }
                
                if (!source.gameObject.activeInHierarchy) return source;
            }

            // If we get here, we need to create a new source
            // Ensure we have a valid prefab/template
            if (_sfxSourcePrefab == null)
            {
                Debug.LogWarning("[AudioManager] SFX Source template was lost or null. Re-initializing.");
                InitializeDefaultSFXSource();
            }

            if (_sfxSourcePrefab != null)
            {
                AudioSource newSource = Instantiate(_sfxSourcePrefab, transform);
                newSource.gameObject.SetActive(false);
                _sfxPool.Add(newSource);
                return newSource;
            }

            return null;
        }

        private void InitializeDefaultSFXSource()
        {
            GameObject go = new GameObject("DefaultSFXSource");
            go.transform.SetParent(transform);
            _sfxSourcePrefab = go.AddComponent<AudioSource>();
            _sfxSourcePrefab.enabled = true; // Ensure component is enabled
            _sfxSourcePrefab.spatialBlend = 0f; // Force 2D for UI/Global SFX
            go.SetActive(false);
        }

        private System.Collections.IEnumerator DisableSourceAfterPlay(AudioSource source)
        {
            // Attendre au moins 1 frame réelle (ignoreTimeScale) avant de vérifier isPlaying
            // Sinon, isPlaying est encore false juste après Play() et la source est désactivée immédiatement
            yield return new WaitForSecondsRealtime(0.05f);

            // Attendre la fin du clip (fonctionne même avec timeScale = 0 en pause)
            while (source != null && source.isPlaying)
            {
                yield return new WaitForSecondsRealtime(0.05f);
            }
            
            if (source != null)
            {
                source.gameObject.SetActive(false);
            }
        }
    }
}
