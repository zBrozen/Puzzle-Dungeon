using UnityEngine;

namespace PuzzleDungeon.Interactions
{
    [RequireComponent(typeof(Rigidbody))]
    public class PushableBlock : MonoBehaviour
    {
        [SerializeField] private float _drag = 10f;

        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            
            // On s'assure que le bloc est bien configuré pour glisser
            _rb.useGravity = true;
            _rb.linearDamping = _drag;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            
            // Verrouillage des rotations pour éviter qu'il ne bascule
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        public void Push(Vector3 force)
        {
            // On applique la force uniquement sur les axes X et Z
            Vector3 pushForce = new Vector3(force.x, 0, force.z);
            _rb.AddForce(pushForce, ForceMode.Impulse);
        }
    }
}
