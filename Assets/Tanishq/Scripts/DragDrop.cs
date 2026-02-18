using UnityEngine;
using UnityEngine.InputSystem;

public class DragDrop : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask draggableMask = ~0;

    private Transform dragged;
    private Plane dragPlane;
    private Vector3 grabOffset;

    private void Awake()
    {
        if (!mainCamera)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (mainCamera == null || Mouse.current == null)
        {
            return;
        }

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, draggableMask, QueryTriggerInteraction.Ignore))
            {
                dragged = hit.transform;
                dragPlane = new Plane(mainCamera.transform.forward, hit.point);

                if (dragPlane.Raycast(ray, out float enter))
                {
                    Vector3 planeHit = ray.GetPoint(enter);
                    grabOffset = dragged.position - planeHit;
                }
            }
        }

        if (Mouse.current.leftButton.isPressed && dragged != null)
        {
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 planeHit = ray.GetPoint(enter);
                dragged.position = planeHit + grabOffset;
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            dragged = null;
        }
    }
}