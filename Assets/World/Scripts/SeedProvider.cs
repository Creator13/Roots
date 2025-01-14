using System;
using Unity.Mathematics;
using UnityEngine;

namespace Roots.World
{
    [CreateAssetMenu(fileName = "SeedProvider", menuName = "Roots/Seed provider", order = 200)]
    public class SeedProvider : ScriptableObject
    {
        [field: SerializeField] public int Seed { get; private set; } = 42;
        [SerializeField] private bool randomizeOnAssetAwake = false;

        private void Awake()
        {
            Initialize();
        }

        private void OnValidate()
        {
            Initialize();
        }

        private void Initialize()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            if (args.Length > 1 && int.TryParse(args[1], out int cmdSeed))
            {
                Seed = cmdSeed;
                Debug.Log($"Using seed from command line argument: {Seed}");
            }
            else if (randomizeOnAssetAwake)
            {
                Seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                Debug.Log($"Using random seed: {Seed}");
            }
            else
            {
                Debug.Log($"Using predefined seed: {Seed}");
            }
        }
        
        public uint SeedAsUint()
        {
            return math.asuint(Seed);
        }
    }
}