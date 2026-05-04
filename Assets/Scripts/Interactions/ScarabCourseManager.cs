using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace PuzzleDungeon.Interactions
{
    public class ScarabCourseManager : MonoBehaviour
    {
        [Header("Course Setup")]
        [SerializeField] private List<ScarabRing> _rings = new List<ScarabRing>();
        [SerializeField] private bool _resetOnScarabDeath = true;
        [SerializeField] private float _miniGameMaxLifetime = 5f;

        [Header("Materials")]
        [SerializeField] private Material _nextRingMaterial;
        [SerializeField] private Material _upcomingRingMaterial;

        [Header("Events")]
        public UnityEvent OnCourseStarted;
        public UnityEvent OnRingPassed;
        public UnityEvent OnCourseCompleted;
        public UnityEvent OnCourseFailed;

        private int _currentRingIndex = 0;
        private bool _isCourseActive = false;

        private void Start()
        {
            foreach (var ring in _rings)
            {
                ring.OnPassedThrough += HandleRingPassed;
            }
            
            UpdateRingsVisuals();
        }

        private void OnDestroy()
        {
            foreach (var ring in _rings)
            {
                if (ring != null) ring.OnPassedThrough -= HandleRingPassed;
            }
        }

        private void HandleRingPassed(ScarabRing ring, Scarab scarab)
        {
            // Vérification si c'est bien l'anneau attendu
            if (_rings.IndexOf(ring) == _currentRingIndex)
            {
                if (!_isCourseActive)
                {
                    _isCourseActive = true;
                    OnCourseStarted?.Invoke();
                }

                // Reset du timer du scarabée à chaque anneau
                if (scarab != null)
                {
                    scarab.ResetLifetime(_miniGameMaxLifetime);
                }

                _currentRingIndex++;
                Debug.Log($"[ScarabCourse] {_currentRingIndex}/{_rings.Count} step");
                OnRingPassed?.Invoke();

                if (_currentRingIndex >= _rings.Count)
                {
                    CompleteCourse();
                }
                else
                {
                    UpdateRingsVisuals();
                }
            }
        }

        private void UpdateRingsVisuals()
        {
            for (int i = 0; i < _rings.Count; i++)
            {
                bool isNext = (i == _currentRingIndex);
                bool isUpcoming = (i == _currentRingIndex + 1);
                
                _rings[i].SetState(isNext, isUpcoming, _nextRingMaterial, _upcomingRingMaterial);
            }
        }

        private void CompleteCourse()
        {
            _isCourseActive = false;
            Debug.Log("[ScarabCourse] Course completed!");
            OnCourseCompleted?.Invoke();
            
            // On peut cacher tous les anneaux à la fin
            foreach (var ring in _rings) ring.SetState(false, false, null, null);
        }

        public void ResetCourse()
        {
            _currentRingIndex = 0;
            _isCourseActive = false;
            UpdateRingsVisuals();
            OnCourseFailed?.Invoke();
        }

        // Optionnel : s'abonner à l'explosion du scarabée pour reset la course
        private void Update()
        {
            if (_isCourseActive && _resetOnScarabDeath)
            {
                // On pourrait chercher si un scarabée existe encore dans la scène
                // Mais c'est plus propre d'écouter un événement global ou de vérifier périodiquement
                if (GameObject.FindObjectOfType<Scarab>() == null)
                {
                    ResetCourse();
                }
            }
        }
    }
}
