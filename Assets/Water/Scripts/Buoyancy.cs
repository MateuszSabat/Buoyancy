using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace PhysWater
{
    //script based on https://gamasutra.com/view/news/237528/Water_interaction_model_for_boats_in_video_games.php
    public class Buoyancy : MonoBehaviour
    {
        public float rho;

        public enum Accuracy
        {
            // number of triangles      point of application of force
            High,   //      high                          exact point
            Medium, //     medium                           center
            Low     //      low                             center
        }
        [Space(5f)]
        [Tooltip("Accuacy when cutting mesh under water into triangles and choosing their point of application of the force")]
        public Accuracy accuracy;


        private Vector3[] vertices;
        private int[] triangles;
        /// <summary>
        /// to avoid calculating depth for some vertices shared by some triangles more than once
        /// (waterHeight - vertex.y)    
        /// if positiv vertex under water
        /// </summary>
        private float[] verticesDepth;
        private Vector3[] verticesInGlobalSpace;

        /// <summary>
        /// max distance between vertex of submerged mesh along velocity vector
        /// </summary>
        float lengthInVelocityDirection;

        [Space(5f)]
        public float pressureDragCoefficient;


        private Rigidbody rb;

        // list that is updated in coroutine
        private List<TriangleData> underwaterTrianglesCoroutine = new List<TriangleData>();
        // list that is used in FixedUpdate to calculate force
        private List<TriangleData> underwaterTrianglesForce = new List<TriangleData>();


        private void Start()
        {
            Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
            vertices = mesh.vertices;
            triangles = mesh.triangles;
            verticesDepth = new float[vertices.Length];
            verticesInGlobalSpace = new Vector3[vertices.Length];

            rb = GetComponent<Rigidbody>();

            StartCoroutine("MeshUpdateCoroutine");
        }

        private void FixedUpdate()
        {
            float pVCoefficient = pressureDragCoefficient * rb.velocity.sqrMagnitude;

            foreach (TriangleData t in underwaterTrianglesForce)
            {
                //Hydrostatic

                rb.AddForceAtPosition(t.HydrostaticForce(rho), t.buoyanceCenter);

                //Hydrodynamic
                t.RecalculateDynamicVariables(rb);

                rb.AddForceAtPosition(t.PressureDrag() * pVCoefficient, t.center);
            }
            //rb.AddForce(-pressureArea * (pressureDragCoefficient * speed) * rb.velocity);
        }


        #region underwaterMeshUpdating
        IEnumerator MeshUpdateCoroutine()
        {
            //wait because first frames has long delta time
            yield return new WaitForEndOfFrame();
            UpdateUnderWaterMesh(Time.time);
            yield return new WaitForEndOfFrame();

            while (true)
            {
                UpdateUnderWaterMesh(Time.time);
                yield return null;
            }
        }

        void UpdateUnderWaterMesh(float time)
        {
            Vector3 vSurf = rb.velocity.normalized;
            float d1 = float.MinValue; //max distance
            float d2 = float.MaxValue; //min distance
                                       /* To count lengthInVelocityDirection we set a plane surface perpendicular to velocity including (0, 0, 0) point
                                        * Its normal is vSurf, so that plane is given with equation:
                                        *      (x, y, z) o vSurf = 0
                                        *  where V o W is a dot product of vectors V and W
                                        *  
                                        *  Distance of point A - d(A) from that plane is
                                        *      d(a) = A o vSurf / |vSurf|
                                        *  and since vSurf is normalized (|vSurf| = 1)
                                        *      d(A) = A o vSurf
                                        *      
                                        *  if it is negative the point is on the other side of surf (the one that vSurf does not point)
                                        *  
                                        *  let d1 be max distance from plane and d2 min distance from plane (we take into cosideration also negative distances)
                                        *  so lengthInVelocityDirection is d1 - d2;
                                        */
            underwaterTrianglesCoroutine.Clear();

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 globalPos = transform.TransformPoint(vertices[i]);
                verticesInGlobalSpace[i] = globalPos;
                verticesDepth[i] = Water.GetHeight(globalPos.x, globalPos.z, time) - globalPos.y;
                float d = Vector3.Dot(globalPos, vSurf);
                if (d > d1)
                    d1 = d;
                if (d < d2)
                    d2 = d;
            }

            lengthInVelocityDirection = d1 - d2;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                float[] depth = new float[3];
                depth[0] = verticesDepth[triangles[i]];
                depth[1] = verticesDepth[triangles[i + 1]];
                depth[2] = verticesDepth[triangles[i + 2]];

                Vector3[] pos = new Vector3[3];
                pos[0] = verticesInGlobalSpace[triangles[i]];
                pos[1] = verticesInGlobalSpace[triangles[i + 1]];
                pos[2] = verticesInGlobalSpace[triangles[i + 2]];

                if (depth[0] <= 0 && depth[1] <= 0 && depth[2] <= 0) // whole triangle above the water
                    continue;

                if (accuracy == Accuracy.Low)
                {
                    if (depth[0] + depth[1] + depth[2] >= 0)
                        AddUnderwaterTriangle(pos[0], pos[1], pos[2], depth[0], depth[1], depth[2]);
                }
                else
                {
                    if (depth[0] >= 0 && depth[1] >= 0 && depth[2] >= 0) // whole triangle under the water
                        AddUnderwaterTriangle(pos[0], pos[1], pos[2], depth[0], depth[1], depth[2]);
                    else if (depth[0] < 0 && depth[1] < 0) // only vertex 2 under the water
                    {
                        float t = depth[2] / (-depth[0] + depth[2]);
                        Vector3 w20 = Vector3.Lerp(pos[2], pos[0], t);
                        t = depth[2] / (-depth[1] + depth[2]);
                        Vector3 w21 = Vector3.Lerp(pos[2], pos[1], t);
                        AddUnderwaterTriangle(pos[2], w20, w21, depth[2], 0, 0);
                    }
                    else if (depth[2] < 0 && depth[1] < 0) // only vertex 0 under the water
                    {
                        float t = depth[0] / (depth[0] + -depth[2]);
                        Vector3 w02 = Vector3.Lerp(pos[0], pos[2], t);
                        t = depth[0] / (-depth[1] + depth[0]);
                        Vector3 w01 = Vector3.Lerp(pos[0], pos[1], t);
                        AddUnderwaterTriangle(pos[0], w01, w02, depth[0], 0, 0);
                    }
                    else if (depth[2] < 0 && depth[0] < 0) // only vertex 1 under the water
                    {
                        float t = depth[1] / (depth[1] + -depth[2]);
                        Vector3 w12 = Vector3.Lerp(pos[1], pos[2], t);
                        t = depth[1] / (depth[1] + -depth[0]);
                        Vector3 w10 = Vector3.Lerp(pos[1], pos[0], t);
                        AddUnderwaterTriangle(pos[1], w12, w10, depth[1], 0, 0);
                    }
                    else if (depth[0] < 0) // vertices 1 and 2 under the water
                    {
                        float t = depth[0] / (depth[0] + -depth[2]);
                        Vector3 w02 = Vector3.Lerp(pos[0], pos[2], t);
                        t = depth[0] / (-depth[1] + depth[0]);
                        Vector3 w01 = Vector3.Lerp(pos[0], pos[1], t);
                        AddUnderwaterTriangle(pos[1], w02, w01, depth[1], 0, 0);
                        AddUnderwaterTriangle(pos[1], pos[2], w02, depth[1], depth[2], 0);
                    }
                    else if (depth[1] < 0) // vertices 0 and 2 under the water
                    {
                        float t = depth[1] / (depth[1] + -depth[2]);
                        Vector3 w12 = Vector3.Lerp(pos[1], pos[2], t);
                        t = depth[1] / (depth[1] + -depth[0]);
                        Vector3 w10 = Vector3.Lerp(pos[1], pos[0], t);
                        AddUnderwaterTriangle(pos[0], w10, w12, depth[0], 0, 0);
                        AddUnderwaterTriangle(pos[0], w12, pos[2], depth[0], 0, depth[2]);
                    }
                    else // vertices 0 and 1 under the water
                    {
                        float t = depth[2] / (-depth[0] + depth[2]);
                        Vector3 w20 = Vector3.Lerp(pos[2], pos[0], t);
                        t = depth[2] / (-depth[1] + depth[2]);
                        Vector3 w21 = Vector3.Lerp(pos[2], pos[1], t);
                        AddUnderwaterTriangle(pos[0], w21, w20, depth[0], 0, 0);
                        AddUnderwaterTriangle(pos[0], pos[1], w21, depth[0], depth[1], 0);
                    }
                }
            }

            underwaterTrianglesForce = underwaterTrianglesCoroutine;
        }

        void AddUnderwaterTriangle(Vector3 a, Vector3 b, Vector3 c, float ha, float hb, float hc)
        {
            if (a == b || a == c || b == c)
                return;
            if (accuracy == Accuracy.High)
            {
                #region sort vertices
                bool normal = true;
                /* at first normal order of vertices (the one to set normal corectly) is:  a -> b -> c
                 * when we switch two vertices the order should be reverted
                 * normal changes after every switch
                 * normal == true when count of switches is even
                 * so 
                 * if  normal == true normal order is:  a -> b -> c
                 * and if not it is:  c -> b -> a
                 */
                if (a.y < b.y)
                {
                    Vector3 v = a;
                    a = b;
                    b = v;
                    float h = ha;
                    ha = hb;
                    hb = h;
                    normal = !normal;
                }
                if (b.y < c.y)
                {
                    Vector3 v = c;
                    c = b;
                    b = v;
                    float h = hc;
                    hc = hb;
                    hb = h;
                    normal = !normal;
                    if (a.y < b.y)
                    {
                        Vector3 v1 = a;
                        a = b;
                        b = v1;
                        float h1 = ha;
                        ha = hb;
                        hb = h1;
                        normal = !normal;
                    }
                }
                #endregion

                if (normal)
                {
                    if (a.y == b.y)
                    {
                        underwaterTrianglesCoroutine.Add(new TriangleData(c, a, b, hc));
                    }
                    else if (b.y == c.y)
                    {
                        underwaterTrianglesCoroutine.Add(new TriangleData(a, b, c, ha));
                    }
                    else //cut triangle in two triangles having one edge horizontally
                    {
                        float t = (a.y - b.y) / (a.y - c.y);
                        Vector3 d = Vector3.Lerp(a, c, t);
                        // d is a vertex on ac segment so that db segment is horizontal
                        underwaterTrianglesCoroutine.Add(new TriangleData(a, b, d, ha));
                        underwaterTrianglesCoroutine.Add(new TriangleData(c, d, b, hc));
                    }
                }
                else
                {
                    if (a.y == b.y)
                    {
                        underwaterTrianglesCoroutine.Add(new TriangleData(c, b, a, hc));
                    }
                    else if (b.y == c.y)
                    {
                        underwaterTrianglesCoroutine.Add(new TriangleData(a, c, b, ha));
                    }
                    else //cut triangle in two triangles having one edge horizontally
                    {
                        float t = (a.y - b.y) / (a.y - c.y);
                        Vector3 d = Vector3.Lerp(a, c, t);
                        // d is a vertex on ac segment so that db segment is horizontal
                        underwaterTrianglesCoroutine.Add(new TriangleData(a, d, b, ha));
                        underwaterTrianglesCoroutine.Add(new TriangleData(c, b, d, hc));
                    }
                }
            }
            else
            {
                underwaterTrianglesCoroutine.Add(new TriangleData(a, b, c));
            }
        }
        #endregion

        float ResistenceCoefficient()
        {
            /* Cf = 0.075 / (log10(Rn) - 2)^2
             * Rn = v * l / nu
             * 
             * Cf - resistance coefficient
             * Rn - Raynold's number
             * v - speed of the body
             * l - length of submerged body
             * nu - viscousity of the fluid
             * 
             * since nu at 20 degrees Celcius is 0.000001 we'll use nu^(-1) = 100 000
             * than Rn = v * l * nu(-1)
             * and Cf = 0.075 / (log10(v * l) + log10(nu^(-1)) - 2)^2
             *        = 0.075 / (log10(v * l) + log10(100 000) - 2)^2 =
             *        = 0.075 / (log10(v * l) + 3) ^ 2
             */
            float denominator = Mathf.Log10(rb.velocity.magnitude * lengthInVelocityDirection) + 3;
            denominator *= denominator;
            return 0.075f / denominator;
        }

        private void OnDrawGizmosSelected()
        {
            Color c = Gizmos.color;
            Gizmos.color = Color.red;
            foreach (TriangleData t in underwaterTrianglesCoroutine)
            {
                Gizmos.DrawLine(t.a, t.b);
                Gizmos.DrawLine(t.a, t.c);
                Gizmos.DrawLine(t.c, t.b);
                //Gizmos.DrawSphere(t.center, 0.01f);
            }
            Gizmos.color = c;
        }
    }
}
