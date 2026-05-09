using UnityEngine;

namespace PuzzleDungeon.Interactions
{
    [RequireComponent(typeof(Rigidbody))]
    public class PushableBlock : MonoBehaviour
    {
        [SerializeField] private float _drag = 10f;
        [SerializeField] private float _gravityScale = 1.5f;
        [SerializeField, Range(0.1f, 5f)] private float _pushResistance = 1f;
        [SerializeField, Range(-1f, 1f)] private float _edgeDetectionOffset = 0.0f;
        [SerializeField] private float _bigPushOffset = 5.0f;

        [Header("Ice Mechanics")]
        [SerializeField, Tooltip("Tag du sol qui agit comme de la glace")] 
        private string _iceFloorTag = "IceFloor";
        [SerializeField, Tooltip("Vitesse initiale donnée au bloc lorsqu'il est poussé sur la glace")] 
        private float _icePushSpeed = 8f;

        private Rigidbody _rb;
        private Collider _col;
        private PhysicsMaterial _frictionlessMat;
        private PhysicsMaterial _originalMat;
        private Vector3 _currentSlideDirection;

        public float PushResistance => _pushResistance;
        public bool IsOnIce { get; private set; }
        public bool IsSlidingOnIce => IsOnIce && _rb.linearVelocity.magnitude > 0.5f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<Collider>();
            
            if (_col != null)
            {
                _originalMat = _col.sharedMaterial;
                _frictionlessMat = new PhysicsMaterial("IceFrictionless");
                _frictionlessMat.dynamicFriction = 0f;
                _frictionlessMat.staticFriction = 0f;
                // Combiner au minimum garantit que même si le sol a de la friction, le résultat sera 0
                _frictionlessMat.frictionCombine = PhysicsMaterialCombine.Minimum;
                _frictionlessMat.bounciness = 0f;
                // Combiner au minimum garantit qu'il n'y aura aucun rebond (même si le mur a du bounciness)
                _frictionlessMat.bounceCombine = PhysicsMaterialCombine.Minimum;
            }
            
            // On s'assure que le bloc est bien configuré pour glisser
            _rb.useGravity = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            
            // Verrouillage des rotations pour éviter qu'il ne bascule
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private Collider _lastHitCollider;

        private void FixedUpdate()
        {
            // Vérification plus robuste si le bloc touche le sol
            // On utilise le centre et les dimensions du Collider plutôt que le transform qui peut être mal placé au niveau du sol
            Vector3 center = _col != null ? _col.bounds.center : transform.position;
            float extentsY = _col != null ? _col.bounds.extents.y : (transform.localScale.y * 0.5f);
            
            float rayLength = extentsY + 0.2f; // On cherche jusqu'à 20cm sous la base du collider
            bool isGrounded = Physics.Raycast(center, Vector3.down, out RaycastHit groundHit, rayLength);

            if (isGrounded)
            {
                IsOnIce = groundHit.collider.CompareTag(_iceFloorTag);
                
                // Debug pour aider à comprendre ce qui est touché (affiché seulement si ça change pour éviter le spam)
                if (groundHit.collider != _lastHitCollider)
                {
                    Debug.Log($"[PushableBlock] Le Raycast touche : {groundHit.collider.name} (Tag: {groundHit.collider.tag}). IsOnIce: {IsOnIce}");
                    _lastHitCollider = groundHit.collider;
                }
            }
            else
            {
                IsOnIce = false;
                _lastHitCollider = null;
            }

            // Gestion de la friction pure et du damping
            if (IsOnIce)
            {
                _rb.linearDamping = 0f; // Aucune perte de vitesse dans l'air/glace
                if (_col != null) _col.material = _frictionlessMat;
            }
            else
            {
                _rb.linearDamping = isGrounded ? _drag : 0.05f;
                if (_col != null) _col.material = _originalMat;

                // Quand on n'est plus sur la glace ou qu'on s'arrête, on libère les axes
                if (isGrounded)
                {
                    _rb.constraints = RigidbodyConstraints.FreezeRotation;
                }
            }

            // (L'ancienne libération automatique des axes a été supprimée. 
            // On veut conserver le verrouillage de l'impact jusqu'à la prochaine poussée.)

            // Application d'une gravité personnalisée et blocage horizontal si on tombe
            if (!isGrounded)
            {
                if (_gravityScale != 1f)
                {
                    // On ajoute la différence de gravité pour atteindre le multiplicateur souhaité
                    _rb.AddForce(Physics.gravity * (_gravityScale - 1f), ForceMode.Acceleration);
                }

                // On annule la vélocité horizontale uniquement quand la chute est bien entamée
                // Cela permet au "Big Push" de finir de propulser le bloc au-dessus du vide
                Vector3 currentVel = _rb.linearVelocity;
                if (currentVel.y < -1f && (Mathf.Abs(currentVel.x) > 0.01f || Mathf.Abs(currentVel.z) > 0.01f))
                {
                    _rb.linearVelocity = new Vector3(0, currentVel.y, 0);
                }
            }
        }

