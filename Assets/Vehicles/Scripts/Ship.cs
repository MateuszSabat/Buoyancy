using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour
{
    Rigidbody rb;

    public Transform centerOfMass;

    public Cargo[] cargo;
    public bool cargoGizmos;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass.localPosition;
    }


    private void FixedUpdate()
    {
        for (int i = 0; i < cargo.Length; i++)
            rb.AddForceAtPosition(cargo[i].mass * Physics.gravity, transform.TransformPoint(cargo[i].position));
    }

    private void OnDrawGizmosSelected()
    {
        if(cargoGizmos && cargo != null)
            for(int i=0; i<cargo.Length; i++)
                Gizmos.DrawCube(transform.TransformPoint(cargo[i].position), new Vector3(cargo[i].mass, cargo[i].mass, cargo[i].mass) * 0.01f);
    }
}
