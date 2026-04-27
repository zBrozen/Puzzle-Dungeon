using UnityEngine;
using PuzzleDungeon.Interactions;

namespace PuzzleDungeon.Player
{
    public class ScarabLauncher : MonoBehaviour
    {
        [Header("Prefab Settings")]
        [SerializeField] private GameObject _scarabPrefab;
        [SerializeField] private Transform _launchPoint;
        [SerializeField] private Vector3 _spawnOffset = new Vector3(0, 1.5f, 0);
        
        [Header("Flight Camera")]
        [SerializeField] private float _flightDistance = 3f;
        [SerializeField] private Vector3 _flightOffset = new Vector3(0, 0.5f, 0);

        [Header("Controls")]
        [SerializeField] private KeyCode _launchKey = KeyCode.Q;
        [SerializeField] private float _cooldown = 0.5f;

        private PlayerController _playerController;
        private CameraController _cameraController;
        private Scarab _activeScarab;
        private float _lastLaunchTime;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _cameraController = FindFirstObjectByType<CameraController>();
            
            if (_launchPoint == null) _launchPoint = transform;
        }

        private void Update()
        {
            if (_activeScarab == null)
            {
                if (Input.GetKeyDown(_launchKey) && Time.time > _lastLaunchTime + _cooldown)
                {
                    Launch();
                }
            }
            else
            {
                if (Input.GetKeyDown(_launchKey))
                {
                    _activeScarab.Explode();
                }
            }
        }

        private void Launch()
        {
            if (_scarabPrefab == null) return;

            // Calcul de la position avec l'offset local
            Vector3 spawnPos = _launchPoint.position + _launchPoint.TransformDirection(_spawnOffset);
            GameObject go = Instantiate(_scarabPrefab, spawnPos, _launchPoint.rotation);
            _activeScarab = go.GetComponent<Scarab>();

            if (_activeScarab == null)
            {
                Destroy(go);
                return;
            }

            // On définit le joueur comme propriétaire pour éviter l'auto-explosion
            _activeScarab.SetOwner(transform);
            _activeScarab.OnScarabDestroyed += HandleScarabDestroyed;

            // IGNORER LA COLLISION PHYSIQUE (évite que le joueur ne "saute" au lancer)
            Collider playerCollider = GetComponent<Collider>();
            Collider scarabCollider = go.GetComponentInChildren<Collider>();
            if (playerCollider != null && scarabCollider != null)
            {
                Physics.IgnoreCollision(playerCollider, scarabCollider);
            }

            _playerController.IsLocked = true;

            if (_cameraController != null)
            {
                _cameraController.SetTarget(_activeScarab.transform, CameraController.CameraMode.DirectFollow, _flightDistance, _flightOffset);
            }

            _lastLaunchTime = Time.time;
        }

        private void HandleScarabDestroyed()
        {
            _activeScarab = null;
            _playerController.IsLocked = false;

            if (_cameraController != null)
            {
                _cameraController.ResetTarget();
            }
        }
    }
}
