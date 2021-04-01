using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
public class RaycastWorldInstantiator : MonoBehaviour
{
    public GameObject worldPrefab;

    private GameObject worldInstance;

    private ARRaycastManager raycastManager;
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();

    void Start()
    {
        raycastManager = gameObject.GetComponent<ARRaycastManager>();
    }

    bool TryGetTouchPosition(out Vector2 touchPosition)
    {
        if (Input.touchCount > 0)
        {
            touchPosition = Input.GetTouch(0).position;
            return true;
        }

        touchPosition = default;
        return false;
    }

    void Update()
    {
        if (worldInstance != null)
        {
            return;
        }

        if (Input.touchCount == 0)
        {
            return;
        }

        var touchPosition = Input.GetTouch(0).position;
        if (raycastManager.Raycast(touchPosition, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            // Raycast hits are sorted by distance, so the first one will be the closest hit.
            var hitPose = raycastHits[0].pose;

            worldInstance = Instantiate(worldPrefab, hitPose.position, hitPose.rotation);
        }
    }
}
