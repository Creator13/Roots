using System.Collections;
using UnityEngine;
using Math = Roots.Util.Math;
using Random = UnityEngine.Random;

namespace Roots.World
{
    public class TreeOfLife : MonoBehaviour
    {
        [SerializeField] private Collider collider;
        
        [SerializeField] private EasingFunction.Ease easingFunction = EasingFunction.Ease.EaseInExpo;
        [SerializeField] private float fallDuration;
        [SerializeField] private Transform rootPoint;
        
        [SerializeField] private GameStateManager gameStateManager;

        public bool CanFall => !fallen;
        private bool fallen = false;
        
        private WorldVisualizationSwitcher visSwitcher;

        private void Awake()
        {
            visSwitcher = FindAnyObjectByType<WorldVisualizationSwitcher>();
        }

        public void Interact(Transform sourceTransform)
        {
            if (fallen) return;
            StartCoroutine(AnimateFall(sourceTransform.position));
        }

        private IEnumerator AnimateFall(Vector3 sourcePos)
        {
            fallen = true;
            
            Quaternion initialRotation = transform.rotation;
            
            Vector3 fallDirection = (transform.position - sourcePos).normalized;
            fallDirection.y = 0;
            fallDirection.Normalize();

            float axisOffset = Math.RandomNormalDistribution(.2f, .4f);
            axisOffset *= Random.value < .5f ? -1 : 1f;
            fallDirection += new Vector3(-fallDirection.z, 0f, fallDirection.x) * axisOffset;
            
            Vector3 axis = Vector3.Cross(Vector3.up, fallDirection);

            float timePassed = 0;
            while (timePassed < fallDuration)
            {
                float fallAngle = EasingFunction.GetEasingFunction(easingFunction)(0, 90, timePassed / fallDuration);
                transform.rotation = Quaternion.AngleAxis(fallAngle, axis) * initialRotation;
                
                timePassed += Time.deltaTime;
                yield return null;
            }

            gameStateManager.ProgressTreeFall(rootPoint.position);
            
            // Trigger collider update after fall
            collider.enabled = false;
            collider.enabled = true;
        }
    }
}