using UnityEngine;

namespace Roots.World
{
    [CreateAssetMenu(fileName = "New Growth Parameters", menuName = "Roots/Growth Parameters")]
    public class GrowthParameters : ScriptableObject
    {
        [field: SerializeField] public float minDistance { get; private set; }
        [field: SerializeField] public float maxDistance { get; private set; }
        [field: SerializeField] public Mesh[] growthStageMeshes { get; private set; }
        [field: SerializeField] public float scaleFactor { get; private set; }
    }
}