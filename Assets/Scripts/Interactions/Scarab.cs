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
        private bool _isFlying = false;

        public System.Action OnScarabDestroyed;
        private Transform _owner;
        
        [Header("Sensors")]
        [SerializeField] private float _sensorCenterYOffset = 0.2f;
        [SerializeField] private float _sensorHorizontalSpacing = 0.8f;
        [SerializeField] private float _sensorVerticalSpacing = 0.4f;
        [SerializeField] private float _sensorMaxDistance = 3.5f;
        
        private struct SensorBeam
        {
            public LineRenderer Line;
            public GameObject Reticle;
            public Vector3 LocalDirection;
        }
        private SensorBeam[] _sensorBeams;

        private void Start()
        {
            _currentYaw = transform.eulerAngles.y;
            _currentPitch = transform.eulerAngles.x;
            if (_currentPitch > 180) _currentPitch -= 360;
            
            SetupSensors();
        }

        private void SetupSensors()
        {
            Vector3[] directions = new Vector3[] {
                Vector3.down,  // Bas
                Vector3.up,    // Haut
                Vector3.left,  // Gauche
                Vector3.right  // Droite
            };

            _sensorBeams = new SensorBeam[directions.Length];

            for (int i = 0; i < directions.Length; i++)
            {
                // Chaque ligne doit être sur son propre GameObject car un objet ne peut avoir qu'un LineRenderer
                GameObject lineObj = new GameObject($"SensorLine_{i}");
                lineObj.transform.SetParent(transform);
                LineRenderer line = lineObj.AddComponent<LineRenderer>();
                
                line.startWidth = 0.08f;
                line.endWidth = 0.01f;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.positionCount = 2;
                line.enabled = false;

                GameObject reticle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                reticle.name = $"SensorReticle_{i}";
                Destroy(reticle.GetComponent<Collider>());
                reticle.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
                
                MeshRenderer reticleRenderer = reticle.GetComponent<MeshRenderer>();
                reticleRenderer.material = new Material(Shader.Find("Sprites/Default"));
                reticleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                reticle.SetActive(false);

                _sensorBeams[i] = new SensorBeam { Line = line, Reticle = reticle, LocalDirection = directions[i] };
            }
        }

        public void SetOwner(Transform owner)
        {
            _owner = owner;
        }

        public void Launch()
        {
            _isFlying = true;
            _lifetime = 0f;
            
            // Réinitialiser les angles sur la rotation actuelle (fin de visée)
            _currentYaw = transform.eulerAngles.y;
            _currentPitch = transform.eulerAngles.x;
            if (_currentPitch > 180) _currentPitch -= 360;

            if (_sensorBeams != null)
            {
                foreach (var sensor in _sensorBeams)
                {
                    sensor.Line.enabled = true;
                    sensor.Reticle.SetActive(true);
                }
            }
        }

        private void Update()
        {
            if (_isDestroyed || !_isFlying) return;

            HandleInputAndMovement();
            UpdateSensors();

            _lifetime += Time.deltaTime;
            if (_lifetime >= _maxLifetime) Explode();
        }

        private void UpdateSensors()
        {
            if (_sensorBeams == null) return;
            
            // Le centre de base des capteurs (ajusté en Y si le pivot du scarabée est trop bas)
            Vector3 sensorCenter = transform.position + transform.up * _sensorCenterYOffset;
            
            foreach (var sensor in _sensorBeams)
            {
                Vector3 worldDir = transform.TransformDirection(sensor.LocalDirection);
                
                // Espacement dynamique selon l'axe
                float originOffset = _sensorVerticalSpacing;
                if (Mathf.Abs(sensor.LocalDirection.x) > 0.1f) originOffset = _sensorHorizontalSpacing;
                
                // On utilise RaycastAll pour pouvoir ignorer les colliders du scarabée (ailes, etc)
                RaycastHit[] hits = Physics.RaycastAll(sensorCenter, worldDir, 50f);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                bool hitFound = false;
                RaycastHit validHit = new RaycastHit();

                foreach (var hit in hits)
                {
                    // Ignorer le joueur
                    if (_owner != null && hit.collider.transform.IsChildOf(_owner)) continue;
                    // Ignorer le scarabée lui-même
                    if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;

                    validHit = hit;
                    hitFound = true;
                    break;
                }

                // On masque si on ne touche rien d'intéressant OU si l'obstacle est trop loin
                if (!hitFound || validHit.distance > _sensorMaxDistance) 
                {
                    HideSensor(sensor);
                    continue;
                }

                if (!sensor.Line.enabled) sensor.Line.enabled = true;
                
                // Décaler l'origine dans la direction du laser
                Vector3 laserOrigin = sensorCenter + worldDir * originOffset; 
                
                sensor.Line.SetPosition(0, laserOrigin);
                sensor.Line.SetPosition(1, validHit.point);
                
                Color laserColor;
                if (validHit.distance < 1.0f)
                {
                    laserColor = new Color(1f, 0f, 0f, 0.8f); // Rouge vif (Très proche)
                }
                else if (validHit.distance < 1.8f)
                {
                    laserColor = new Color(1f, 0.5f, 0f, 0.6f); // Orange (Attention)
                }
                else
                {
                    laserColor = new Color(0f, 1f, 1f, 0.4f); // Cyan/Bleu (Détection)
                }

                sensor.Line.startColor = laserColor;
                sensor.Line.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.2f);

                if (!sensor.Reticle.activeSelf) sensor.Reticle.SetActive(true);
                
                sensor.Reticle.transform.position = validHit.point + validHit.normal * 0.05f;
                sensor.Reticle.transform.up = validHit.normal;
                
                float targetScale = Mathf.Clamp(0.8f * (validHit.distance / _sensorMaxDistance), 0.2f, 0.8f);
                sensor.Reticle.transform.localScale = new Vector3(targetScale, 0.01f, targetScale);
                sensor.Reticle.GetComponent<MeshRenderer>().material.color = new Color(laserColor.r, laserColor.g, laserColor.b, 0.8f);
            }
        }

        private void HideSensor(SensorBeam sensor)
        {
            if (sensor.Line.enabled) sensor.Line.enabled = false;
            if (sensor.Reticle.activeSelf) sensor.Reticle.SetActive(false);
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
            _currentPitch -= vertical * _pitchSpeed * Time.deltaTime; 
            
            // Calcul du Roll (inclinaison) visuel
            float targetRoll = -horizontal * _tiltAmount;
            _currentRoll = Mathf.Lerp(_currentRoll, targetRoll, Time.deltaTime * _smoothRotationTime);

            // 3. Application de la rotation (Pivot principal reste droit pour la caméra)
            transform.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);

            // Application de l'inclinaison uniquement au modèle visuel
            if (_model != null)
            {
                _model.localRotation = Quaternion.Euler(0, 0, _currentRoll);
            }

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
            
            if (_sensorBeams != null)
            {
                foreach (var sensor in _sensorBeams)
                {
                    if (sensor.Line != null) Destroy(sensor.Line.gameObject);
                    if (sensor.Reticle != null) Destroy(sensor.Reticle);
                }
            }
            
            OnScarabDestroyed?.Invoke();
            Destroy(gameObject);
        }
    }
}