        public void Push(Vector3 force)
        {
            // On applique la force uniquement sur les axes X et Z
            Vector3 pushForce = new Vector3(force.x, 0, force.z);
            
            // On stocke la direction initiale prévue pour pouvoir bien analyser les futurs impacts
            if (pushForce != Vector3.zero)
            {
                _currentSlideDirection = pushForce.normalized;
            }
            
            Debug.Log($"[PushableBlock] Push appelé ! Force globale demandée : {force}. Force X/Z appliquée : {pushForce}. IsOnIce = {IsOnIce}");

            if (IsOnIce)
            {
                // Trouver l'axe transversal global (celui sur lequel on ne veut pas bouger)
                Vector3 globalPushDir = pushForce.normalized;
                Vector3 globalTransverseDir = (Mathf.Abs(globalPushDir.x) > Mathf.Abs(globalPushDir.z)) 
                                              ? new Vector3(0, 0, 1) 
                                              : new Vector3(1, 0, 0);
                
                // Convertir cet axe transversal global en espace local
                Vector3 localTransverseDir = transform.InverseTransformDirection(globalTransverseDir);
                
                float absX = Mathf.Abs(localTransverseDir.x);
                float absY = Mathf.Abs(localTransverseDir.y);
                float absZ = Mathf.Abs(localTransverseDir.z);

                // Geler l'axe local correspondant à la translation transversale
                if (absX > absY && absX > absZ)
                {
                    _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX;
                    Debug.Log($"[PushableBlock] Verrouillage X (Transversal global: {globalTransverseDir}). Contraintes: {_rb.constraints}");
                }
                else if (absY > absX && absY > absZ)
                {
                    _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
                    Debug.Log($"[PushableBlock] Verrouillage Y (Transversal global: {globalTransverseDir}). Contraintes: {_rb.constraints}");
                }
                else
                {
                    _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
                    Debug.Log($"[PushableBlock] Verrouillage Z (Transversal global: {globalTransverseDir}). Contraintes: {_rb.constraints}");
                }
            }

            _rb.AddForce(pushForce, ForceMode.Impulse);
            Debug.Log($"[PushableBlock] Force Impulse ajoutée. Velocité actuelle du Rigidbody : {_rb.linearVelocity}");
        }

        public bool IsNearEdge(Vector3 direction)
        {
            // On tire un rayon vers le bas depuis un point précis du bloc
            Vector3 center = transform.position;
            Vector3 extent = transform.localScale * 0.5f;

            // Calcul du point de détection basé sur l'offset et la taille du bloc
            // Offset 1.0 = Bord avant | Offset 0.0 = Centre | Offset -1.0 = Bord arrière
            Vector3 edgeOrigin = center + direction * (extent.x * _edgeDetectionOffset);

            // On vérifie s'il y a du vide (ou une marche basse) sous ce point
            bool hit = Physics.Raycast(edgeOrigin, Vector3.down, out RaycastHit ledgeHit, extent.y + 1.0f);

            if (!hit) return true; // Vide total détecté sous le point

            // Si on a touché quelque chose, on vérifie si c'est significativement plus bas
            float currentGroundY = center.y - extent.y;
            return ledgeHit.point.y < currentGroundY - 0.1f; // Plus bas que 10cm
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Si on glisse sur la glace et qu'on touche un autre objet (pas le sol, pas le joueur)
            if (IsOnIce && IsSlidingOnIce && !collision.gameObject.CompareTag("Player"))
            {
                // On utilise la direction initialement prévue au lieu de _rb.linearVelocity.
                // Car au moment de OnCollisionEnter, le moteur physique a potentiellement DÉJÀ 
                // fait rebondir le cube et inversé sa vélocité, faussant le calcul !
                Vector3 currentDir = _currentSlideDirection;

                foreach (ContactPoint contact in collision.contacts)
                {
                    // Si on heurte une surface verticale (un mur ou un autre bloc)
                    if (Mathf.Abs(contact.normal.y) < 0.5f)
                    {
                        // On vérifie si c'est un impact frontal (qui doit nous arrêter) ou un frottement latéral (qui doit être ignoré)
                        // Dot == -1 : Choc parfaitement de face
                        // Dot == 0  : Frottement parfaitement parallèle sur le côté
                        float impactAngle = Vector3.Dot(currentDir, contact.normal);

                        // On ne s'arrête que si le choc est majoritairement de face (angle > 45 degrés)
                        if (impactAngle < -0.5f)
                        {
                            // On tue instantanément la vélocité pour éviter tout micro-rebond
                            _rb.linearVelocity = Vector3.zero;
                            
                            // IMPORTANT : On fige complètement le bloc sur le plan horizontal (X et Z).
                            // Cela empêche le moteur physique d'Unity de le repousser en arrière (séparation de pénétration).
                            // Ces axes seront déverrouillés par la méthode Push() au prochain coup du joueur.
                            _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
                            
                            Debug.Log($"[PushableBlock] Impact frontal ({impactAngle:F2}) avec {collision.gameObject.name}. Arrêt et verrouillage complet du bloc.");
                            break;
                        }
                        else
                        {
                            // C'est juste un frottement sur le bord dans un passage étroit, on l'ignore.
                        }
                    }
                }
            }
        }

        public float GetBigPushForce(Vector3 direction)
        {
            if (IsOnIce)
            {
                // Force d'impulsion pour atteindre la vitesse _icePushSpeed (F = m * v)
                // Comme le drag est quasi nul, le bloc va glisser très loin
                return _rb.mass * _icePushSpeed;
            }

            // On calcule la taille du bloc sur l'axe de poussée
            float sizeOnAxis = Mathf.Abs(direction.x * transform.localScale.x) + 
                               Mathf.Abs(direction.y * transform.localScale.y) + 
                               Mathf.Abs(direction.z * transform.localScale.z);

            // Formule : (Taille * Drag * Masse) pour contrer le frottement initial selon le poids + Offset
            // Cela garantit que le bloc a assez d'énergie pour franchir sa propre longueur peu importe sa masse
            return (sizeOnAxis * _drag * _rb.mass) + _bigPushOffset;
        }
    }
}
