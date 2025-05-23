using UnityEngine;
using UnityEngine.Serialization;

namespace Roots.Player
{
    public class LooseTransformParenter : MonoBehaviour
    {
        [FormerlySerializedAs("headBone")]
        [SerializeField] private Transform targetPositionTransform;
        [FormerlySerializedAs("cameraTargetTransform")] [SerializeField]
        private Transform objectToMove;


        private void LateUpdate()
        {
            objectToMove.position = targetPositionTransform.position;
        }
    }
}