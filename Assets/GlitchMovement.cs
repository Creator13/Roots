using UnityEngine;

public class GlitchMovement : MonoBehaviour
{
    [SerializeField] private Transform child;
    [SerializeField] private float perlinSpeed = 1;
    [SerializeField, Range(0, 1)] private float moveThreshold = 0.6f;
    [SerializeField] private float moveStrength = 0.1f;
    [SerializeField] private float attractStrength = 5;
    [SerializeField, Range(0, 1)] private float damping = 0.95f; // Damping factor for velocity

    private Vector3 acceleration;
    private Vector3 velocity;

    private void Update()
    {
        // Random movement using Perlin noise
        if (Mathf.PerlinNoise(Time.time * perlinSpeed + 2, 0) > moveThreshold)
        {
            acceleration += Random.insideUnitSphere * (Mathf.PerlinNoise(Time.time, 0) * moveStrength);
        }

        ApplyPullBack();

        // Update velocity with damping
        velocity += acceleration * Time.deltaTime;
        velocity *= damping; // Apply damping to reduce overshoot

        // Update position
        child.position += velocity * Time.deltaTime;

        // Reset acceleration for the next frame
        acceleration = Vector3.zero;
    }

    private void ApplyPullBack()
    {
        Vector3 direction = transform.position - child.position;
        float dist = direction.magnitude;

        // Pull force proportional to distance
        Vector3 pullVector = direction.normalized * (dist * attractStrength);
        acceleration += pullVector;
    }
}