using UnityEngine;
using UnityEngine.Events;

namespace PuzzleDungeon.Interactions
{
    public class SwitchGroup : MonoBehaviour
    {
        [Header("Switches in Group")]
        [SerializeField, Tooltip("Placez ici tous les interrupteurs qui doivent être activés ensemble.")]
        private SwordSwitch[] _switches;
        
        [Header("Events")]
        [Tooltip("Appelé quand tous les interrupteurs du groupe sont actifs en même temps.")]
        public UnityEvent OnAllSwitchesActivated;
        [Tooltip("Appelé si le groupe était complet mais qu'un interrupteur vient de s'éteindre.")]
        public UnityEvent OnGroupDeactivated;

        private bool _isGroupActive = false;

        private void OnEnable()
        {
            foreach (var s in _switches)
            {
                if (s != null)
                {
                    s.OnActivated.AddListener(CheckSwitches);
                    s.OnDeactivated.AddListener(CheckSwitches);
                }
            }
        }

        private void OnDisable()
        {
            foreach (var s in _switches)
            {
                if (s != null)
                {
                    s.OnActivated.RemoveListener(CheckSwitches);
                    s.OnDeactivated.RemoveListener(CheckSwitches);
                }
            }
        }

        private void CheckSwitches()
        {
            bool allActive = true;
            foreach (var s in _switches)
            {
                if (s != null && !s.IsOn)
                {
                    allActive = false;
                    break;
                }
            }

            if (allActive && !_isGroupActive)
            {
                _isGroupActive = true;
                OnAllSwitchesActivated?.Invoke();
            }
            else if (!allActive && _isGroupActive)
            {
                _isGroupActive = false;
                OnGroupDeactivated?.Invoke();
            }
        }
    }
}
