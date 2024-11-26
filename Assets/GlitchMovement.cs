using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class GlitchMovement : MonoBehaviour
{
    [SerializeField] private Vector3 centerPoint = Vector3.zero;
    [SerializeField] private float orbitSpeed = 1f;
    [SerializeField] private Vector3 orbitShape = Vector3.one; // Base orbit shape
    [SerializeField] private float orbitSize = 1;
    [SerializeField] private float noiseIntensity = 0.5f; // How much noise affects the orbit
    [SerializeField] private float noiseSpeed = 1f; // Speed of noise evolution

    private float orbitTime = 0f;

    private void Update()
    {
        orbitTime += Time.deltaTime * orbitSpeed;

        float noiseX = Mathf.PerlinNoise(Time.time * noiseSpeed, 0f) * noiseIntensity;
        float noiseY = Mathf.PerlinNoise(0f, Time.time * noiseSpeed) * noiseIntensity;
        float noiseZ = Mathf.PerlinNoise(Time.time * noiseSpeed, Time.time * noiseSpeed) * noiseIntensity;

        orbitShape = new Vector3(
            orbitShape.x + noiseX,
            orbitShape.y + noiseY,
            orbitShape.z + noiseZ
        ).normalized;

        Vector3 modulatedShape = orbitShape * orbitSize;

        float x = Mathf.Cos(orbitTime) * modulatedShape.x;
        float y = Mathf.Sin(orbitTime * 1.5f) * modulatedShape.y;
        float z = Mathf.Sin(orbitTime) * modulatedShape.z;

        transform.position = centerPoint + new Vector3(x, y, z);
    }
}