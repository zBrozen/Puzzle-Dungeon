using UnityEngine;
using UnityEngine.Audio;

namespace PuzzleDungeon.Enemies
{
    [RequireComponent(typeof(AudioSource))]
    public class EnemyAudio : MonoBehaviour
    {
        [Header("SFX Clips")]
        [SerializeField] private AudioClip[] _attackSFXs;
        [SerializeField] private AudioClip _hurtSFX;
        [SerializeField] private AudioClip _deathSFX;

        [Header("Settings")]
        [SerializeField, Range(0, 1)] private float _volume = 0.7f;
        [SerializeField, Tooltip("Contrôle la vitesse de lecture (Pitch de l'AudioSource)")] 
        private float _playbackSpeed = 1f;
        [SerializeField] private float _pitchVariation = 0.1f;

        [Header("Mixer Control (Optional)")]
        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private string _pitchParameterName = "EnemyPitchShift";
        [SerializeField, Range(0.5f, 2.0f)] private float _basePitchShift = 1f;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.spatialBlend = 1f; // Assurer que le son est bien en 3D
            _audioSource.playOnAwake = false;
        }

        public void PlayAttackSFX()
        {
            if (_attackSFXs == null || _attackSFXs.Length == 0) return;
            PlaySFX(_attackSFXs[Random.Range(0, _attackSFXs.Length)]);
        }

        public void PlayHurtSFX()
        {
            PlaySFX(_hurtSFX);
        }

        public void PlayDeathSFX()
        {
            PlaySFX(_deathSFX);
        }

        private void PlaySFX(AudioClip clip)
        {
            if (clip == null || _audioSource == null) return;

            // Appliquer le volume global
            float globalMultiplier = 1f;
            if (Systems.AudioManager.Instance != null)
            {
                globalMultiplier = Systems.AudioManager.Instance.SFXVolume * Systems.AudioManager.Instance.MasterVolume;
            }

            // Le pitch de l'AudioSource contrôle maintenant la VITESSE
            _audioSource.pitch = _playbackSpeed + Random.Range(-_pitchVariation, _pitchVariation);

            // Si on a un mixer, on règle la HAUTEUR (Pitch) indépendamment
            if (_mixer != null && !string.IsNullOrEmpty(_pitchParameterName))
            {
                _mixer.SetFloat(_pitchParameterName, _basePitchShift);
            }

            _audioSource.PlayOneShot(clip, _volume * globalMultiplier);
        }
    }
}
