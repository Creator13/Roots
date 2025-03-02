using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Roots.World
{
    public class ChunkVegetationManager : MonoBehaviour
    {
        private List<Transform> objects;

        [SerializeField] private GameObject prefab;

        public void SetPrefab(GameObject prefab)
        {
            this.prefab = prefab;
        }
        
        public void Initialize(int count)
        {
            objects = new List<Transform>(count);
            for (int i = 0; i < count; i++)
            {
                AddObject();
            }
        }
        
        public void SetVegetation(IList<float3> positions)
        {
            // Grow list if necessary
            if (positions.Count > objects.Count)
            {
                for (int i = 0; i < objects.Count - positions.Count; i++)
                {
                    AddObject();
                }
            }

            // Set positions of all vegetation instances
            for (int i = 0; i < positions.Count; i++)
            {
                objects[i].localPosition = positions[i];
                objects[i].gameObject.SetActive(true);
            }

            // Disable all unnecessary objects
            if (objects.Count > positions.Count)
            {
                for (int i = positions.Count; i < objects.Count; i++)
                {
                    objects[i].gameObject.SetActive(false);
                }
            }
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddObject()
        {
            GameObject obj = Instantiate(prefab, this.transform);
            objects.Add(obj.transform);
            obj.SetActive(false);
        }
    }
}