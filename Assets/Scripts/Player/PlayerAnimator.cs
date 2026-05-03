using UnityEngine;

namespace PuzzleDungeon.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private float _transitionDuration = 0.15f;

        [Header("Jump Animations")]
        [SerializeField, Range(0.1f, 1.0f), Tooltip("Fraction de l'animation de saut qui doit être jouée avant d'autoriser la chute")] 
        private float _jumpAnimMinCompletion = 0.85f;
        [SerializeField, Tooltip("Activer pour utiliser plusieurs animations de saut aléatoirement")]
        private bool _useRandomJumps = false;
        [SerializeField, Tooltip("Liste des noms des animations de saut à utiliser")]
        private string[] _randomJumpAnimations = { "Jump" };
        [SerializeField, Tooltip("Animation de saut spécifique après une roulade (laisser vide pour utiliser l'anim normale)")]
        private string _rollJumpAnimationName = "";

        [Header("Animator Layers")]
        [SerializeField, Tooltip("Index du layer UpperBody dans l'Animator (généralement 1)")]
        private int _upperBodyLayerIndex = 1;
        [SerializeField, Tooltip("Nom de l'état vide dans le layer UpperBody")]
        private string _upperBodyEmptyState = "Empty";

        private Animator _animator;
        private PlayerController.PlayerState _lastState;
        private int _currentJumpHash;
        private PlayerHealth _playerHealth;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_playerController == null)
                _playerController = GetComponentInParent<PlayerController>();

            _playerHealth = GetComponentInParent<PlayerHealth>();

            _currentJumpHash = Animator.StringToHash("Jump");
        }

        private void OnEnable()
        {
            if (_playerController != null)
            {
                _playerController.OnDrawWeapon += HandleDrawWeapon;
                _playerController.OnSheatheWeapon += HandleSheatheWeapon;
                _playerController.OnAttackAction += HandleAttackAction;
            }
            if (_playerHealth != null)
            {
                _playerHealth.OnTakeDamage += HandleTakeDamage;
            }
        }

        private void OnDisable()
        {
            if (_playerController != null)
            {
                _playerController.OnDrawWeapon -= HandleDrawWeapon;
                _playerController.OnSheatheWeapon -= HandleSheatheWeapon;
                _playerController.OnAttackAction -= HandleAttackAction;
            }
            if (_playerHealth != null)
            {
                _playerHealth.OnTakeDamage -= HandleTakeDamage;
            }
        }

        private void HandleDrawWeapon()
        {
            _animator.SetTrigger("DrawWeapon");
        }

        private void HandleSheatheWeapon()
        {
            _animator.SetTrigger("SheatheWeapon");
        }

        private void HandleAttackAction(int comboStep)
        {
            // Les attaques utilisent souvent le corps entier (ou on vide l'upperbody si l'attaque est sur la base)
            ClearUpperBodyLayer();
            
            // On utilise CrossFadeInFixedTime avec un temps très court (0.05s) pour une réactivité maximale
            _animator.CrossFadeInFixedTime($"Attack_{comboStep}", 0.05f);
        }

        private void HandleTakeDamage(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Void:
                    // Play() force l'animation immédiatement, sans attendre la fin de l'anim en cours
                    _animator.Play("GetUp_SwordAndShield", 0, 0f);
                    break;
                default:
                    _animator.SetTrigger("GetHit");
                    break;
            }
        }

        public void TriggerTreasureOpening()
        {
            _playerController.SetState(PlayerController.PlayerState.Treasure);
            ClearUpperBodyLayer();
            // Play est plus radical et direct que CrossFade pour le debug
            _animator.Play("TreasureOpen", 0, 0f);
        }

        public void TriggerTreasureSuccess()
        {
            _playerController.SetState(PlayerController.PlayerState.Treasure);
            ClearUpperBodyLayer();
            _animator.Play("TreasureSuccess", 0, 0f);
        }

        /// <summary>
        /// Force le layer UpperBody à retourner sur l'état Empty, interrompant
        /// toute animation de dégainage/rengainage en cours.
        /// </summary>
        private void ClearUpperBodyLayer()
        {
            if (_upperBodyLayerIndex > 0 && _upperBodyLayerIndex < _animator.layerCount)
            {
                // Reset les triggers pour éviter qu'ils ne se déclenchent après le saut
                _animator.ResetTrigger("DrawWeapon");
                _animator.ResetTrigger("SheatheWeapon");
                _animator.Play(_upperBodyEmptyState, _upperBodyLayerIndex, 0f);
            }
        }

        private void Update()
        {
            if (_playerController == null) return;

            // Ne pas écraser l'animation de respawn par les transitions de state normales
            if (_playerHealth != null && _playerHealth.IsRespawning) return;

            PlayerController.PlayerState currentState = _playerController.CurrentState;

            if (currentState != _lastState)
            {
                // Prévenir l'interruption de l'animation de saut par l'animation de chute
                if (_lastState == PlayerController.PlayerState.Jump && currentState == PlayerController.PlayerState.Fall)
                {
                    AnimatorStateInfo currentInfo = _animator.GetCurrentAnimatorStateInfo(0);
                    AnimatorStateInfo nextInfo = _animator.GetNextAnimatorStateInfo(0);

                    bool isPlayingJump = currentInfo.shortNameHash == _currentJumpHash && currentInfo.normalizedTime < _jumpAnimMinCompletion;
                    bool isTransitioningToJump = _animator.IsInTransition(0) && nextInfo.shortNameHash == _currentJumpHash;

                    if (isPlayingJump || isTransitioningToJump)
                    {
                        return; // On attend la fin de l'animation de saut
                    }
                }

                PlayAnimation(currentState);
                _lastState = currentState;
            }
        }

        private void PlayAnimation(PlayerController.PlayerState state)
        {
            string stateName = "";

            // Pour ces états, le corps entier doit être libre -> on vide le layer UpperBody
            bool isFullBodyState = (state == PlayerController.PlayerState.Jump
                                 || state == PlayerController.PlayerState.Fall
                                 || state == PlayerController.PlayerState.Climb
                                 || state == PlayerController.PlayerState.Push
                                 || state == PlayerController.PlayerState.BigPush
                                 || state == PlayerController.PlayerState.HardLand
                                 || state == PlayerController.PlayerState.Roll
                                 || state == PlayerController.PlayerState.Attack
                                 || state == PlayerController.PlayerState.Treasure);

            if (isFullBodyState)
            {
                ClearUpperBodyLayer();
            }

            switch (state)
            {
                case PlayerController.PlayerState.Idle:
                    stateName = "Idle";
                    break;
                case PlayerController.PlayerState.Move:
                    stateName = "Move";
                    break;
                case PlayerController.PlayerState.Jump:
                    // Priorité : saut-roulade > aléatoire > défaut
                    if (!string.IsNullOrEmpty(_rollJumpAnimationName) && _playerController.HasRollJumpBoost)
                    {
                        stateName = _rollJumpAnimationName;
                    }
                    else if (_useRandomJumps && _randomJumpAnimations != null && _randomJumpAnimations.Length > 0)
                    {
                        stateName = _randomJumpAnimations[Random.Range(0, _randomJumpAnimations.Length)];
                    }
                    else
                    {
                        stateName = "Jump";
                    }
                    _currentJumpHash = Animator.StringToHash(stateName);
                    break;
                case PlayerController.PlayerState.Fall:
                    stateName = "Fall";
                    break;
                case PlayerController.PlayerState.Land:
                    stateName = "Land";
                    break;
                case PlayerController.PlayerState.Climb:
                    stateName = "Climb";
                    break;
                case PlayerController.PlayerState.Push:
                    stateName = "pushAnim";
                    break;
                case PlayerController.PlayerState.BigPush:
                    stateName = "bigPushAnim";
                    break;
                case PlayerController.PlayerState.HardLand:
                    stateName = "FallingToRoll";
                    break;
                case PlayerController.PlayerState.Roll:
                    stateName = "Roll";
                    break;
                case PlayerController.PlayerState.Attack:
                    // Déjà géré par l'événement HandleAttackAction
                    stateName = "";
                    break;
                case PlayerController.PlayerState.Treasure:
                    // Géré par les méthodes TriggerTreasure...
                    stateName = "";
                    break;
            }

            if (!string.IsNullOrEmpty(stateName))
            {
                // Utiliser FixedTime garantit que _transitionDuration (ex: 0.15s) est bien en secondes 
                // absolues et non un pourcentage de la longueur de l'animation, ce qui évite l'effet de lenteur.
                _animator.CrossFadeInFixedTime(stateName, _transitionDuration);
            }
        }
        // --- Ponts pour les Animation Events ---
        // Ces méthodes sont appelées par les clips d'animation sur l'objet du modèle
        // et transmettent l'ordre au PlayerController situé sur le parent.

        public void AnimEvent_DealDamage()
        {
            if (_playerController != null) _playerController.AnimEvent_DealDamage();
        }

        public void AnimEvent_GrabSword()
        {
            if (_playerController != null) _playerController.AnimEvent_GrabSword();
        }

        public void AnimEvent_StoreSword()
        {
            if (_playerController != null) _playerController.AnimEvent_StoreSword();
        }
    }
}
