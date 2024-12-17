using System;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class GlitchMovement : MonoBehaviour
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

    private void FixedUpdate()
    {
        // float deltaTime = Mathf.Max(Time.deltaTime, .033f);
        float deltaTime = Time.fixedDeltaTime;
        
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
        target.localPosition += velocity * deltaTime;

        // Rotate
        target.Rotate(target.up, deltaTime * rotateSpeed);
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