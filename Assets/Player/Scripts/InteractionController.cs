using Roots.World;
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
        [SerializeField] private Transform placePreview;
        [SerializeField] private float maxReachDistance = 5;
        
        private Vector3 previewPosition = Vector3.positiveInfinity;
        private bool canPlace;
        
        private bool placeMode = false;

        private void Start()
        {
            placePreview.gameObject.SetActive(placeMode);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C)) SetPlaceMode(!placeMode);

            if (placeMode)
            {
                UpdatePlacePreview();

                if (Input.GetMouseButtonDown(0))
                {
                    if (canPlace) Place();
                }
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
            if (terrain.RaycastTerrain(centerRay, maxReachDistance, out var hitPoint))
            {
                canPlace = true;
                previewPosition = hitPoint;
            }
            else
            {
                canPlace = false;
            }

            placePreview.position = previewPosition;
            placePreview.gameObject.SetActive(canPlace);
        }
    }
}