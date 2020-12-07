using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhysWater
{
    public class TriangleData
    {
        #region Variables
        /// <summary>
        /// vertex
        /// </summary>
        public Vector3 a, b, c; // b.y == c.y

        /// <summary>
        /// point of application of hydrostatic force
        /// </summary>
        public Vector3 buoyanceCenter;

        /// <summary>
        /// center of tirangle
        /// </summary>
        public Vector3 center;

        /// <summary>
        /// vector perpendicular to the surface of the triangle of length equal to the area of the triangle
        /// </summary>
        private Vector3 normal;

        /// <summary>
        /// velocity of triangle (sum of linear and angular)
        /// </summary>
        private Vector3 velocity;

        /// <summary>
        /// cos of an angle between velocity and normal
        /// </summary>
        private float cosTheta;

        /// <summary>
        /// sin of an angle betweenvelocity and normal
        /// </summary>
        private float sinTheta;

        #endregion
        #region Constructors
        /// <summary>
        /// should be used if accuracy isn't High
        /// </summary>
        public TriangleData(Vector3 a, Vector3 b, Vector3 c)
        {
            this.a = a;
            this.b = b;
            this.c = c;

            buoyanceCenter = (a + b + c) / 3;
            center = buoyanceCenter;

            normal = Vector3.Cross(b - a, c - a) * .5f;
        }
        /// <summary>
        /// should be used if accuracy is High, than vertices need to be in correct order
        /// </summary>
        /// <param name="a">the highest or the lowest vertex</param>
        /// <param name="b">one of two verex with the same y component</param>
        /// <param name="c">one of two verex with the same y component</param>
        /// <param name="z0">depth of vertex a</param>
        public TriangleData(Vector3 a, Vector3 b, Vector3 c, float z0)
        {
            this.a = a;
            this.b = b;
            this.c = c;

            float h = a.y - b.y;
            buoyanceCenter = a + ((c + b) * 0.5f - a) * (4 * Mathf.Abs(z0) + 3 * h) / (6 * Mathf.Abs(z0) + 4 * h); //calculation based on https://gamasutra.com/view/news/237528/Water_interaction_model_for_boats_in_video_games.php 
            center = (a + b + c) / 3;

            normal = Vector3.Cross(b - a, c - a) * .5f;
        }
        #endregion

        #region Hydrostatic 
        // All calculation based on:
        // https://gamasutra.com/view/news/237528/Water_interaction_model_for_boats_in_video_games.php

        public Vector3 HydrostaticForce(float rho)
        {
            Vector3 f = rho * Physics.gravity.y * (Water.GetHeight(buoyanceCenter.x, buoyanceCenter.z, Time.time) - buoyanceCenter.y) * normal;
            f.x = 0f;
            f.z = 0f;

            return ForceIsValid(f, "buoyancy");
        }

        #endregion
        #region Hydrodynamic 
        // All calculation based on:
        //https://www.gamasutra.com/view/news/263237/Water_interaction_model_for_boats_in_video_games_Part_2.php

        public void RecalculateDynamicVariables(Rigidbody rb)
        {
            velocity = rb.velocity + Vector3.Cross(rb.angularVelocity, center - rb.worldCenterOfMass);
            cosTheta = Vector3.Dot(normal, velocity) / (normal.magnitude * velocity.magnitude);
            // sinTheta = Mathf.Sqrt(1 - cosTheta * cosTheta);
        }
        /* Viscous
        public Vector3 Viscous(float rho, float Cf, Rigidbody rb)
        {
            /*  
             *  F - viscous force
            *   rho - density(float)
            *   Vf - relative velocity (Vector3) of the flow at the center of the triangle 
            *   S - area(float)
            *   Cf - Coefficient of frictional resistance
            *   v - velocity of triangle
            *   n - vector perpendicual to triangle's surface of length equal triangle's area
            *   N - normal of triangle (n = N * S)
            *   
            *   than
            *   
            *   F = 0.5 * rho * Cf * S * |Vf| * Vf       
            *   
            *   v x n - cross product perpendicular to v and n
            *   n x (v x n) gives us a vector tangent to triangle surface that is projection of v on that surface
            *   
            *   Vf = - |v| * n x (v x n) / |n x (v x n)| =
            *      =  - |v| * n x (v x n) / (S * |N| * |v x n| * sin(pi/2))=  // since |n| = 1 and sin(pi/2) = 1
            *      =  - |v| * n x (v x n) / (S * |v x n|)=
            *      = |v| * (v x n) x n / (S * |v x n|)
            *   
            *   |Vf| = |v|
            *   
            *   so
            *   
            *   F = 0.5 * rho * Cf * S * |v| * |v| * (v x n) x n / (S * |v x n|) =
            *     = 0.5 * rho * Cf * |v|^2 * (v x n) x n / |v x n|   


            Vector3 w = Vector3.Cross(velocity, normal).normalized;
            Vector3 F = 0.5f * rho * Cf * speed * speed * Vector3.Cross(w, normal);

            return ForceIsValid(F, "viscous");
        }
        */

        public Vector3 PressureDrag()
        {
            Vector3 F = -normal * cosTheta;
            return ForceIsValid(F, "pressure drag");
        }
        #endregion


        private static Vector3 ForceIsValid(Vector3 f, string name)
        {
            if (!float.IsNaN(f.x + f.y + f.z))
                return f;
            else
            {
                Debug.Log(name + " force is NaN");
                return Vector3.zero;
            }
        }
    }
}