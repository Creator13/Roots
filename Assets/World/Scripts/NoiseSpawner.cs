using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace World
{
    public class NoiseSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;

        [SerializeField] private float gridSpacing = 0.1f; // Distance between sample points
        [SerializeField] private float areaSize = 10f; // Size of the grid (both width and height)
        [SerializeField] private float noiseScale = 1f; // Scale of the Perlin noise
        [SerializeField] private float densityMultiplier = 1f; // Multiplier to control overall density

        private List<GameObject> objects = new List<GameObject>();
        
        private bool SampleNoise(float x, float z, Random random)
        {
            float noiseValue = Mathf.PerlinNoise(x * noiseScale, z * noiseScale);
            float threshold = noiseValue * densityMultiplier;
            return (float)random.NextDouble() < threshold;
        }

        [ContextMenu("Generate")]
        private void GenerateObjects()
        {
            RemoveChildren();

            Random random = new Random(0);

            for (float x = transform.position.x; x <= areaSize; x += gridSpacing)
            {
                for (float z = transform.position.z; z <= areaSize; z += gridSpacing)
                {
                    if (SampleNoise(x, z, random))
                    {
                        GameObject obj = Instantiate(prefab, new Vector3(x, 0, z), Quaternion.identity, transform);
                        objects.Add(obj);
                    }
                }
            }
        }

        private void RemoveChildren()
        {
            foreach (GameObject obj in objects)
            {
                DestroyImmediate(obj);
            }
        }
    }
}