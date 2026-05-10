using UnityEngine;

namespace PuzzleDungeon.Player
{
    public class PlayerAudio : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _footstepSource;

        [Header("Footsteps")]
        [SerializeField] private AudioClip[] _footstepClips;
        [SerializeField] private float _footstepVolume = 0.5f;
        [SerializeField] private float _footstepPitchRange = 0.1f;

        [Header("Player SFX (Actions)")]
        [SerializeField] private AudioClip[] _attackSFXs;
        [SerializeField] private AudioClip _jumpSFX;
        [SerializeField] private AudioClip _climbSFX;
        [SerializeField] private AudioClip _hurtSFX;
        [SerializeField, Range(0, 1)] private float _sfxVolume = 0.8f;

        private void Awake()
        {
            // Ensure we have sources if not assigned
            if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();
            if (_footstepSource == null) _footstepSource = gameObject.AddComponent<AudioSource>();
            
            _sfxSource.spatialBlend = 1f; // 3D sound
            _footstepSource.spatialBlend = 1f;
        }

        public void PlayFootstep()
        {
            if (_footstepClips == null || _footstepClips.Length == 0) return;

            AudioClip clip = _footstepClips[Random.Range(0, _footstepClips.Length)];
            _footstepSource.pitch = 1f + Random.Range(-_footstepPitchRange, _footstepPitchRange);
            _footstepSource.clip = clip;
            _footstepSource.volume = _footstepVolume;
            _footstepSource.Play();
        }

        public void StopFootstep()
        {
            if (_footstepSource != null) _footstepSource.Stop();
        }

        public void PlayJumpSFX() => PlaySFX(_jumpSFX);
        
        public void PlayAttackSFX()
        {
            if (_attackSFXs == null || _attackSFXs.Length == 0) return;
            PlaySFX(_attackSFXs[Random.Range(0, _attackSFXs.Length)]);
        }

        public void PlayClimbSFX() => PlaySFX(_climbSFX);
        public void PlayHurtSFX() => PlaySFX(_hurtSFX);
        
        public void StopPlayerSFX()
        {
            if (_sfxSource != null) _sfxSource.Stop();
        }

        private void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            
            _sfxSource.pitch = 1f + Random.Range(-0.1f, 0.1f); // Un peu plus de variation sur le pitch
            _sfxSource.clip = clip;
            _sfxSource.volume = _sfxVolume;
            _sfxSource.Play();
        }
    }
}
