using UnityEngine;

namespace PuzzleDungeon.Interactions
{
    public class Scarab : MonoBehaviour
    {
        [Header("Flight Settings")]
        [SerializeField] private float _forwardSpeed = 8f;
        [SerializeField] private float _turnSpeed = 120f;
        [SerializeField] private float _pitchSpeed = 100f;
        [SerializeField] private float _maxLifetime = 15f;
        
        [Header("Visuals")]
        [SerializeField] private Transform _model;
        [SerializeField] private float _tiltAmount = 30f;
        [SerializeField] private float _smoothRotationTime = 10f;

        private float _currentPitch = 0f;
        private float _currentYaw = 0f;
        private float _currentRoll = 0f;
        private float _lifetime = 0f;
        private bool _isDestroyed = false;

        public System.Action OnScarabDestroyed;
        private Transform _owner;

        private void Start()
        {
            _currentYaw = transform.eulerAngles.y;
            _currentPitch = transform.eulerAngles.x;
            if (_currentPitch > 180) _currentPitch -= 360;
        }

        public void SetOwner(Transform owner)
        {
            _owner = owner;
        }

        private void Update()
        {
            if (_isDestroyed) return;

            HandleInputAndMovement();

            _lifetime += Time.deltaTime;
            if (_lifetime >= _maxLifetime) Explode();
        }

        private void HandleInputAndMovement()
        {
            // 1. Inputs (ZQSD / WASD / Flèches)
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // --- INVERSION INTELLIGENTE ---
            // Si on est à l'envers (tête vers le sol), on inverse la gauche et la droite
            // pour que le contrôle reste intuitif à l'écran.
            bool isUpsideDown = Vector3.Dot(transform.up, Vector3.up) < 0;
            if (isUpsideDown) horizontal = -horizontal;

            // 2. Calcul des angles
            _currentYaw += horizontal * _turnSpeed * Time.deltaTime;
            _currentPitch -= vertical * _pitchSpeed * Time.deltaTime; // Plus de clamp pour permettre les loopings
            _currentRoll = 0f;

            // 3. Application de la rotation
            transform.rotation = Quaternion.Euler(_currentPitch, _currentYaw, _currentRoll);

            // 4. Détection de collision manuelle (Bulletproof)
            float moveDistance = _forwardSpeed * Time.deltaTime;
            RaycastHit hit;
            if (Physics.SphereCast(transform.position, 0.2f, transform.forward, out hit, moveDistance + 0.1f))
            {
                // On ignore seulement si c'est le joueur (l'owner)
                if (_owner == null || !hit.collider.transform.IsChildOf(_owner)) 
                {
                    HandleImpact(hit.collider);
                    return; 
                }
            }

            // 5. Mouvement
            transform.Translate(Vector3.forward * moveDistance);
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleImpact(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleImpact(other);
        }

        private void HandleImpact(Collider other)
        {
            if (_isDestroyed) return;
            
            // Sécurité supplémentaire : si c'est l'owner, on ignore
            if (_owner != null && other.transform.IsChildOf(_owner)) return;

            // Détection des cibles (sur l'objet lui-même ou ses parents)
            ScarabTarget target = other.GetComponentInParent<ScarabTarget>();
            if (target != null)
            {
                target.OnHitByScarab();
            }

            Explode();
        }

        public void Explode()
        {
            if (_isDestroyed) return;
            _isDestroyed = true;
            
            Debug.Log("[Scarab] Impact !");
            
            OnScarabDestroyed?.Invoke();
            Destroy(gameObject);
        }
    }
}
