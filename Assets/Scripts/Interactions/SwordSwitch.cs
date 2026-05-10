using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace PuzzleDungeon.Interactions
{
    [RequireComponent(typeof(Collider))]
    public class SwordSwitch : MonoBehaviour, IHittable
    {
        [Header("Animations")]
        [SerializeField] private Animator _animator;
        [SerializeField] private string _idleAnim = "SwordToggleIdle";
        [SerializeField] private string _onAnim = "SwordToggleON";

        [Header("State")]
        [SerializeField] private bool _isOn = false;
        public bool IsOn => _isOn;
        
        [Header("Settings")]
        [SerializeField, Tooltip("Si vrai, l'interrupteur ne peut être activé qu'une seule fois.")]
        private bool _oneShot = false;

        [SerializeField, Tooltip("Si vrai, frapper l'interrupteur alors qu'il est déjà actif le désactivera.")]
        private bool _canToggle = false;

        [SerializeField, Tooltip("Si vrai, déclenche les événements OnActivated/OnDeactivated au lancement du jeu selon l'état initial.")]
        private bool _syncEventsOnStart = true;

        [SerializeField, Tooltip("Temps d'attente minimum entre deux coups pour éviter l'activation multiple.")]
        private float _hitCooldown = 0.6f;

        [SerializeField, Tooltip("Si supérieur à 0, l'interrupteur se désactivera automatiquement après X secondes.")]
        private float _resetTimer = 0f;
        
        [Header("Timer Visuals (Optional)")]
        [SerializeField, Tooltip("Les Renderers (ex: MeshRenderer de la plateforme) à faire clignoter avant la fin du timer. Cela n'affecte pas les collisions.")]
        private Renderer[] _blinkRenderers;

        [SerializeField, Tooltip("Combien de secondes avant la fin le clignotement doit commencer.")]
        private float _startBlinkingAt = 3f;

        [SerializeField, Tooltip("Vitesse de clignotement maximale à la toute fin.")]
        private float _maxBlinkSpeed = 15f;
        
        [Header("Events")]
        public UnityEvent OnActivated;
        public UnityEvent OnDeactivated;

        private Coroutine _timerCoroutine;
        private float _lastHitTime = -1f;

        private void Start()
        {
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            
            // Assurez-vous que l'objet est sur le layer Interactable pour que l'épée le détecte
            if (gameObject.layer != LayerMask.NameToLayer("Interactable"))
            {
                Debug.LogWarning($"[SwordSwitch] {gameObject.name} n'est pas sur le layer 'Interactable'. Les coups d'épée pourraient ne pas le détecter.", this);
            }
            
            UpdateAnimation();

            if (_syncEventsOnStart)
            {
                if (_isOn) OnActivated?.Invoke();
                else OnDeactivated?.Invoke();
            }
        }

        public void OnHit()
        {
            // Éviter les activations multiples liées à la hitbox de l'épée sur une même attaque
            if (Time.time - _lastHitTime < _hitCooldown)
            {
                // Debug.Log($"[SwordSwitch] {gameObject.name} hit ignored (cooldown).");
                return;
            }
            _lastHitTime = Time.time;
            Debug.Log($"[SwordSwitch] {gameObject.name} HIT! State: {(_isOn ? "ON" : "OFF")}");

            if (_oneShot && _isOn) return;

            if (_isOn)
            {
                if (_canToggle)
                {
                    SetState(false);
                }
                else if (_resetTimer > 0f)
                {
                    // Si l'interrupteur est sur timer et déjà allumé, le frapper à nouveau reset le timer au lieu de l'éteindre
                    if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
                    
                    // On restaure les visuels si c'était en train de clignoter
                    if (_blinkRenderers != null)
                    {
                        foreach (var r in _blinkRenderers)
                        {
                            if (r != null) r.enabled = true;
                        }
                    }
                    
                    // Relance le timer
                    _timerCoroutine = StartCoroutine(ResetRoutine());
                    
                    // Force l'animation pour le retour visuel
                    UpdateAnimation();
                }
            }
            else
            {
                SetState(true);
            }
        }

        public void SetState(bool state)
        {
            if (_isOn == state) return;
            
            _isOn = state;
            UpdateAnimation();

            if (_isOn)
            {
                OnActivated?.Invoke();
                
                if (_resetTimer > 0f && !_oneShot)
                {
                    if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
                    _timerCoroutine = StartCoroutine(ResetRoutine());
                }
            }
            else
            {
                OnDeactivated?.Invoke();
                if (_timerCoroutine != null)
                {
                    StopCoroutine(_timerCoroutine);
                    _timerCoroutine = null;
                }

                // Restaurer la visibilité si on a annulé le timer en plein clignotement
                if (_blinkRenderers != null)
                {
                    foreach (var r in _blinkRenderers)
                    {
                        if (r != null) r.enabled = true;
                    }
                }
            }
        }

        private void UpdateAnimation()
        {
            if (_animator != null)
            {
                _animator.Play(_isOn ? _onAnim : _idleAnim);
            }
        }

        private IEnumerator ResetRoutine()
        {
            float elapsed = 0f;
            bool isBlinking = false;

            while (elapsed < _resetTimer)
            {
                elapsed += Time.deltaTime;
                float remaining = _resetTimer - elapsed;

                if (_blinkRenderers != null && _blinkRenderers.Length > 0 && remaining <= _startBlinkingAt)
                {
                    isBlinking = true;
                    // Vitesse croissante de 2 jusqu'à _maxBlinkSpeed
                    float t = 1f - (remaining / _startBlinkingAt);
                    float currentFreq = Mathf.Lerp(2f, _maxBlinkSpeed, t);
                    
                    // Mathf.Sin oscille ; > 0f donne un rendu visible/invisible bien tranché
                    bool isVisible = Mathf.Sin(elapsed * currentFreq * Mathf.PI) > 0f;
                    
                    foreach (var r in _blinkRenderers)
                    {
                        if (r != null) r.enabled = isVisible;
                    }
                }

                yield return null;
            }

            // Remettre tout visible à la fin pour le prochain cycle
            if (isBlinking)
            {
                foreach (var r in _blinkRenderers)
                {
                    if (r != null) r.enabled = true;
                }
            }

            if (_isOn)
            {
                SetState(false);
            }
        }
    }
}
