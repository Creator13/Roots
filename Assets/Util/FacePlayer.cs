using Roots.Player;
using UnityEngine;

namespace Roots.Util
{
    public class FacePlayer : MonoBehaviour
    {
        [SerializeField] private Transform target;

        private void Start()
        {
            if (target == null)
            {
                target = FindFirstObjectByType<FirstPersonController>().transform;
            }
        }

        private void Update()
        {
            Vector3 direction =  transform.position - target.position;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = targetRotation;
            }
        }
    }
}