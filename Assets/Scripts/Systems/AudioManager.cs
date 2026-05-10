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

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePool();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (_playMusicOnStart && _defaultBackgroundMusic != null)
            {
                PlayMusic(_defaultBackgroundMusic);
            }
        }

        private void InitializePool()
        {
            if (_sfxSourcePrefab == null)
            {
                // Create a default AudioSource if none provided
                GameObject go = new GameObject("DefaultSFXSource");
                go.transform.SetParent(transform);
                _sfxSourcePrefab = go.AddComponent<AudioSource>();
                go.SetActive(false);
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
            _musicSource.Play();
        }

        public void StopMusic()
        {
            if (_musicSource != null) _musicSource.Stop();
        }

        public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetNextAvailableSFXSource();
            if (source != null)
            {
                source.gameObject.SetActive(true);
                source.clip = clip;
                source.volume = volume;
                source.pitch = pitch;
                source.Play();
                StartCoroutine(DisableSourceAfterPlay(source));
            }
        }

        private AudioSource GetNextAvailableSFXSource()
        {
            foreach (var source in _sfxPool)
            {
                if (!source.gameObject.activeInHierarchy) return source;
            }

            // Optional: Expand pool if needed
            AudioSource newSource = Instantiate(_sfxSourcePrefab, transform);
            _sfxPool.Add(newSource);
            return newSource;
        }

        private System.Collections.IEnumerator DisableSourceAfterPlay(AudioSource source)
        {
            yield return new WaitUntil(() => !source.isPlaying);
            source.gameObject.SetActive(false);
        }
    }
}
