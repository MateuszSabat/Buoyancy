using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhysWater;

public class WaterShaderSript : MonoBehaviour
{
    Material material;
    public EndlessOcean ocean;


    void Start()
    {
        material = GetComponent<Renderer>().sharedMaterial;
    }

    void Update()
    {
        Vector4 waveParams = new Vector4(
            ocean.frequency,
            ocean.amplitude,
            ocean.inverseLength.x,
            ocean.inverseLength.y);

        material.SetVector("waveParams", waveParams);
    }
}
