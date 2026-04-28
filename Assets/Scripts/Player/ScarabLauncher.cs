using UnityEngine;
using PuzzleDungeon.Interactions;

namespace PuzzleDungeon.Player
{
    public class ScarabLauncher : MonoBehaviour
    {
        public enum LauncherState { Idle, Aiming, Flying }

        [Header("Prefab Settings")]
        [SerializeField] private GameObject _scarabPrefab;
        [SerializeField] private Transform _launchPoint;
        [SerializeField] private Vector3 _spawnOffset = new Vector3(0, 1.5f, 0);
        
        [Header("Flight Camera")]
        [SerializeField] private float _flightDistance = 3f;
        [SerializeField] private Vector3 _flightOffset = new Vector3(0, 0.5f, 0);

        [Header("Aiming Camera")]
        [SerializeField] private float _aimingDistance = 2.5f;
        [SerializeField] private Vector3 _aimingOffset = new Vector3(0.6f, 1.6f, 0f);
        [SerializeField] private float _aimingMaxVerticalAngle = 80f;

        [Header("Controls")]
        [SerializeField] private KeyCode _launchKey = KeyCode.Q;
        [SerializeField] private KeyCode _cancelKey = KeyCode.Space;
        [SerializeField] private float _cooldown = 0.5f;

        [Header("UI")]
        [SerializeField] private ScarabReticleUI _reticleUI;

        private PlayerController _playerController;
        private CameraController _cameraController;
        private Scarab _activeScarab;
        private float _lastLaunchTime;
        private LauncherState _currentState = LauncherState.Idle;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _cameraController = FindFirstObjectByType<CameraController>();
            
            if (_launchPoint == null) _launchPoint = transform;
            if (_reticleUI == null) _reticleUI = FindFirstObjectByType<ScarabReticleUI>();
        }

        private void Update()
        {
            switch (_currentState)
            {
                case LauncherState.Idle:
                    if (Input.GetKeyDown(_launchKey) && Time.time > _lastLaunchTime + _cooldown)
                    {
                        StartAiming();
                    }
                    break;

                case LauncherState.Aiming:
                    HandleAiming();
                    break;

                case LauncherState.Flying:
                    if (Input.GetKeyDown(_launchKey))
                    {
                        if (_activeScarab != null) _activeScarab.Explode();
                    }
                    break;
            }
        }

        private void StartAiming()
        {
            if (_scarabPrefab == null) return;

            // Forcer l'alignement du joueur sur la caméra immédiatement
            if (Camera.main != null)
            {
                Transform camTransform = Camera.main.transform;
                Vector3 camForward = camTransform.forward;
                camForward.y = 0;
                if (camForward.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(camForward);
                }
            }

            // Spawn preview (le scarabée ne vole pas encore grâce à la modif dans Scarab.cs)
            Vector3 spawnPos = _launchPoint.position + _launchPoint.TransformDirection(_spawnOffset);
            
            // On utilise la rotation de la caméra pour inclure le pitch (inclinaison verticale)
            Quaternion spawnRot = Camera.main != null ? Camera.main.transform.rotation : _launchPoint.rotation;
            GameObject go = Instantiate(_scarabPrefab, spawnPos, spawnRot);
            _activeScarab = go.GetComponent<Scarab>();

            if (_activeScarab == null)
            {
                Destroy(go);
                return;
            }

            _activeScarab.SetOwner(transform);
            _activeScarab.OnScarabDestroyed += HandleScarabDestroyed;
            
            if (_cameraController != null) _cameraController.SetIgnoreTarget(_activeScarab.transform);

            // Ignorer la collision physique
            Collider playerCollider = GetComponent<Collider>();
            Collider scarabCollider = go.GetComponentInChildren<Collider>();
            if (playerCollider != null && scarabCollider != null)
            {
                Physics.IgnoreCollision(playerCollider, scarabCollider);
            }

            _playerController.IsLocked = true;
            _currentState = LauncherState.Aiming;
            
            if (_cameraController != null)
            {
                _cameraController.SetTarget(transform, CameraController.CameraMode.Orbit, _aimingDistance, _aimingOffset, null, _aimingMaxVerticalAngle);
            }

            if (_reticleUI != null) _reticleUI.SetVisible(true);
        }

        private void HandleAiming()
        {
            // Rotation du joueur pour suivre la caméra
            if (Camera.main != null)
            {
                Transform camTransform = Camera.main.transform;
                Vector3 camForward = camTransform.forward;
                camForward.y = 0;
                if (camForward.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(camForward);
                }
            }

            // Maintien du scarabée devant le joueur
            if (_activeScarab != null)
            {
                _activeScarab.transform.position = _launchPoint.position + _launchPoint.TransformDirection(_spawnOffset);
                
                // Le scarabée suit la rotation de la caméra (inclinaison comprise)
                if (Camera.main != null)
                {
                    _activeScarab.transform.rotation = Camera.main.transform.rotation;
                }
            }

            if (Input.GetKeyDown(_cancelKey))
            {
                CancelAiming();
            }
            else if (Input.GetKeyDown(_launchKey))
            {
                ConfirmLaunch();
            }
        }

        private void ConfirmLaunch()
        {
            if (_activeScarab == null) return;

            _activeScarab.Launch();
            _currentState = LauncherState.Flying;
            
            if (_reticleUI != null) _reticleUI.SetVisible(false);

            if (_cameraController != null)
            {
                _cameraController.SetTarget(_activeScarab.transform, CameraController.CameraMode.DirectFollow, _flightDistance, _flightOffset);
            }

            _lastLaunchTime = Time.time;
        }

        private void CancelAiming()
        {
            if (_activeScarab != null)
            {
                // Détruire le scarabée déclenchera HandleScarabDestroyed via l'action OnScarabDestroyed
                Destroy(_activeScarab.gameObject);
            }
        }

        private void HandleScarabDestroyed()
        {
            _activeScarab = null;
            _playerController.IsLocked = false;
            _currentState = LauncherState.Idle;

            if (_cameraController != null) _cameraController.SetIgnoreTarget(null);
            if (_reticleUI != null) _reticleUI.SetVisible(false);

            if (_cameraController != null)
            {
                _cameraController.ResetTarget();
            }
        }
    }
}
