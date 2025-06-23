using Roots.Util;
using Roots.World;
using Roots.World.Chunking;
using StarterAssets;
using UnityEngine;

namespace Roots.Player
{
    public class InteractionController : MonoBehaviour // aka "hands"
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private ChunkLoader terrain;
        [SerializeField] private VegetationRootManager vegetationManager;
        [SerializeField] private StarterAssetsInputs input;

        [Space]
        [SerializeField] private VegetationInteractor vegetationInteractor;
        [SerializeField] private CharacterAnimationController characterAnimationController;
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private FirstPersonController firstPersonController;

        [Space]
        [SerializeField] private Transform placePreview;
        [SerializeField] private float maxReachDistance = 5;

        private Vector3 previewPosition = Vector3.positiveInfinity;
        private bool canPlace;
        private bool hasPhysicsTarget;
        private GameObject interactionTarget;
        
        private bool foundTree = false;
        private bool foundGlitchSeed = false;

        private bool placeMode = false;
        public bool HasInteractionTarget { get; private set; } = false;

        private void Start()
        {
            placePreview.gameObject.SetActive(placeMode);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C)) SetPlaceMode(!placeMode);

            UpdatePhysicsInteractionTarget();

            HasInteractionTarget = !firstPersonController.IsMoving 
                                   && !placeMode 
                                   && (vegetationInteractor.HasTargetInRange || hasPhysicsTarget);

            if (placeMode)
            {
                UpdatePlacePreview();

                if (Input.GetMouseButtonDown(0))
                {
                    if (canPlace) Place();
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.G))
                {
                    TryToggleSit();
                }

                if (Input.GetKeyDown(KeyCode.E) && !firstPersonController.IsMoving)
                {
                    if (hasPhysicsTarget)
                    {
                        if (foundTree) interactionTarget.GetComponent<TreeOfLife>().Interact(transform);
                        else if (foundGlitchSeed)
                        {
                            gameStateManager.CollectSeed(interactionTarget);
                        }
                    }
                    else if (vegetationInteractor.HasTargetInRange)
                    {
                        vegetationInteractor.Interact();
                    }
                }
            }
        }

        private void TryToggleSit()
        {
            if (characterAnimationController.IsStateChangeLocked) return;

            if (characterAnimationController.IsKneeling)
            {
                characterAnimationController.StopKneeling();
            }
            else
            {
                characterAnimationController.StartKneeling();
            }
        }

        private void UpdatePhysicsInteractionTarget()
        {
            foundTree = false;
            foundGlitchSeed = false;
            interactionTarget = null;

            Ray centerRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            if (Physics.Raycast(centerRay, out var hit, maxReachDistance))
            {
                var treeComponent = hit.transform.GetComponent<TreeOfLife>();
                if (treeComponent)
                {
                    if (treeComponent.CanFall)
                    {
                        foundTree = true;
                    }
                }
                else
                {
                    var plantSeedComponent = hit.transform.GetComponent<PlantSeed>();
                    if (plantSeedComponent)
                    {
                        foundGlitchSeed = true;
                    }
                }
            } 

            hasPhysicsTarget = foundTree || foundGlitchSeed;
            if (hasPhysicsTarget)
            {
                interactionTarget = hit.transform.gameObject;
            }
        }

        private void SetPlaceMode(bool newMode)
        {
            placeMode = newMode;
            canPlace = false;
            placePreview.gameObject.SetActive(placeMode);
        }

        private void Place()
        {
            vegetationManager.PlaceNew(previewPosition);
            SetPlaceMode(false);
        }

        private void UpdatePlacePreview()
        {
            Ray centerRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            centerRay.direction += -mainCamera.transform.up * .12f; // angle ray downward ever so slightly for more natural aiming on grass patches
            if (terrain.RaycastTerrain(centerRay, maxReachDistance, out var hitPoint))
            {
                canPlace = true;
                previewPosition = hitPoint;
                placePreview.position = previewPosition;
            }
            else
            {
                canPlace = false;
            }

            placePreview.gameObject.SetActive(canPlace);
        }

        private void OnDrawGizmos()
        {
            Ray centerRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            centerRay.direction += -mainCamera.transform.up * .2f;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(centerRay.origin, centerRay.direction * 5);
        }
    }
}