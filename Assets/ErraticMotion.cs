using UnityEngine;

public class ErraticMotion : MonoBehaviour
{
    [SerializeField] private Transform target;
    
    [SerializeField] private float outwardForce = 10f;
    [SerializeField] private float pullForce = 20f;
    [SerializeField] private float damping = 0.98f;
    [SerializeField, Range(0, 1)] private float burstChance = .2f;
    [SerializeField, Range(0, 1)] private float perlinThreshold = .5f;
    [SerializeField] private float perlinSpeed = 1;
    [SerializeField] private float centerRandomization = .3f;
    [SerializeField] private float rotateSpeed = 1;

    private Vector3 velocity = Vector3.zero;

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        var randomCenter = Random.insideUnitSphere * centerRandomization;
        Vector3 pullDirection = (randomCenter - target.localPosition).normalized;
        float distanceFromCenter = (randomCenter - target.localPosition).magnitude;
        Vector3 pull = pullDirection * (pullForce * distanceFromCenter);

        velocity += pull * deltaTime;
        velocity *= damping;

        if (Random.value < burstChance && Mathf.PerlinNoise1D((Time.time * perlinSpeed) % 500) > perlinThreshold)
        {
            ApplyRandomBurst();
        }

        // Apply velocity
        target.localPosition += Vector3.ClampMagnitude(velocity, 5f * outwardForce) * deltaTime;

        // Rotate
        target.Rotate(target.up, Time.deltaTime * rotateSpeed);
        if (velocity.sqrMagnitude > 0.01f)
        {
            target.rotation *= Quaternion.LookRotation(Vector3.forward, velocity.normalized);
        }
    }

    private void ApplyRandomBurst()
    {
        Vector3 randomDirection = Random.insideUnitSphere;

        velocity += randomDirection * outwardForce;
    }
}