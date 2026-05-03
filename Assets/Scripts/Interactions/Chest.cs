using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using PuzzleDungeon.Player;

namespace PuzzleDungeon.Interactions
{
    [System.Serializable]
    public class ChestEventStep
    {
        public string Label = "New Step";
        public UnityEvent OnStepStart;
        public Transform CameraPoint; // Position où la caméra se déplace
        public Transform LookAtTarget; // Objet que la caméra doit regarder
        public float DelayBefore = 0.5f;
        public float StepDuration = 2.0f;
    }

    public class Chest : MonoBehaviour
    {
        [Header("Item Settings")]
        [SerializeField] private ItemData _itemInside;
        [SerializeField] private Transform _itemSpawnPoint;
        
        [Header("Animation Settings")]
        [SerializeField] private Animator _chestAnimator;
        [SerializeField] private string _openParameter = "Open";
        
        [Header("Lighting Settings")]
        [SerializeField] private Light _divineLight; // The spotlight above the chest
        [SerializeField] private Light _itemHighlight; // Extra light for the item itself
        [SerializeField] private Light _cameraSpotlight; // Light attached to camera for events
        [SerializeField] private float _dimSpeed = 2f;
        [SerializeField] private bool _dimAmbientLight = true;

        [Header("Interaction Settings")]
        [SerializeField] private Transform _interactionPoint; // Where the player should stand
        [SerializeField] private Transform _cameraPoint; // Optional: specific camera placement
        [SerializeField] private float _interactionDistance = 2f;
        
        [Header("Sequence Settings")]
        [SerializeField] private float _chestOpenDelay = 0.3f; // Réduit pour plus de réactivité
        [SerializeField] private float _itemShowDelay = 0.4f;
        [SerializeField] private float _sequenceEndDelay = 2.0f;
        [SerializeField] private float _itemSpinSpeed = 100f;

        [Header("Events")]
        public UnityEvent OnSequenceStart;
        public UnityEvent OnSequenceEnd;
        [SerializeField] private List<ChestEventStep> _sequentialEvents = new List<ChestEventStep>();

        [Header("Cleanup")]
        [SerializeField] private bool _destroyAfterSequence = false;

        private bool _isOpened = false;
        private GameObject _spawnedItem;
        private Color _originalAmbientColor;
        private float _originalReflectionIntensity;
        private Coroutine _lightCoroutine;

        public bool IsOpened => _isOpened;

        private void Start()
        {
            // On s'assure que les lumières sont éteintes au début
            if (_divineLight != null) _divineLight.gameObject.SetActive(false);
            if (_itemHighlight != null) _itemHighlight.gameObject.SetActive(false);
            if (_cameraSpotlight != null) _cameraSpotlight.gameObject.SetActive(false);
        }

        public void Open(PlayerController player)
        {
            if (_isOpened) return;
            StartCoroutine(ChestSequence(player));
        }

        private IEnumerator ChestSequence(PlayerController player)
        {
            _isOpened = true;
            player.IsLocked = true;

            // Trigger event at the start (ex: close doors, spawn enemies)
            OnSequenceStart?.Invoke();

            // Start dimming lights
            if (_lightCoroutine != null) StopCoroutine(_lightCoroutine);
            _lightCoroutine = StartCoroutine(TransitionLights(true));

            // 1. Position the player in front of the chest
            if (_interactionPoint != null)
            {
                player.transform.position = _interactionPoint.position;
                
                // On ne garde que la rotation Y pour éviter que le joueur ne bascule en avant/arrière
                Vector3 forward = _interactionPoint.forward;
                forward.y = 0;
                if (forward.sqrMagnitude > 0.001f)
                    player.transform.rotation = Quaternion.LookRotation(forward);
            }
            else
            {
                // Fallback: face the chest
                Vector3 lookDir = (transform.position - player.transform.position);
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f)
                    player.transform.rotation = Quaternion.LookRotation(lookDir);
            }

            // 2. Camera traveling
            CameraController cam = Camera.main.GetComponent<CameraController>();
            if (cam != null)
            {
                if (_cameraPoint != null)
                {
                    // Use specific camera point
                    cam.SetTarget(_cameraPoint, CameraController.CameraMode.DirectFollow, 0f, Vector3.zero);
                }
                else
                {
                    // Fallback: move camera behind player for the Zelda effect
                    cam.FocusOn(player.transform, 5.0f, new Vector3(0, 1.5f, -3f), Vector3.up * 1.5f);
                }
            }

            // 3. Player Open Chest Animation
            PlayerAnimator anim = player.GetComponentInChildren<PlayerAnimator>();
            if (anim != null)
            {
                anim.TriggerTreasureOpening();
            }
            else
            {
                Debug.LogError("[Chest] PlayerAnimator not found on player!");
            }

            yield return new WaitForSeconds(_chestOpenDelay);

            // 4. Chest Animation
            if (_chestAnimator != null)
            {
                // Play force l'animation même sans transitions
                _chestAnimator.Play(_openParameter);
            }
            else
            {
                Debug.LogWarning("[Chest] Chest Animator is not assigned in the inspector!");
            }

            yield return new WaitForSeconds(_itemShowDelay);

            // 5. Player turns around
            player.transform.Rotate(0, 180, 0);
            
