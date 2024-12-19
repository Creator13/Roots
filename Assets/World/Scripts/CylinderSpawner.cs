using UnityEngine;

public class CylinderSpawner : MonoBehaviour
{
    [SerializeField] private GameObject cylinderPrefab;
    [SerializeField] private Vector2 xBound;
    [SerializeField] private Vector2 yBound;
    [SerializeField] private int count;

    private void Start()
    {
        for (int i = 0; i < count; i++)
        {
            GameObject cylinder = Instantiate(cylinderPrefab, transform);
            cylinder.transform.position = new Vector3(Random.Range(xBound.x, xBound.y), 0, Random.Range(yBound.x, yBound.y));
        }
    }
}