using UnityEngine;
using System;

namespace PuzzleDungeon.Enemies
{
    public class EnemyHealth : MonoBehaviour
    {
        public event Action OnHurt;
        public event Action OnDeath;

        [Header("Health Settings")]
        [SerializeField] private int _maxHealth = 3;
        
        [Header("Visual Feedback")]
        [SerializeField] private bool _enableFlash = true;
        [SerializeField] private Renderer[] _meshRenderers;
        [SerializeField] private float _flashDuration = 0.5f;
        [SerializeField] private Color _flashColor = Color.red;

        private int _currentHealth;
        private bool _isDead = false;
        private Color[] _originalColors;
        private EnemyAudio _enemyAudio;

        private void Awake()
        {
            _currentHealth = _maxHealth;
            _enemyAudio = GetComponent<EnemyAudio>();

            // Essayer de trouver les renderers si non assignés
            if (_meshRenderers == null || _meshRenderers.Length == 0)
            {
                _meshRenderers = GetComponentsInChildren<Renderer>();
            }

            // Sauvegarder les couleurs originales pour le clignotement
            _originalColors = new Color[_meshRenderers.Length];
            for (int i = 0; i < _meshRenderers.Length; i++)
            {
                if (_meshRenderers[i].material.HasProperty("_Color"))
                {
                    _originalColors[i] = _meshRenderers[i].material.color;
                }
            }
        }

        public void TakeDamage(int amount)
        {
            if (_isDead) return;

            _currentHealth -= amount;
            
            if (_currentHealth <= 0)
            {
                Die();
            }
            else
            {
                if (_enemyAudio != null) _enemyAudio.PlayHurtSFX();
                OnHurt?.Invoke();
                if (_enableFlash) StartCoroutine(FlashRoutine(_flashColor, _flashDuration));
            }
        }

        private void Die()
        {
            _isDead = true;
            _currentHealth = 0;
            if (_enemyAudio != null) _enemyAudio.PlayDeathSFX();
            OnDeath?.Invoke();
        }

        public void Flash(Color color, float duration)
        {
            if (!_enableFlash) return;
            StartCoroutine(FlashRoutine(color, duration));
        }

        public void Blink(Color color, int count, float duration)
        {
            if (!_enableFlash) return;
            StartCoroutine(BlinkRoutine(color, count, duration));
        }

        private System.Collections.IEnumerator BlinkRoutine(Color targetColor, int count, float duration)
        {
            float interval = duration / (count * 2f);
            for (int i = 0; i < count; i++)
            {
                ApplyColor(targetColor);
                yield return new WaitForSeconds(interval);
                RestoreColors();
                yield return new WaitForSeconds(interval);
            }
        }

        private void ApplyColor(Color targetColor)
        {
            for (int i = 0; i < _meshRenderers.Length; i++)
            {
                if (_meshRenderers[i] != null)
                {
                    Material mat = _meshRenderers[i].material;
                    
                    // On force les couleurs en mode "brillant"
                    Color hdrColor = targetColor * 20f; // Très haute intensité pour forcer le blanc

                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", hdrColor);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hdrColor);
                    
                    if (mat.HasProperty("_EmissionColor")) 
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", hdrColor); 
                    }
                }
            }
        }

        private void RestoreColors()
        {
            for (int i = 0; i < _meshRenderers.Length; i++)
            {
                if (_meshRenderers[i] != null)
                {
                    Material mat = _meshRenderers[i].material;
                    if (mat.HasProperty("_Color")) mat.color = _originalColors[i];
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", _originalColors[i]);
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
                }
            }
        }

        private System.Collections.IEnumerator FlashRoutine(Color targetColor, float duration)
        {
            ApplyColor(targetColor);
            yield return new WaitForSeconds(duration);
            RestoreColors();
        }
    }
}
