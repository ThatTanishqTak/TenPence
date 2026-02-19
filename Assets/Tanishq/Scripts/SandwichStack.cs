using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class SandwichStack : MonoBehaviour
{
    [Header("Stack Settings")]
    [SerializeField] private Transform stackRoot;
    [SerializeField] private Transform stackOrigin;
    [SerializeField] private float padding = 0.001f;

    [Header("Game / Order")]
    [SerializeField] private bool resetOnNewOrder = true;
    [SerializeField] private bool lockStackWhenOrderComplete = true;
    [SerializeField] private bool requireNoExtrasForCompletion = false;

    [Header("Whole Sandwich Drag")]
    [SerializeField] private bool enableWholeSandwichDragOnComplete = true;
    [SerializeField] private Collider wholeSandwichCollider;
    [SerializeField] private Rigidbody wholeSandwichRigidbody;
    [SerializeField] private bool autoAddRigidbodyWhenComplete = true;
    [SerializeField] private bool autoFitBoxColliderOnComplete = true;
    [SerializeField] private bool disableWholeSandwichColliderUntilComplete = true;
    [SerializeField] private bool disableIngredientCollidersOnComplete = true;
    [SerializeField] private bool setKinematicOnComplete = true;
    [SerializeField] private int lockedLayer = 0;
    [SerializeField] private int draggableLayer = 0;

    [Header("Physics / Interaction")]
    [SerializeField] private bool disableCollidersAfterPlace = false;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly List<Ingredient> ingredients = new();
    private float currentTopY;

    private GameManager gameManager;
    private bool isOrderComplete;

    public bool IsOrderComplete => isOrderComplete;

    private void Awake()
    {
        if (debugLogs)
        {
            Debug.Log("[SandwichStack] Awake() on: " + name);
        }

        if (!stackRoot)
        {
            stackRoot = transform;

            if (debugLogs)
            {
                Debug.Log("[SandwichStack] stackRoot was NULL, set to transform: " + stackRoot.name);
            }
        }
        else
        {
            if (debugLogs)
            {
                Debug.Log("[SandwichStack] stackRoot assigned: " + stackRoot.name);
            }
        }

        if (!stackOrigin)
        {
            stackOrigin = transform;

            if (debugLogs)
            {
                Debug.Log("[SandwichStack] stackOrigin was NULL, set to transform: " + stackOrigin.name);
            }
        }
        else
        {
            if (debugLogs)
            {
                Debug.Log("[SandwichStack] stackOrigin assigned: " + stackOrigin.name);
            }
        }

        if (!wholeSandwichCollider)
        {
            wholeSandwichCollider = GetComponent<Collider>();

            if (debugLogs)
            {
                Debug.Log("[SandwichStack] wholeSandwichCollider was NULL, using GetComponent<Collider>(): " + (wholeSandwichCollider != null ? wholeSandwichCollider.name : "NULL"));
            }
        }

        if (!wholeSandwichRigidbody)
        {
            wholeSandwichRigidbody = GetComponent<Rigidbody>();

            if (debugLogs)
            {
                Debug.Log("[SandwichStack] wholeSandwichRigidbody was NULL, using GetComponent<Rigidbody>(): " + (wholeSandwichRigidbody != null ? wholeSandwichRigidbody.name : "NULL"));
            }
        }

        currentTopY = stackOrigin.position.y;

        if (debugLogs)
        {
            Debug.Log("[SandwichStack] currentTopY initialized to: " + currentTopY);
            Debug.Log("[SandwichStack] padding: " + padding + " disableCollidersAfterPlace: " + disableCollidersAfterPlace);
            Debug.Log("[SandwichStack] enableWholeSandwichDragOnComplete: " + enableWholeSandwichDragOnComplete + " disableWholeSandwichColliderUntilComplete: " + disableWholeSandwichColliderUntilComplete);
            Debug.Log("[SandwichStack] lockedLayer: " + lockedLayer + " draggableLayer: " + draggableLayer);
        }

        ApplyWholeSandwichDragState(false);

        gameManager = GameManager.Instance;

        if (debugLogs)
        {
            Debug.Log("[SandwichStack] GameManager.Instance: " + (gameManager != null ? gameManager.name : "NULL"));
        }

        if (gameManager != null && resetOnNewOrder)
        {
            gameManager.OnNewOrder += HandleNewOrder;

            if (debugLogs)
            {
                Debug.Log("[SandwichStack] Subscribed to GameManager.OnNewOrder");
            }
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null && resetOnNewOrder)
        {
            gameManager.OnNewOrder -= HandleNewOrder;
        }
    }

    private void HandleNewOrder(GameManager.OrderDefinition order)
    {
        if (debugLogs)
        {
            Debug.Log("[SandwichStack] HandleNewOrder() called. Order: " + (order != null ? order.orderName : "NULL"));
        }

        ResetSandwich();
    }

    public void AddIngredient(Ingredient ingredient)
    {
        if (debugLogs)
        {
            Debug.Log("[SandwichStack] AddIngredient() called with: " + (ingredient != null ? ingredient.name : "NULL"));
        }

        if (!ingredient)
        {
            if (debugLogs)
            {
                Debug.LogWarning("[SandwichStack] AddIngredient() aborted. ingredient is NULL.");
            }

            return;
        }

        if (lockStackWhenOrderComplete && isOrderComplete)
        {
            if (debugLogs)
            {
                Debug.Log("[SandwichStack] AddIngredient() ignored. Order already complete.");
            }

            return;
        }

        ingredients.Add(ingredient);

        if (debugLogs)
        {
            Debug.Log("[SandwichStack] Added to list. Total ingredients: " + ingredients.Count);
        }

        ingredient.transform.SetParent(stackRoot, true);

        if (debugLogs)
        {
            Debug.Log("[SandwichStack] SetParent -> stackRoot: " + stackRoot.name);
        }

        float t = ingredient.GetThickness();

        if (debugLogs)
        {
            Debug.Log("[SandwichStack] Thickness returned: " + t);
            Debug.Log("[SandwichStack] currentTopY before place: " + currentTopY);
        }

        float y = currentTopY + (t * 0.5f) + padding;

        Vector3 pos = stackOrigin.position;
        pos.y = y;

        if (debugLogs)
        {
            Debug.Log("[SandwichStack] Computed placement y: " + y);
            Debug.Log("[SandwichStack] stackOrigin position: " + stackOrigin.position);
            Debug.Log("[SandwichStack] target pos: " + pos);
        }

        Vector3 pivotOffset = Vector3.zero;

        if (ingredient.centerPivot)
        {
            pivotOffset = ingredient.centerPivot.position - ingredient.transform.position;

            if (debugLogs)
            {
                Debug.Log("[SandwichStack] centerPivot found: " + ingredient.centerPivot.name);
                Debug.Log("[SandwichStack] pivotOffset: " + pivotOffset);
            }
        }
        else
        {
            if (debugLogs)
            {
                Debug.Log("[SandwichStack] centerPivot is NULL. pivotOffset = Vector3.zero");
            }
        }

        ingredient.transform.position = pos - pivotOffset;

        if (debugLogs)
        {
            Debug.Log("[SandwichStack] Ingredient positioned to: " + ingredient.transform.position);
        }

        currentTopY = y + (t * 0.5f);

        if (debugLogs)
        {
            Debug.Log("[SandwichStack] currentTopY updated to: " + currentTopY);
        }

        if (ingredient.TryGetComponent<Rigidbody>(out var rb))
        {
            if (debugLogs)
            {
                Debug.Log("[SandwichStack] Rigidbody found on ingredient. Freezing it (kinematic).");
            }

            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (debugLogs)
            {
                Debug.Log("[SandwichStack] Rigidbody set. isKinematic: " + rb.isKinematic);
            }
        }
        else
        {
            if (debugLogs)
            {
                Debug.Log("[SandwichStack] No Rigidbody found on ingredient.");
            }
        }

        if (disableCollidersAfterPlace)
        {
            if (debugLogs)
            {
                Debug.Log("[SandwichStack] disableCollidersAfterPlace is ON. Disabling ingredient colliders.");
            }

            var colliders = ingredient.GetComponentsInChildren<Collider>();

            if (debugLogs)
            {
                Debug.Log("[SandwichStack] Found colliders count: " + colliders.Length);
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;

                if (debugLogs)
                {
                    Debug.Log("[SandwichStack] Disabled collider: " + colliders[i].name);
                }
            }
        }

        if (debugLogs)
        {
            Debug.Log("[SandwichStack] AddIngredient() DONE for: " + ingredient.name);
        }

        TryUpdateOrderCompletion();
    }

    private void TryUpdateOrderCompletion()
    {
        if (isOrderComplete)
        {
            return;
        }

        if (!gameManager)
        {
            gameManager = GameManager.Instance;

            if (debugLogs)
            {
                Debug.Log("[SandwichStack] TryUpdateOrderCompletion() gameManager was NULL. Re-fetching Instance: " + (gameManager != null ? gameManager.name : "NULL"));
            }
        }

        if (!gameManager)
        {
            if (debugLogs)
            {
                Debug.LogWarning("[SandwichStack] TryUpdateOrderCompletion() aborted. No GameManager found.");
            }

            return;
        }

        if (gameManager.GetCurrentOrder() == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning("[SandwichStack] TryUpdateOrderCompletion() aborted. GameManager has no current order.");
            }

            return;
        }

        GameManager.SandwichScore score = gameManager.EvaluateSandwich(gameObject);

        bool complete = score.missingCount == 0;

        if (requireNoExtrasForCompletion)
        {
            complete = complete && score.wrongCount == 0;
        }

        if (debugLogs)
        {
            Debug.Log("[SandwichStack] TryUpdateOrderCompletion() missing: " + score.missingCount + " wrong: " + score.wrongCount + " requireNoExtrasForCompletion: " + requireNoExtrasForCompletion);
            Debug.Log("[SandwichStack] TryUpdateOrderCompletion() complete: " + complete);
        }

        if (complete)
        {
            SetOrderComplete(true);
        }
    }

    private void SetOrderComplete(bool complete)
    {
        if (isOrderComplete == complete)
        {
            return;
        }

        isOrderComplete = complete;

        if (debugLogs)
        {
            Debug.Log("[SandwichStack] SetOrderComplete() -> " + isOrderComplete);
        }

        ApplyWholeSandwichDragState(isOrderComplete);

        if (isOrderComplete)
        {
            if (debugLogs)
            {
                Debug.Log("[SandwichStack] Order completed -> Whole sandwich can now be dragged to DeliveryBag.");
            }
        }
    }

    private void ApplyWholeSandwichDragState(bool enabled)
    {
        if (!enableWholeSandwichDragOnComplete)
        {
            if (debugLogs)
            {
                Debug.Log("[SandwichStack] ApplyWholeSandwichDragState() ignored. enableWholeSandwichDragOnComplete is OFF.");
            }

            return;
        }

        if (wholeSandwichCollider)
        {
            if (!enabled && disableWholeSandwichColliderUntilComplete)
            {
                wholeSandwichCollider.enabled = false;

                if (debugLogs)
                {
                    Debug.Log("[SandwichStack] Whole sandwich collider DISABLED (until complete): " + wholeSandwichCollider.name);
                }
            }
            else
            {
                wholeSandwichCollider.enabled = true;

                if (debugLogs)
                {
                    Debug.Log("[SandwichStack] Whole sandwich collider ENABLED: " + wholeSandwichCollider.name);
                }
            }
        }
        else
        {
            if (debugLogs)
            {
                Debug.LogWarning("[SandwichStack] No wholeSandwichCollider assigned. Whole-sandwich dragging may not work.");
            }
        }

        if (enabled)
        {
            if (autoFitBoxColliderOnComplete)
            {
                TryFitBoxColliderToSandwich();
            }

            if (disableIngredientCollidersOnComplete)
            {
                DisableIngredientColliders();
            }

            ApplyLayerRecursive(gameObject, draggableLayer);

            if (wholeSandwichCollider)
            {
                ApplyLayerRecursive(wholeSandwichCollider.gameObject, draggableLayer);
            }

            EnsureWholeSandwichRigidbody();
        }
        else
        {
            ApplyLayerRecursive(gameObject, lockedLayer);

            if (wholeSandwichCollider)
            {
                ApplyLayerRecursive(wholeSandwichCollider.gameObject, lockedLayer);
            }
        }

        if (debugLogs)
        {
            Debug.Log("[SandwichStack] ApplyWholeSandwichDragState() enabled: " + enabled);
        }
    }

    private void EnsureWholeSandwichRigidbody()
    {
        if (!wholeSandwichRigidbody)
        {
            wholeSandwichRigidbody = GetComponent<Rigidbody>();

            if (debugLogs)
            {
                Debug.Log("[SandwichStack] EnsureWholeSandwichRigidbody() GetComponent<Rigidbody>(): " + (wholeSandwichRigidbody != null ? wholeSandwichRigidbody.name : "NULL"));
            }
        }

        if (!wholeSandwichRigidbody && autoAddRigidbodyWhenComplete)
        {
            wholeSandwichRigidbody = gameObject.AddComponent<Rigidbody>();

            if (debugLogs)
            {
                Debug.Log("[SandwichStack] EnsureWholeSandwichRigidbody() Added Rigidbody to stack root.");
            }
        }

        if (wholeSandwichRigidbody)
        {
            wholeSandwichRigidbody.useGravity = false;
            wholeSandwichRigidbody.isKinematic = setKinematicOnComplete;
            wholeSandwichRigidbody.linearVelocity = Vector3.zero;
            wholeSandwichRigidbody.angularVelocity = Vector3.zero;
            wholeSandwichRigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            if (debugLogs)
            {
                Debug.Log("[SandwichStack] EnsureWholeSandwichRigidbody() Configured. isKinematic: " + wholeSandwichRigidbody.isKinematic);
            }
        }
        else
        {
            if (debugLogs)
            {
                Debug.LogWarning("[SandwichStack] EnsureWholeSandwichRigidbody() No Rigidbody available. DeliveryBag trigger may not fire unless bag has a Rigidbody.");
            }
        }
    }

    private void DisableIngredientColliders()
    {
        if (debugLogs)
        {
            Debug.Log("[SandwichStack] DisableIngredientColliders() called. Ingredients: " + ingredients.Count);
        }

        for (int i = 0; i < ingredients.Count; i++)
        {
            var ing = ingredients[i];

            if (!ing)
            {
                continue;
            }

            var cols = ing.GetComponentsInChildren<Collider>(true);

            for (int c = 0; c < cols.Length; c++)
            {
                cols[c].enabled = false;
            }

            if (debugLogs)
            {
                Debug.Log("[SandwichStack] Disabled colliders for ingredient: " + ing.name + " count: " + cols.Length);
            }
        }
    }

    private void TryFitBoxColliderToSandwich()
    {
        if (!wholeSandwichCollider)
        {
            return;
        }

        if (wholeSandwichCollider is not BoxCollider box)
        {
            if (debugLogs)
            {
                Debug.Log("[SandwichStack] TryFitBoxColliderToSandwich() skipped. wholeSandwichCollider is not a BoxCollider.");
            }

            return;
        }

        bool hasBounds = false;
        Bounds b = new(transform.position, Vector3.zero);

        for (int i = 0; i < ingredients.Count; i++)
        {
            Ingredient ing = ingredients[i];

            if (!ing)
            {
                continue;
            }

            Collider ingCol = ing.GetComponentInChildren<Collider>();
            Renderer ingRend = ing.GetComponentInChildren<Renderer>();

            if (ingCol)
            {
                if (!hasBounds)
                {
                    b = ingCol.bounds;
                    hasBounds = true;
                }
                else
                {
                    b.Encapsulate(ingCol.bounds);
                }
            }
            else if (ingRend)
            {
                if (!hasBounds)
                {
                    b = ingRend.bounds;
                    hasBounds = true;
                }
                else
                {
                    b.Encapsulate(ingRend.bounds);
                }
            }
        }

        if (!hasBounds)
        {
            if (debugLogs)
            {
                Debug.Log("[SandwichStack] TryFitBoxColliderToSandwich() no bounds found. Skipping.");
            }

            return;
        }

        Vector3 localCenter = transform.InverseTransformPoint(b.center);
        Vector3 localSize = transform.InverseTransformVector(b.size);

        box.center = localCenter;
        box.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

        if (debugLogs)
        {
            Debug.Log("[SandwichStack] TryFitBoxColliderToSandwich() Fit BoxCollider. center: " + box.center + " size: " + box.size);
        }
    }

    private void ApplyLayerRecursive(GameObject root, int layer)
    {
        if (!root)
        {
            return;
        }

        root.layer = layer;

        for (int i = 0; i < root.transform.childCount; i++)
        {
            ApplyLayerRecursive(root.transform.GetChild(i).gameObject, layer);
        }
    }

    public void ResetSandwich()
    {
        if (debugLogs)
        {
            Debug.Log("[SandwichStack] ResetSandwich() called. Ingredients count: " + ingredients.Count);
        }

        for (int i = 0; i < ingredients.Count; i++)
        {
            var ing = ingredients[i];

            if (ing != null)
            {
                if (debugLogs)
                {
                    Debug.Log("[SandwichStack] Destroying ingredient: " + ing.name);
                }

                Destroy(ing.gameObject);
            }
            else
            {
                if (debugLogs)
                {
                    Debug.Log("[SandwichStack] Ingredient at index " + i + " is NULL.");
                }
            }
        }

        ingredients.Clear();
        currentTopY = stackOrigin.position.y;

        if (debugLogs)
        {
            Debug.Log("[SandwichStack] Cleared list. currentTopY reset to: " + currentTopY);
        }

        SetOrderComplete(false);

        if (debugLogs)
        {
            Debug.Log("[SandwichStack] ResetSandwich() DONE.");
        }
    }
}