using System;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshRenderer))]
public class ColorRandomizer : MonoBehaviour
{
    [SerializeField] private string colorPropertyName;
    [SerializeField] private Color colorA = Color.white;
    [SerializeField] private Color colorB= Color.white;
    
    private MeshRenderer renderer;
    
    private void Awake()
    {
        renderer = GetComponent<MeshRenderer>();
        renderer.material.SetColor(colorPropertyName, Color.Lerp(colorA, colorB, Random.value));
    }
}