            // 6. Spawn and show item
            if (_itemInside != null && _itemInside.Prefab != null)
            {
                Transform spawnPoint = _itemSpawnPoint != null ? _itemSpawnPoint : transform;
                Vector3 spawnPos = spawnPoint.position;
                
                _spawnedItem = Instantiate(_itemInside.Prefab, spawnPos, Quaternion.identity);
                
                // On utilise le scale du point de spawn pour redimensionner l'objet
                _spawnedItem.transform.localScale = spawnPoint.localScale;
                
                // Petit effet de "pop" (agrandissement progressif)
                StartCoroutine(AnimateItemEntry(_spawnedItem, spawnPoint.localScale));
                StartCoroutine(SpinItem(_spawnedItem));
            }

            // 7. Player Success Animation (holding item)
            if (anim != null)
            {
                anim.TriggerTreasureSuccess();
            }

            yield return new WaitForSeconds(_sequenceEndDelay);

            // 8. Give item and hide chest
            if (_spawnedItem != null) Destroy(_spawnedItem);
            HideChestVisuals();
            
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            if (inventory != null && _itemInside != null)
            {
                inventory.AddItem(_itemInside);
            }
            
            Debug.Log($"Collected: {_itemInside.ItemName}");

            // 9. Sequential events with camera focus (Player still locked)
            if (_sequentialEvents.Count > 0)
            {
                yield return StartCoroutine(ExecuteSequentialEvents(cam));
            }

            // Restore lights
            if (_lightCoroutine != null) StopCoroutine(_lightCoroutine);
            _lightCoroutine = StartCoroutine(TransitionLights(false));

            // Final restoration
            player.IsLocked = false;
            player.SetState(PlayerController.PlayerState.Idle);
            if (cam != null) cam.ResetTarget();

            // Trigger event at the end
            OnSequenceEnd?.Invoke();

            if (_destroyAfterSequence) FinalDestroy();
        }

        public void FinalDestroy()
        {
            Destroy(gameObject);
        }

        private void HideChestVisuals()
        {
            // Désactive tous les MeshRenderers pour faire disparaître le coffre visuellement
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers) r.enabled = false;

            // Désactive la collision pour que le joueur puisse passer à travers
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Optionnel : désactiver l'Animator pour arrêter les mouvements
            if (_chestAnimator != null) _chestAnimator.enabled = false;
            
            // On désactive aussi les lumières s'il en reste
            if (_divineLight != null) _divineLight.enabled = false;
            if (_itemHighlight != null) _itemHighlight.enabled = false;
        }

        private IEnumerator ExecuteSequentialEvents(CameraController cam)
        {
            foreach (var step in _sequentialEvents)
            {
                // Sécurité : on s'assure que le spot est éteint pendant le trajet
                if (_cameraSpotlight != null) _cameraSpotlight.gameObject.SetActive(false);

                if (cam != null)
                {
                    if (step.LookAtTarget != null)
                    {
                        // On commence à regarder la cible immédiatement
                        cam.FocusOn(step.LookAtTarget, step.StepDuration + step.DelayBefore + 1.5f, Vector3.zero, Vector3.zero);
                    }

                    if (step.CameraPoint != null)
                    {
                        // On lance le déplacement
                        cam.SetTarget(step.CameraPoint, CameraController.CameraMode.DirectFollow, 0f, Vector3.zero);
                    }

                    // On laisse une frame passer pour que la caméra enregistre le nouvel état
                    yield return null;

                    // ATTENTE : On attend que la caméra ait fini son traveling
                    while (cam.IsTransitioning)
                    {
                        yield return null;
                    }
                }

                // Petit délai supplémentaire (DelayBefore) + stabilisation de sécurité
                float waitTime = Mathf.Max(0.1f, step.DelayBefore);
                yield return new WaitForSeconds(waitTime);

                // Une fois bien stabilisé, on allume le spot
                if (_cameraSpotlight != null) _cameraSpotlight.gameObject.SetActive(true);

                step.OnStepStart?.Invoke();

                yield return new WaitForSeconds(step.StepDuration);

                // On éteint le spot avant la suite
                if (_cameraSpotlight != null) _cameraSpotlight.gameObject.SetActive(false);
            }
        }

        private IEnumerator TransitionLights(bool dim)
        {
            if (!_dimAmbientLight) yield break;

            if (dim)
            {
                _originalAmbientColor = RenderSettings.ambientLight;
                _originalReflectionIntensity = RenderSettings.reflectionIntensity;
                if (_divineLight != null) _divineLight.gameObject.SetActive(true);
                if (_itemHighlight != null) _itemHighlight.gameObject.SetActive(true);
            }

            Color targetColor = dim ? Color.black : _originalAmbientColor;
            float targetReflection = dim ? 0f : _originalReflectionIntensity;

            float elapsed = 0f;
            float duration = 1f / _dimSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetColor, t);
                RenderSettings.reflectionIntensity = Mathf.Lerp(RenderSettings.reflectionIntensity, targetReflection, t);
                yield return null;
            }

            RenderSettings.ambientLight = targetColor;
            RenderSettings.reflectionIntensity = targetReflection;

            if (!dim)
            {
                if (_divineLight != null) _divineLight.gameObject.SetActive(false);
                if (_itemHighlight != null) _itemHighlight.gameObject.SetActive(false);
            }
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
                // Effet de rebond élastique
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

        private void OnDrawGizmos()
        {
            if (_interactionPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(_interactionPoint.position, _interactionPoint.position + _interactionPoint.forward * 0.5f);
                Gizmos.DrawWireSphere(_interactionPoint.position, 0.1f);
            }
            if (_cameraPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(_cameraPoint.position, _cameraPoint.position + _cameraPoint.forward * 0.5f);
                Gizmos.DrawWireSphere(_cameraPoint.position, 0.1f);
            }
        }
    }
}
