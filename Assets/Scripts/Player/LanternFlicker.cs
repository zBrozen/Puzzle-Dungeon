using UnityEngine;

namespace PuzzleDungeon.Effects
{
    [RequireComponent(typeof(Light))]
    public class LanternFlicker : MonoBehaviour
    {
        [Header("Intensity Settings")]
        [SerializeField] private float _minIntensity = 0.8f;
        [SerializeField] private float _maxIntensity = 1.2f;
        [SerializeField] private float _intensitySmoothing = 0.1f;

        [Header("Range Settings")]
        [SerializeField] private float _minRange = 4.5f;
        [SerializeField] private float _maxRange = 5.5f;
        [SerializeField] private float _rangeSmoothing = 0.2f;

        private Light _light;
        private float _targetIntensity;
        private float _targetRange;
        private float _intensityVelocity;
        private float _rangeVelocity;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _targetIntensity = _light.intensity;
            _targetRange = _light.range;
        }

        private void Update()
        {
            // Choix de nouvelles cibles aléatoires de temps en temps
            if (Random.value < 0.1f)
            {
                _targetIntensity = Random.Range(_minIntensity, _maxIntensity);
                _targetRange = Random.Range(_minRange, _maxRange);
            }

            // Lissage pour éviter que ça ne clignote trop violemment
            _light.intensity = Mathf.SmoothDamp(_light.intensity, _targetIntensity, ref _intensityVelocity, _intensitySmoothing);
            _light.range = Mathf.SmoothDamp(_light.range, _targetRange, ref _rangeVelocity, _rangeSmoothing);
        }
    }
}
