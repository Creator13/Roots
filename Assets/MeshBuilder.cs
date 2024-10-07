using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public enum FaceDirection
    {
        CW,
        CCW
    }

    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    }

    public class MeshBuilder
    {
        private readonly List<Vector3> vertices;
        private readonly List<Vector3> normals;
        private readonly List<Vector2> uvs;

        private readonly List<int> triangles = new();

        public MeshBuilder(int capacity = 0)
        {
            vertices = capacity > 0 ? new List<Vector3>(capacity) : new List<Vector3>();
            normals = capacity > 0 ? new List<Vector3>(capacity) : new List<Vector3>();
            uvs = capacity > 0 ? new List<Vector2>(capacity) : new List<Vector2>();
        }

        public Mesh GetMesh(string name = "")
        {
            var mesh = new Mesh
            {
                name = name,
                vertices = vertices.ToArray(),
                normals = normals.ToArray(),
                uv = uvs.ToArray(),
                triangles = triangles.ToArray()
            };

            return mesh;
        }

        // public void TriangulateCircle(List<Vector3> circle, FaceDirection dir)
        // {
        //     // Create center point
        //     // TODO other triangulation methods don't necessarily need a center point
        //     var center = new Vector3(0, circle[0].y, 0);
        //     var centerIndex = vertices.Count;
        //     vertices.Add(center);
        //
        //     var n = circle.Count;
        //
        //     var vertexIndices = AddVertices(circle);
        //
        //     // Add triangles
        //     for (var i = 0; i < n; i++)
        //     {
        //         AddTriangle(centerIndex, vertexIndices[i], vertexIndices[(i + 1) % n], dir);
        //     }
        // }
        //
        // public void BridgeEdgeLoopsSmooth(List<Vector3> c1, List<Vector3> c2)
        // {
        //     if (c1.Count != c2.Count)
        //     {
        //         throw new System.ArgumentException("Circle segment count must be equal");
        //     }
        //
        //     var n = c1.Count - 1;
        //
        //     var c1Verts = AddVertices(c1);
        //     var c2Verts = AddVertices(c2);
        //
        //     for (var i = 0; i < n; i++)
        //     {
        //         AddQuad(c1Verts[i], c2Verts[i], c2Verts[i + 1], c1Verts[i + 1]);
        //     }
        //
        //     AddQuad(c1Verts[n], c2Verts[n], c2Verts[0], c1Verts[0]);
        // }

        public void BridgeEdgeLoops(List<Vector3> c1, List<Vector3> c2)
        {
            if (c1.Count != c2.Count)
            {
                throw new System.ArgumentException("Circle segment count must be equal");
            }

            var n = c1.Count - 1;

            for (var i = 0; i < n; i++)
            {
                AddQuadNew(c1[i], c2[i], c2[i + 1], c1[i + 1]);
            }

            AddQuadNew(c1[n], c2[n], c2[0], c1[0]);
        }

        public List<int> AddVertices(ICollection<Vertex> original)
        {
            var indices = new List<int>(original.Count);

            foreach (var i in original)
            {
                indices.Add(vertices.Count);
                vertices.Add(i.position);
                normals.Add(i.normal);
                uvs.Add(i.uv);
            }

            return indices;
        }

        public int AddVertex(Vertex vertex)
        {
            vertices.Add(vertex.position);
            uvs.Add(vertex.uv);
            normals.Add(vertex.normal);
            return vertices.Count - 1;
        }

        public void AddTriangle(int v1, int v2, int v3, FaceDirection dir = FaceDirection.CW)
        {
            triangles.Add(v1);
            if (dir == FaceDirection.CW)
            {
                triangles.Add(v2);
                triangles.Add(v3);
            }
            else
            {
                triangles.Add(v3);
                triangles.Add(v2);
            }
        }

        public void AddTriangleNew(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            var vertexIndex = vertices.Count;
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
        }

        public void AddQuad(int v1, int v2, int v3, int v4, FaceDirection dir = FaceDirection.CW)
        {
            AddTriangle(v1, v2, v3, dir);
            AddTriangle(v3, v4, v1, dir);
        }

        public void AddQuadNew(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            var vertexIndex = vertices.Count;
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            vertices.Add(v4);
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);
            triangles.Add(vertexIndex);
        }
    }
}