using Roots.World;
using UnityEngine;

namespace Roots.Player
{
    public class VegetationInteractor : MonoBehaviour
    {
        private VegetationRootManager vegetationManager;

        public bool HasTargetInRange { get; private set; }
        public VegetationRoot Target { get; private set; }
        
        private void Update()
        {
            UpdateClosestInstance();
        }

        private void UpdateClosestInstance()
        {
            Vector3 playerPos = transform.position;
            Vector2 playerPosFlat = new Vector2(playerPos.x, playerPos.z);
            
            float closestDistance = float.MaxValue;
            VegetationRoot closest = null;
            bool hasInRange = false;
            
            foreach (var current in vegetationManager.GetAll())
            {
                float sqrRadius = current.Radius * current.Radius;
                
                Vector2 currentPosFlat = new Vector2(current.transform.position.x, current.transform.position.z);
                float sqrDistance = (currentPosFlat - playerPosFlat).sqrMagnitude;
                
                // Check if instance is closer than current closest *distance*
                if (sqrDistance < closestDistance)
                {
                    // Assign new closest distance to the *distance* of the current
                    closestDistance = sqrDistance;
                    // Only assign *instance* to the current instance if the player is also in its radius
                    if (sqrDistance < sqrRadius)
                    {
                        closest = current;
                        hasInRange = true;
                        // Keep searching for a new closest instance until all options have been checked, but the loop may 
                        // exit with hasInRange = false if even the closest instance wasn't in the player's range.
                    }
                }
            }

            HasTargetInRange = hasInRange;
            Target = closest;
        }
    }
}