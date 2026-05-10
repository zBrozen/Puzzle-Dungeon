using UnityEngine;
using UnityEngine.Events;

namespace PuzzleDungeon.Interactions
{
    public class SwitchGroup : MonoBehaviour
    {
        [Header("Switches in Group")]
        [SerializeField, Tooltip("Placez ici tous les interrupteurs qui doivent être activés ensemble.")]
        private SwordSwitch[] _switches;

        [SerializeField, Tooltip("Si vrai, déclenche les événements OnAllSwitchesActivated/OnGroupDeactivated au lancement selon l'état initial.")]
        private bool _syncEventsOnStart = true;
        
        [Header("Events")]
        [Tooltip("Appelé quand tous les interrupteurs du groupe sont actifs en même temps.")]
        public UnityEvent OnAllSwitchesActivated;
        [Tooltip("Appelé si le groupe était complet mais qu'un interrupteur vient de s'éteindre.")]
        public UnityEvent OnGroupDeactivated;

        private bool _isGroupActive = false;

        private void Start()
        {
            // Initialisation de l'état
            bool allActive = IsEverythingActive();
            _isGroupActive = allActive;

            if (_syncEventsOnStart)
            {
                if (_isGroupActive) OnAllSwitchesActivated?.Invoke();
                else OnGroupDeactivated?.Invoke();
            }
        }

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
            bool allActive = IsEverythingActive();

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

        private bool IsEverythingActive()
        {
            if (_switches == null || _switches.Length == 0) return false;

            foreach (var s in _switches)
            {
                if (s == null || !s.IsOn)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
