using System;
using UnityEngine;

namespace Roots.World
{
    public abstract class PointProvider : MonoBehaviour
    {
        public event Action PointDataChanged;

        public abstract Vector3[] GetPointData();

        protected void OnPointDataChanged()
        {
            PointDataChanged?.Invoke();
        }

        public abstract Bounds GetBounds();
    }
}