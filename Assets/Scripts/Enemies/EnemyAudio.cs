using UnityEngine;

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
        [SerializeField] private float _pitchVariation = 0.1f;

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

            _audioSource.pitch = 1f + Random.Range(-_pitchVariation, _pitchVariation);
            _audioSource.PlayOneShot(clip, _volume * globalMultiplier);
        }
    }
}
