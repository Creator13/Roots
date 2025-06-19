using System;
using UnityEngine;

namespace Roots.World.Chunking
{
    [Serializable]
    public struct Root
    {
        public Vector2 pos;
        public float radius;
    }

    public class ChunkVegetationMap : MonoBehaviour
    {
        [SerializeField] private ComputeShader shader;
        [SerializeField] private int texSize = 512;
        [SerializeField] private Root[] roots;

        private GraphicsBuffer inBuffer;
        private GraphicsBuffer outBuffer;
        [SerializeField] private RenderTexture renderTexture;

        [ContextMenu("Do the thing")]
        public void UpdateBuffers()
        {
            inBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, roots.Length, sizeof(float) * 3);
            inBuffer.SetData(roots);

            outBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, texSize * texSize, sizeof(uint));
            outBuffer.SetData(new uint[texSize * texSize]);

            renderTexture = new RenderTexture(texSize, texSize, 0, RenderTextureFormat.RFloat);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();

            int kernel1 = shader.FindKernel("CSMain");

            shader.SetBuffer(kernel1, "roots", inBuffer);
            shader.SetBuffer(kernel1, "density_map", outBuffer);
            shader.SetInt("num_roots", roots.Length);
            shader.SetInts("texture_size", texSize, texSize);
            shader.Dispatch(kernel1, roots.Length, 1, 1);

            int kernel2 = shader.FindKernel("CopyToTexture");
            shader.SetBuffer(kernel2, "density_map", outBuffer);
            shader.SetTexture(kernel2, "density_texture", renderTexture);
            shader.Dispatch(kernel2, texSize / 8, texSize / 8, 1);
        }
    }
}