using UnityEngine;

namespace Roots
{
    [CreateAssetMenu(fileName = "NewGlitchMovementSettings", menuName = "Roots/Glitch Movement Settings", order = 0)]
    public class GlitchMovementSettings : ScriptableObject
    {
        [field: SerializeField] public float outwardForce { get; private set; } = 10f;
        [field: SerializeField] public float pullForce { get; private set; } = 20f;
        [field: SerializeField] public float damping { get; private set; } = 0.98f;
        [field: SerializeField, Range(0, 1)] public float burstChance { get; private set; } = .2f;
        [field: SerializeField, Range(0, 1)] public float perlinThreshold { get; private set; } = .5f;
        [field: SerializeField] public float perlinSpeed { get; private set; } = 1;
        [field: SerializeField] public float centerRandomization { get; private set; } = .3f;
        [field: SerializeField] public float rotateSpeed { get; private set; } = 1;
    }
}