using System;
using System.IO;
using Roots.Util;
using Unity.Collections;
using UnityEngine;

namespace Roots
{
    public class Tracker : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float interval = 1;

        private Timer timer;
        private NativeList<Vector3> positions;
        private int count;

        private void Start()
        {
            timer = new Timer(interval, true);
            positions = new NativeList<Vector3>(2048, Allocator.Persistent);
        }

        private void OnDestroy()
        {
            positions.Dispose();
        }

        private void Update()
        {
            TryAdd();

            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.S))
            {
                Save();
            }
        }

        private void TryAdd()
        {
            if (!timer.CheckTime()) return;
            if (positions.Length > 0 && (positions[^1] - target.position).sqrMagnitude < 1) return;
            
            positions.Add(target.position);
            count++;
        }

        private void Save()
        {
            DateTime now= DateTime.Now;
            string path = Application.dataPath + $"TRACK {now.ToShortTimeString()}.json";
            var json = JsonUtility.ToJson(positions, true);
            if (!File.Exists(path))
            {
                File.WriteAllText(path, json);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!positions.IsCreated || positions.Length < 2) return;

            Gizmos.color = Color.orange;
            Gizmos.DrawLineStrip(positions.AsReadOnly().AsReadOnlySpan()[..count], false);
        }
    }
}