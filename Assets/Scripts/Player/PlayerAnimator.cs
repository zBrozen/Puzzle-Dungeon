using UnityEngine;

namespace PuzzleDungeon.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private float _transitionDuration = 0.15f;

        private Animator _animator;
        private PlayerController.PlayerState _lastState;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_playerController == null)
                _playerController = GetComponentInParent<PlayerController>();
        }

        private void Update()
        {
            if (_playerController == null) return;

            PlayerController.PlayerState currentState = _playerController.CurrentState;

            if (currentState != _lastState)
            {
                PlayAnimation(currentState);
                _lastState = currentState;
            }
        }

        private void PlayAnimation(PlayerController.PlayerState state)
        {
            string stateName = "";

            switch (state)
            {
                case PlayerController.PlayerState.Idle:
                    stateName = "Idle";
                    break;
                case PlayerController.PlayerState.Move:
                    stateName = "Move";
                    break;
                case PlayerController.PlayerState.Jump:
                    stateName = "Jump";
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
            }

            if (!string.IsNullOrEmpty(stateName))
            {
                _animator.CrossFade(stateName, _transitionDuration);
            }
        }
    }
}
