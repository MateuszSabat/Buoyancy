using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhysWater
{
    public class Water : MonoBehaviour
    {
        public static EndlessOcean ocean;

        //grid coord
        public int xPos;
        public int yPos;

        private MeshFilter meshFilter;
        private Mesh mesh;
        private Vector3[] vertices;

        /// <summary>
        /// Update every vertices of chunk using sin wave at time t
        /// </summary>
        public void UpdateWaterMesh(float t)
        {
            if (ocean.shadingMode == EndlessOcean.ShadingMode.Flat)
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i].y = GetHeight(transform.position.x + vertices[i].x, transform.position.z + vertices[i].z, t);
                }
                mesh.vertices = vertices;
                mesh.RecalculateNormals();
            }
            else
            {
                Vector3[] normals = new Vector3[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    SurfData data = GetHeghtAndNormal(transform.position.x + vertices[i].x, transform.position.z + vertices[i].z, t);
                    vertices[i].y = data.height;
                    normals[i] = data.normal;
                }
                mesh.vertices = vertices;
                mesh.normals = normals;
            }
        }
        /// <summary>
        /// Returns height of surface of the water at coord: (x, y) and time t
        /// </summary>
        public static float GetHeight(float x, float y, float t, bool addNoise = true)
        {
            float p = ocean.frequency * t + x * ocean.inverseLength.x + y * ocean.inverseLength.y;

            float h = Mathf.Sin(p) * ocean.amplitude;

            if (addNoise)
                h += Mathf.PerlinNoise(x + ocean.noiseFrequency * t, y + .5f + ocean.noiseFrequency * t) * ocean.noiseStrength;

            return h;
        }
        /// <summary>
        /// return height and normal of surface of the water at coord: (x, y) and time t
        /// </summary>
        public static SurfData GetHeghtAndNormal(float x, float y, float t, bool addNoise = true)
        {
            float p = ocean.frequency * t + x * ocean.inverseLength.x + y * ocean.inverseLength.y;

            float h = Mathf.Sin(p) * ocean.amplitude;

            float cos = Mathf.Cos(p) * ocean.amplitude;

            float hx = cos * ocean.inverseLength.x; // dh/dx
            float hy = cos * ocean.inverseLength.y; // dh/dy

            if(addNoise)
                h+= Mathf.PerlinNoise(x + ocean.noiseFrequency * t, y + .5f + ocean.noiseFrequency * t) * ocean.noiseStrength;

            Vector3 n = new Vector3(hx, hy, -1).normalized;

            return new SurfData(h, n);
        }


        public void GenerateMesh()
        {
            if (ocean == null)
                ocean = EndlessOcean.instance;

            int gridSize = (int)(ocean.chunkSize / ocean.vertexDistance);
            int arraySize = 2 * gridSize + 1;
            int trisIndex = 0;
            int vertIndex = 0;

            meshFilter = GetComponent<MeshFilter>();

            Vector3[] newVertices = new Vector3[0];
            int[] newTriangles = new int[0];

            void AddTriangle(int a, int b, int c)
            {
                newTriangles[trisIndex] = a;
                newTriangles[trisIndex + 1] = b;
                newTriangles[trisIndex + 2] = c;
                trisIndex += 3;
            }

            if (ocean.shadingMode == EndlessOcean.ShadingMode.Smooth)
            {
                newVertices = new Vector3[arraySize * arraySize];
                newTriangles = new int[(arraySize - 1) * (arraySize - 1) * 6];

                for (int x = -gridSize; x <= gridSize; x++)
                    for (int y = -gridSize; y <= gridSize; y++)
                    {
                        newVertices[vertIndex] = new Vector3(x * ocean.vertexDistance, 0, y * ocean.vertexDistance);
                        if (x != gridSize && y != gridSize)
                        {
                            AddTriangle(vertIndex, vertIndex + 1, vertIndex + arraySize + 1);
                            AddTriangle(vertIndex, vertIndex + arraySize + 1, vertIndex + arraySize);
                        }
                        vertIndex++;
                    }
            }
            else
            {
                newVertices = new Vector3[(arraySize - 1) * (arraySize - 1) * 6];
                newTriangles = new int[(arraySize - 1) * (arraySize - 1) * 6];
                for (int x = -gridSize; x < gridSize; x++)
                    for (int y = -gridSize; y < gridSize; y++)
                    {
                        newVertices[vertIndex] = new Vector3(x * ocean.vertexDistance, 0, y * ocean.vertexDistance);
                        newVertices[vertIndex + 1] = new Vector3(x * ocean.vertexDistance, 0, (y + 1) * ocean.vertexDistance);
                        newVertices[vertIndex + 2] = new Vector3((x + 1) * ocean.vertexDistance, 0, y * ocean.vertexDistance);
                        AddTriangle(vertIndex, vertIndex + 1, vertIndex + 2);
                        newVertices[vertIndex + 3] = new Vector3((x + 1) * ocean.vertexDistance, 0, y * ocean.vertexDistance);
                        newVertices[vertIndex + 4] = new Vector3(x * ocean.vertexDistance, 0, (y + 1) * ocean.vertexDistance);
                        newVertices[vertIndex + 5] = new Vector3((x + 1) * ocean.vertexDistance, 0, (y + 1) * ocean.vertexDistance);
                        AddTriangle(vertIndex + 3, vertIndex + 4, vertIndex + 5);
                        vertIndex += 6;
                    }
            }
            Vector2[] newUV = new Vector2[newVertices.Length];
            float uvMultiplier = gridSize * ocean.vertexDistance;
            for(int i = 0; i<newVertices.Length; i++)
            {
                newUV[i] = new Vector2(newVertices[i].x + uvMultiplier, newVertices[i].z + uvMultiplier) / (2 * uvMultiplier);
            }

            Mesh newMesh = new Mesh
            {
                vertices = newVertices,
                triangles = newTriangles,
                uv = newUV
            };
            meshFilter.sharedMesh = newMesh;

            mesh = meshFilter.sharedMesh;
            vertices = newVertices;
            mesh.RecalculateNormals();
        }
    }

    public class SurfData
    {
        public float height;
        public Vector3 normal;

        public SurfData(float h, Vector3 n)
        {
            height = h;
            normal = n;
        }
    }
}
