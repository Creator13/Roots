using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Roots.World
{
    public class VegetationRootManager : MonoBehaviour
    {
        [SerializeField] private VegetationRoot placePrefab;

        private List<VegetationRoot> roots = new List<VegetationRoot>();

        public ReadOnlyCollection<VegetationRoot> GetAll()
        {
            return roots.AsReadOnly();
        }

        public void PlaceNew(Vector3 position)
        {
            VegetationRoot instance = Instantiate(placePrefab, position, Quaternion.identity, transform);
            instance.Initialize(1);
            roots.Add(instance);
        }
    }
}