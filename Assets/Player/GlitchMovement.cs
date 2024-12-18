using Roots;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Roots
{
    public class GlitchMovement : MonoBehaviour
    {
        [SerializeField] private Transform target;
        
        [SerializeField] private GlitchMovementSettings highEnergySettings;
        [SerializeField] private GlitchMovementSettings lowEnergySettings;

        private float energy;
        public float Energy
        {
            get => energy;
            set => energy = Mathf.Clamp01(value);
        }

        private Vector3 velocity = Vector3.zero;

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            
            // Interpolate settings
            float centerRandomization = Mathf.Lerp(lowEnergySettings.centerRandomization, highEnergySettings.centerRandomization, Energy);
            float pullForce = Mathf.Lerp(lowEnergySettings.pullForce, highEnergySettings.pullForce, Energy);
            float damping = Mathf.Lerp(lowEnergySettings.damping, highEnergySettings.damping, Energy);
            float burstChance = Mathf.Lerp(lowEnergySettings.burstChance, highEnergySettings.burstChance, Energy);
            float perlinSpeed = Mathf.Lerp(lowEnergySettings.perlinSpeed, highEnergySettings.perlinSpeed, Energy);
            float perlinThreshold = Mathf.Lerp(lowEnergySettings.perlinThreshold, highEnergySettings.perlinThreshold, Energy);

            var randomCenter = Random.insideUnitSphere * centerRandomization;
            Vector3 pullDirection = (randomCenter - target.localPosition).normalized;
            float distanceFromCenter = (randomCenter - target.localPosition).magnitude;
            Vector3 pull = pullDirection * (pullForce * distanceFromCenter);

            velocity += pull * deltaTime;
            velocity *= damping;

            if (Random.value < burstChance && Mathf.PerlinNoise1D((Time.time * perlinSpeed) % 500) > perlinThreshold)
            {
                ApplyRandomBurst();
            }

            // Apply velocity
            target.localPosition += velocity * deltaTime;

            // // Rotate
            // target.Rotate(target.up, deltaTime * settings.rotateSpeed);
            // if (velocity.sqrMagnitude > 0.01f)
            // {
            //     target.rotation *= Quaternion.LookRotation(Vector3.forward, velocity.normalized);
            // }
        }
        
        private void ApplyRandomBurst()
        {
            float outwardForce = Mathf.Lerp(lowEnergySettings.outwardForce, highEnergySettings.outwardForce, Energy);
            velocity += Random.insideUnitSphere * outwardForce;
        }
    }
}