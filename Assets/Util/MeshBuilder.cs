using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Roots.Util
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

        public Vertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            this.position = position;
            this.normal = normal;
            this.uv = uv;
        }
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

        // public void TriangulateCircle(List<Vertex> circle, FaceDirection dir)
        // {
        //     // Create center point
        //     // TODO other triangulation methods don't necessarily need a center point
        //     var center = new Vector3(0, circle[0].position.y, 0);
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

        public int CreateCircle(Vector3 center, float radius, int segments, Vector3 normal, bool fill = false)
        {
            Assert.IsTrue(segments > 2, "Cannot create circle with less than three vertices");

            Vector3[] positions = new Vector3[segments];

            float angleStep = math.PI2 / segments;
            Quaternion rot = Quaternion.FromToRotation(Vector3.back, normal);

            for (int i = 0; i < segments; i++)
            {
                Vector3 pos = Vector3.zero;
                math.sincos(angleStep * i, out pos.y, out pos.x);

                pos *= radius;
                pos = rot * pos;
                pos += center;

                positions[i] = pos;
            }

            int startIndex = vertices.Count;
            for (int i = 0; i < positions.Length; i++)
            {
                AddVertex(positions[i], normal, Vector2.zero);
            }

            if (fill)
            {
                // Only add a single triangle if the shape is a triangle
                if (segments == 3)
                {
                    AddTriangle(startIndex, startIndex + 1, startIndex + 2, FaceDirection.CCW);
                }
                // Else: guaranteed that segments > 3 thanks to assertion at function start
                else 
                {
                    // TODO: smarter triangulation
                    int centerIndex = AddVertex(center, normal, Vector2.zero);
                    // Regular fan triangles that connect edge vertices n and n + 1
                    for (int i = 0; i < segments - 1; i++)
                    {
                        int vIndex1 = startIndex + i;
                        int vIndex2 = startIndex + i + 1;
                        AddTriangle(vIndex1, vIndex2, centerIndex, FaceDirection.CCW);
                    }

                    // Final fan triangle that connects edge vertices n_max and 0
                    AddTriangle(startIndex + segments - 1, startIndex, centerIndex, FaceDirection.CCW);
                }
            }

            return startIndex;
        }

        public int AddVertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            vertices.Add(position);
            uvs.Add(uv);
            normals.Add(normal);
            return vertices.Count - 1;
        }

        public int AddVertex(Vertex vertex)
        {
            return AddVertex(vertex.position, vertex.normal, vertex.uv);
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

        public void AddQuadNew(Vertex v1, Vertex v2, Vertex v3, Vertex v4)
        {
            var vertexIndex = vertices.Count;
            vertices.Add(v1.position);
            vertices.Add(v2.position);
            vertices.Add(v3.position);
            vertices.Add(v4.position);
            
            normals.Add(v1.normal);
            normals.Add(v2.normal);
            normals.Add(v3.normal);
            normals.Add(v4.normal);
            
            uvs.Add(v1.uv);
            uvs.Add(v2.uv);
            uvs.Add(v3.uv);
            uvs.Add(v4.uv);
            
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);
            triangles.Add(vertexIndex);
        }
    }
}