using UnityEngine;

public class Ingredient : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Unique ID used for orders (e.g. 'Bread', 'Tomato'). If empty, falls back to GameObject name (without '(Clone)').")]
    public string ingredientId = "";

    [Tooltip("If > 0, overrides automatic thickness from bounds.")]
    public float thicknessOverride = 0f;

    [Tooltip("Optional: what point should be centered on the sandwich (defaults to transform).")]
    public Transform centerPivot;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Renderer cachedRenderer;
    private Collider cachedCollider;

    private void Awake()
    {
        if (debugLogs)
        {
            Debug.Log("[Ingredient] Awake() on: " + name);
            Debug.Log("[Ingredient] ingredientId: " + (string.IsNullOrWhiteSpace(ingredientId) ? "(EMPTY)" : ingredientId));
        }

        cachedRenderer = GetComponentInChildren<Renderer>();
        cachedCollider = GetComponentInChildren<Collider>();

        if (debugLogs)
        {
            Debug.Log("[Ingredient] cachedRenderer: " + (cachedRenderer != null ? cachedRenderer.name : "NULL"));
            Debug.Log("[Ingredient] cachedCollider: " + (cachedCollider != null ? cachedCollider.name : "NULL"));
        }

        if (centerPivot == null)
        {
            centerPivot = transform;

            if (debugLogs)
            {
                Debug.Log("[Ingredient] centerPivot was NULL, set to transform: " + centerPivot.name);
            }
        }
        else
        {
            if (debugLogs)
            {
                Debug.Log("[Ingredient] centerPivot already assigned: " + centerPivot.name);
            }
        }
    }

    public string GetId()
    {
        string id = ingredientId;

        if (string.IsNullOrWhiteSpace(id))
        {
            id = gameObject.name;

            if (id.EndsWith("(Clone)"))
            {
                id = id.Replace("(Clone)", "").Trim();
            }
        }

        if (debugLogs)
        {
            Debug.Log("[Ingredient] GetId() on: " + name + " -> " + id);
        }

        return id;
    }

    public float GetThickness()
    {
        if (debugLogs)
        {
            Debug.Log("[Ingredient] GetThickness() called on: " + name);
            Debug.Log("[Ingredient] thicknessOverride: " + thicknessOverride);
        }

        if (thicknessOverride > 0f)
        {
            if (debugLogs)
            {
                Debug.Log("[Ingredient] Using thicknessOverride: " + thicknessOverride);
            }

            return thicknessOverride;
        }

        if (cachedCollider != null)
        {
            float t = cachedCollider.bounds.size.y;

            if (debugLogs)
            {
                Debug.Log("[Ingredient] Using Collider bounds thickness: " + t + " collider: " + cachedCollider.name);
            }

            return t;
        }

        if (cachedRenderer != null)
        {
            float t = cachedRenderer.bounds.size.y;

            if (debugLogs)
            {
                Debug.Log("[Ingredient] Using Renderer bounds thickness: " + t + " renderer: " + cachedRenderer.name);
            }

            return t;
        }

        if (debugLogs)
        {
            Debug.LogWarning("[Ingredient] No Collider/Renderer found. Returning fallback thickness 0.02");
        }

        return 0.02f;
    }
}