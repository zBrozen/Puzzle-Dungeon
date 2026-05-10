using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;
using PuzzleDungeon.Player;
using PuzzleDungeon.Systems.Save;
using PuzzleDungeon.Systems;

namespace PuzzleDungeon.Interactions
{
    [RequireComponent(typeof(UniqueIdentifier))]
    public class ScarabCourseManager : MonoBehaviour, ISaveable
    {
        [Header("Course Setup")]
        [SerializeField] private List<ScarabRing> _rings = new List<ScarabRing>();
        [SerializeField] private bool _resetOnScarabDeath = true;
        [SerializeField] private float _miniGameMaxLifetime = 5f;

        [Header("Reward Settings")]
        [SerializeField] private ItemData _rewardItem;
        [SerializeField] private Transform _rewardCameraPoint;
        [SerializeField] private Vector3 _itemSpawnOffset = new Vector3(0, 2f, 0);
        [SerializeField] private float _itemSpinSpeed = 100f;

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
        private bool _isCompleted = false;
        private bool _isPlayerOnPlatform = false;
        private UniqueIdentifier _uid;

        public string UniqueID => _uid != null ? _uid.Id : string.Empty;

        public void PopulateSaveData(GameData data)
        {
            if (string.IsNullOrEmpty(UniqueID)) return;
            if (_isCompleted)
            {
                if (!data.solvedPuzzles.Contains(UniqueID))
                    data.solvedPuzzles.Add(UniqueID);
            }
        }

        public void LoadFromSaveData(GameData data)
        {
            if (string.IsNullOrEmpty(UniqueID)) return;
            if (data.solvedPuzzles.Contains(UniqueID))
            {
                _isCompleted = true;
                _currentRingIndex = _rings.Count;
                // On cache les anneaux immédiatement si déjà fini
                foreach (var ring in _rings) ring.SetState(false, false, null, null);
            }
        }

        private void Awake()
        {
            _uid = GetComponent<UniqueIdentifier>();
        }

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
                    // Sécurité : on ne peut pas lancer la course si on n'est pas sur la plateforme
                    if (!_isPlayerOnPlatform) return;

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

                // Si on n'a pas commencé et qu'on n'est pas sur la plateforme, on cache les 2 premiers anneaux
                if (!_isCourseActive && !_isPlayerOnPlatform && (i == 0 || i == 1))
                {
                    _rings[i].SetState(false, false, null, null);
                    continue;
                }
                
                _rings[i].SetState(isNext, isUpcoming, _nextRingMaterial, _upcomingRingMaterial);
            }
        }

        private void CompleteCourse()
        {
            _isCourseActive = false;
            _isCompleted = true;
            Debug.Log("[ScarabCourse] Course completed!");
            OnCourseCompleted?.Invoke();
            
            // On peut cacher tous les anneaux à la fin
            foreach (var ring in _rings) ring.SetState(false, false, null, null);

            if (_rewardItem != null)
            {
                PlayerController player = GameObject.FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    StartCoroutine(RewardSequence(player));
                }
            }
        }

        private IEnumerator RewardSequence(PlayerController player)
        {
            player.IsLocked = true;
            player.SetState(PlayerController.PlayerState.Treasure);

            // 1. Caméra
            CameraController cam = Camera.main.GetComponent<CameraController>();
            if (cam != null)
            {
                if (_rewardCameraPoint != null)
                {
                    cam.SetTarget(_rewardCameraPoint, CameraController.CameraMode.DirectFollow, 0.5f, Vector3.zero);
                }
                else
                {
                    cam.FocusOn(player.transform, 5.0f, new Vector3(0, 1.5f, -3f), Vector3.up * 1.5f);
                }
            }

            // 2. Animation d'ouverture (même si pas de coffre, pour le style)
            PlayerAnimator anim = player.GetComponentInChildren<PlayerAnimator>();
            if (anim != null)
            {
                anim.TriggerTreasureOpening();
            }

            yield return new WaitForSeconds(0.5f);

            // 3. Apparition de l'objet
            GameObject spawnedItem = null;
            if (_rewardItem != null && _rewardItem.Prefab != null)
            {
                Vector3 spawnPos = player.transform.position + _itemSpawnOffset;
                spawnedItem = Instantiate(_rewardItem.Prefab, spawnPos, Quaternion.identity);
                
                // Petit effet de pop
                StartCoroutine(AnimateItemEntry(spawnedItem, Vector3.one));
                StartCoroutine(SpinItem(spawnedItem));
            }

            // 4. Animation de succès
            if (anim != null)
            {
                anim.TriggerTreasureSuccess();
            }

            yield return new WaitForSeconds(2.5f);

            // 5. Cleanup et Don de l'objet
            if (spawnedItem != null) Destroy(spawnedItem);
            
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            if (inventory != null && _rewardItem != null)
            {
                inventory.AddItem(_rewardItem);
            }

            // 6. Restauration
            player.IsLocked = false;
            player.SetState(PlayerController.PlayerState.Idle);
            if (cam != null) cam.ResetTarget();
        }

        private IEnumerator AnimateItemEntry(GameObject item, Vector3 targetScale)
        {
            item.transform.localScale = Vector3.zero;
            float elapsed = 0f;
            float duration = 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float s = -t * (t - 2); // Quad Ease Out
                item.transform.localScale = targetScale * s;
                yield return null;
            }
            item.transform.localScale = targetScale;
        }

        private IEnumerator SpinItem(GameObject item)
        {
            while (item != null)
            {
                item.transform.Rotate(Vector3.up, _itemSpinSpeed * Time.deltaTime);
                yield return null;
            }
        }

        public void ResetCourse()
        {
            _currentRingIndex = 0;
            _isCourseActive = false;
            UpdateRingsVisuals();
            OnCourseFailed?.Invoke();
        }

        private void Update()
        {
            if (_isCourseActive && _resetOnScarabDeath)
            {
                if (GameObject.FindObjectOfType<Scarab>() == null)
                {
                    ResetCourse();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _isPlayerOnPlatform = true;
                UpdateRingsVisuals();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _isPlayerOnPlatform = false;
                UpdateRingsVisuals();
            }
        }
    }
}
