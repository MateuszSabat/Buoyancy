using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhysWater
{
    public class EndlessOcean : MonoBehaviour
    {
        public static EndlessOcean instance;
        private void Awake()
        {
            if (instance == null)
                instance = this;
            else
                Destroy(this);
        }
        public enum ShadingMode { Smooth, Flat }

        public ShadingMode shadingMode;

        [Tooltip("Transform that is observed by mainCamera. The one that should be always in center of the grid of water chunks")]
        public Transform observer;

        [Tooltip("The number of chunks between center and bounds of the grid")]
        public int size;
        public GameObject chunkPrefab;

        [Tooltip("Size of one chunk. It must be a multiple of vertexDistance")]
        public float chunkSize;
        [Tooltip("Distance between adjacent vertices of the mesh. It must be a divasor of chunkSize")]
        public float vertexDistance;

        public float amplitude;
        [Tooltip("It is vector to make it possible to set direction of the waves")]
        public Vector2 inverseLength;
        public float frequency;

        public float noiseStrength;
        public float noiseFrequency;

        private Water[,] chunks;

        void Start()
        {
            //Set Camera Depth Texture Mode
            Camera.main.depthTextureMode = DepthTextureMode.Depth;

            //Generate grid of water chunks
            int gridSize = 2 * size + 1;
            chunks = new Water[gridSize, gridSize];
            for (int x = -size; x <= size; x++)
                for (int y = -size; y <= size; y++)
                {
                    chunks[size + x, size + y] = Instantiate(chunkPrefab, new Vector3(x * (chunkSize * 2), 0, y * (chunkSize * 2)), Quaternion.identity).GetComponent<Water>();
                    chunks[size + x, size + y].transform.parent = transform;
                    chunks[size + x, size + y].xPos = x;
                    chunks[size + x, size + y].yPos = y;
                    chunks[size + x, size + y].GenerateMesh();
                }

            StartCoroutine("UpdateOceanMesh");
        }
        void Update()
        {
            UpdateChunkPos();
        }

        /// <summary>
        /// Translates bound chunks if observer is away from center of the grid
        /// </summary>
        void UpdateChunkPos()
        {
            // get coord of observer in grid space
            int x = Mathf.RoundToInt(observer.position.x / (2 * chunkSize));
            int y = Mathf.RoundToInt(observer.position.z / (2 * chunkSize));

            //Relocate bound chunks and update grid
            if (x > chunks[size, size].xPos)
            {
                Water[] c = new Water[chunks.GetLength(0)];
                float newX = (x + size) * (chunkSize * 2);
                //Relocate
                for (int i = 0; i < chunks.GetLength(1); i++)
                {
                    c[i] = chunks[0, i];
                    chunks[0, i].transform.position = new Vector3(newX, 0, chunks[0, i].transform.position.z);
                    chunks[0, i].xPos = x + size;
                }
                //Update grid
                for (int xi = 0; xi < chunks.GetLength(0) - 1; xi++)
                    for (int yi = 0; yi < chunks.GetLength(1); yi++)
                        chunks[xi, yi] = chunks[xi + 1, yi];
                for (int yi = 0; yi < chunks.GetLength(1); yi++)
                    chunks[chunks.GetLength(0) - 1, yi] = c[yi];

            }
            else if (x < chunks[size, size].xPos)
            {
                Water[] c = new Water[chunks.GetLength(0)];
                float newX = (x - size) * (chunkSize * 2);
                int oldX = chunks.GetLength(0) - 1;
                //Relocate
                for (int i = 0; i < chunks.GetLength(0); i++)
                {
                    c[i] = chunks[oldX, i];
                    chunks[oldX, i].transform.position = new Vector3(newX, 0, chunks[oldX, i].transform.position.z);
                    chunks[oldX, i].xPos = x - size;
                }
                //Update grid
                for (int xi = chunks.GetLength(0) - 1; xi > 0; xi--)
                    for (int yi = 0; yi < chunks.GetLength(1); yi++)
                        chunks[xi, yi] = chunks[xi - 1, yi];
                for (int yi = 0; yi < chunks.GetLength(1); yi++)
                    chunks[0, yi] = c[yi];
            }

            if (y > chunks[size, size].yPos)
            {
                Water[] c = new Water[chunks.GetLength(1)];
                float newY = (y + size) * (chunkSize * 2);
                //Relocate
                for (int i = 0; i < chunks.GetLength(0); i++)
                {
                    c[i] = chunks[i, 0];
                    chunks[i, 0].transform.position = new Vector3(chunks[i, 0].transform.position.x, 0, newY);
                    chunks[i, 0].yPos = y + size;
                }
                //Update grid
                for (int xi = 0; xi < chunks.GetLength(0); xi++)
                    for (int yi = 0; yi < chunks.GetLength(1) - 1; yi++)
                        chunks[xi, yi] = chunks[xi, yi + 1];
                for (int xi = 0; xi < chunks.GetLength(0); xi++)
                    chunks[xi, chunks.GetLength(1) - 1] = c[xi];
            }
            else if (y < chunks[size, size].yPos)
            {
                Water[] c = new Water[chunks.GetLength(1)];
                float newY = (y - size) * (chunkSize * 2);
                int oldY = chunks.GetLength(1) - 1;
                //Relocate
                for (int i = 0; i < chunks.GetLength(0); i++)
                {
                    c[i] = chunks[i, oldY];
                    chunks[i, oldY].transform.position = new Vector3(chunks[i, oldY].transform.position.x, 0, newY);
                    chunks[i, oldY].yPos = y - size;
                }
                //Update grid
                for (int xi = 0; xi < chunks.GetLength(0); xi++)
                    for (int yi = chunks.GetLength(1) - 1; yi > 0; yi--)
                        chunks[xi, yi] = chunks[xi, yi - 1];
                for (int xi = 0; xi < chunks.GetLength(0); xi++)
                    chunks[xi, 0] = c[xi];
            }
        }

        /// <summary>
        /// Update chunk waving
        /// </summary>
        IEnumerator UpdateOceanMesh()
        {
            while (true)
            {
                float time = Time.time;
                foreach (Water w in chunks)
                {
                    w.UpdateWaterMesh(time);
                }
                //yield return new WaitForSeconds(Time.deltaTime * 3f);
                yield return null;
            }
        }
    }
}
