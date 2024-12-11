using System.Collections.Generic;
using UnityEngine;

namespace World
{
    public class ChunkLoader : MonoBehaviour
    {
        [SerializeField] private Chunk chunkPrefab;
        [SerializeField] private Transform treePrefab;

        [Space]
        [SerializeField] private float chunkSize = 10;
        [SerializeField] private int loadRadius = 3;

        [Space]
        [SerializeField] private Transform player;
        [SerializeField] private int playerChunkX;
        [SerializeField] private int playerChunkZ;

        [Space]
        [SerializeField] private float treeDensity;
        [SerializeField] private PathNoiseGenerator noiseGenerator;

        private Vector3 playerPosition;
        private Dictionary<Vector2Int, Chunk> loadedChunks;

        private bool playerChunkChanged;

        private void Awake()
        {
            loadedChunks = new Dictionary<Vector2Int, Chunk>();
        }

        private void Start()
        {
            UpdateVisibleChunks();
        }

        private void Update()
        {
            playerChunkChanged = false;
            playerPosition = player.transform.position;

            UpdateCurrentChunk();

            if (playerChunkChanged) UpdateVisibleChunks();
        }

        private void UpdateVisibleChunks()
        {
            Dictionary<Vector2Int, Chunk> newChunks = new();

            // Create new chunks
            for (int x = -loadRadius; x < loadRadius + 1; x++)
            {
                for (int z = -loadRadius; z < loadRadius + 1; z++)
                {
                    Vector2Int key = new Vector2Int(x + playerChunkX, z + playerChunkZ);
                    if (loadedChunks.TryGetValue(key, out var chunk))
                    {
                        newChunks.Add(key, chunk);
                    }
                    else
                    {
                        newChunks.Add(key, CreateChunk(x + playerChunkX, z + playerChunkZ));
                    }
                }
            }

            // Invalidate and remove old chunks
            foreach (var (key, chunk) in loadedChunks)
            {
                if (!newChunks.ContainsKey(key))
                {
                    Destroy(chunk.gameObject);
                }
            }

            // Update loaded chunks
            loadedChunks = newChunks;
        }

        private void UpdateCurrentChunk()
        {
            int originalChunkX = playerChunkX;
            int originalChunkZ = playerChunkZ;

            playerChunkX = Mathf.FloorToInt(playerPosition.x / chunkSize);
            playerChunkZ = Mathf.FloorToInt(playerPosition.z / chunkSize);

            if (playerChunkX != originalChunkX || playerChunkZ != originalChunkZ)
            {
                playerChunkChanged = true;
            }
        }

        private Chunk CreateChunk(int x, int z)
        {
            Chunk chunk = Instantiate(chunkPrefab, CalculateChunkCenter(x, z), Quaternion.identity, transform);
            chunk.gameObject.name = $"Chunk ({x}, {z})";
            chunk.LoadAt(x, z);
            SpawnTrees(chunk.transform);
            return chunk;
        }

        private void SpawnTrees(Transform chunkTransform)
        {
            float targetTreesPerChunk = chunkSize * chunkSize * treeDensity;
            for (int i = 0; i < targetTreesPerChunk; i++)
            {
                float x = Random.Range(0, chunkSize) - chunkSize * .5f;
                float z = Random.Range(0, chunkSize) - chunkSize * .5f;
                float noise = noiseGenerator.GetNoise(chunkTransform.position.x + x, chunkTransform.position.z + z);
                if (Random.value < noise)
                {
                    Transform treeInstance = Instantiate(treePrefab, chunkTransform, true);
                    treeInstance.SetLocalPositionAndRotation(new Vector3(x, 0, z), Quaternion.Euler(0, Random.Range(-180, 180), 0));
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(CalculateChunkCenter(playerChunkX, playerChunkZ), new Vector3(chunkSize, chunkSize, chunkSize));

            if (loadedChunks != null)
            {
                Gizmos.color = Color.green;
                foreach (var pos in loadedChunks.Keys)
                {
                    if (pos.x == playerChunkX && pos.y == playerChunkZ) continue;

                    Gizmos.DrawWireCube(CalculateChunkCenter(pos.x, pos.y), new Vector3(chunkSize, chunkSize, chunkSize));
                }
            }
        }

        private Vector3 CalculateChunkCenter(int x, int z)
        {
            return new Vector3(x * chunkSize + .5f * chunkSize, 0, z * chunkSize + .5f * chunkSize);
        }
    }
}