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

        private Rigidbody _rb;

        public float PushResistance => _pushResistance;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            
            // On s'assure que le bloc est bien configuré pour glisser
            _rb.useGravity = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            
            // Verrouillage des rotations pour éviter qu'il ne bascule
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private void FixedUpdate()
        {
            // Vérification si le bloc touche le sol
            // On tire un rayon légèrement plus long que la demi-hauteur du bloc
            float rayLength = (transform.localScale.y * 0.5f) + 0.1f;
            bool isGrounded = Physics.Raycast(transform.position, Vector3.down, rayLength);

            // On n'applique le drag que si on est au sol
            // Sinon (en chute), on met un drag quasi-nul pour une chute naturelle
            _rb.linearDamping = isGrounded ? _drag : 0.05f;

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
            _rb.AddForce(pushForce, ForceMode.Impulse);
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

        public float GetBigPushForce(Vector3 direction)
        {
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
