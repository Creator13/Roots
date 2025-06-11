using Roots.World;
using Roots.World.Chunking;
using UnityEngine;

namespace Roots.Player
{
    public class VegetationInteractor : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private ChunkLoader chunkLoader;
        [SerializeField] private VegetationRootManager vegetationManager;

        public bool HasTargetInRange { get; private set; }
        public VegetationRoot Target { get; private set; }
        
        private void Update()
        {
            // UpdateClosestInstance();
            ShootBeamThroughGrass();
        }

        public void Interact()
        {

        }

        private void ShootBeamThroughGrass()
        {
            // TODO do the raycast in the interaction controller so that it happens only once every frame (and settings can be tweaked from there)
            Ray centerRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            centerRay.direction += -mainCamera.transform.up * .12f; // angle ray downward ever so slightly for more natural aiming on grass patches
            if (chunkLoader.RaycastTerrain(centerRay, 5, out var hitPoint))
            {
                foreach (var current in vegetationManager.GetAll())
                {
                    if (current.FullGrownInstanceRatio < .95f) continue;
                    
                    var positionFlat = current.transform.position;
                    positionFlat.y = hitPoint.y;
                    if ((positionFlat - hitPoint).sqrMagnitude < current.Radius * current.Radius)
                    {
                        HasTargetInRange = true;
                        Target = current;
                        return;
                    }
                }
            }

            HasTargetInRange = false;
            Target = null;
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
                if (current.FullGrownInstanceRatio < .95f) continue;
                
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