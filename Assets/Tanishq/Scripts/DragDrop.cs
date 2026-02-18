using UnityEngine;
using UnityEngine.InputSystem;

public class DragDrop : MonoBehaviour
{
    [Header("Required Refferences")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask draggableMask = ~0;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference pointAction;
    [SerializeField] private InputActionReference clickAction;

    [Header("Rigidbody handling")]
    [SerializeField] private bool moveRigidbodyIfPresent = true;
    [SerializeField] private bool setKinematicWhileDragging = true;

    private Transform dragged;
    private Rigidbody draggedRigidbody;
    private bool oldKinematic;
    private Plane dragPlane;
    private Vector3 grabOffset;

    private void Awake()
    {
        if (!mainCamera)
        {
            mainCamera = Camera.main;
        }
        if (!mainCamera)
        {
            Debug.LogError("No Camera assigned and no MainCamera found. Dragging won't work.");
        }
    }

    private void OnEnable()
    {
        if (pointAction)
        {
            pointAction.action.Enable();
        }

        if (clickAction)
        {
            clickAction.action.Enable();
            clickAction.action.started += OnPress;
            clickAction.action.canceled += OnRelease;
        }
    }

    private void OnDisable()
    {
        if (clickAction)
        {
            clickAction.action.started -= OnPress;
            clickAction.action.canceled -= OnRelease;
            clickAction.action.Disable();
        }

        if (pointAction)
        {
            pointAction.action.Disable();
        }
    }

    private void OnPress(InputAction.CallbackContext ctx)
    {
        if (!mainCamera || pointAction == null)
        {
            return;
        }

        Vector2 screenPosition = pointAction.action.ReadValue<Vector2>();
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, draggableMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        dragged = hit.transform;
        draggedRigidbody = dragged.GetComponent<Rigidbody>();
        dragPlane = new Plane(mainCamera.transform.forward, hit.point);

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 planeHit = ray.GetPoint(enter);
            grabOffset = dragged.position - planeHit;
        }

        if (draggedRigidbody && setKinematicWhileDragging)
        {
            oldKinematic = draggedRigidbody.isKinematic;
            draggedRigidbody.isKinematic = true;
        }
    }

    private void OnRelease(InputAction.CallbackContext ctx)
    {
        if (draggedRigidbody && setKinematicWhileDragging)
        {
            draggedRigidbody.isKinematic = oldKinematic;
        }

        dragged = null;
        draggedRigidbody = null;
    }

    private void Update()
    {
        if (!dragged || !mainCamera || pointAction == null)
        {
            return;
        }

        Vector2 screenPosition = pointAction.action.ReadValue<Vector2>();
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 planeHit = ray.GetPoint(enter);
            Vector3 target = planeHit + grabOffset;

            if (moveRigidbodyIfPresent && draggedRigidbody)
            {
                draggedRigidbody.MovePosition(target);
            }
            else
            {
                dragged.position = target;
            }
        }
    }
}