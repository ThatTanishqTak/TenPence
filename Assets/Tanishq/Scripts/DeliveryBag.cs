using UnityEngine;

public class DeliveryBag : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("Behaviour")]
    [SerializeField] private bool deliverOnlyOnce = true;
    [SerializeField] private bool startNewOrderAfterDeliver = true;
    [SerializeField] private bool resetSandwichAfterDeliver = false;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private bool hasDelivered;

    private void Awake()
    {
        if (debugLogs)
        {
            Debug.Log("[DeliveryBag] Awake() on: " + name);
        }

        if (!gameManager)
        {
            gameManager = GameManager.Instance;

            if (debugLogs)
            {
                Debug.Log("[DeliveryBag] gameManager was NULL. Using GameManager.Instance: " + (gameManager != null ? gameManager.name : "NULL"));
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (debugLogs)
        {
            Debug.Log("[DeliveryBag] OnTriggerEnter() other: " + (other != null ? other.name : "NULL"));
        }

        if (deliverOnlyOnce && hasDelivered)
        {
            if (debugLogs)
            {
                Debug.Log("[DeliveryBag] OnTriggerEnter() ignored. Already delivered once.");
            }

            return;
        }

        if (!gameManager)
        {
            gameManager = GameManager.Instance;

            if (debugLogs)
            {
                Debug.Log("[DeliveryBag] OnTriggerEnter() gameManager was NULL. Re-fetching Instance: " + (gameManager != null ? gameManager.name : "NULL"));
            }
        }

        if (!gameManager)
        {
            if (debugLogs)
            {
                Debug.LogWarning("[DeliveryBag] OnTriggerEnter() aborted. No GameManager reference found.");
            }

            return;
        }

        SandwichStack stack = other.GetComponentInParent<SandwichStack>();

        if (!stack)
        {
            if (debugLogs)
            {
                Debug.Log("[DeliveryBag] OnTriggerEnter() No SandwichStack found in parent hierarchy.");
            }

            return;
        }

        if (debugLogs)
        {
            Debug.Log("[DeliveryBag] SandwichStack detected: " + stack.name + " -> Submitting sandwich.");
        }

        hasDelivered = true;

        GameManager.SandwichScore score = gameManager.SubmitSandwich(stack.gameObject);

        if (debugLogs)
        {
            Debug.Log("[DeliveryBag] SubmitSandwich() returned stars: " + score.stars);
        }

        if (resetSandwichAfterDeliver)
        {
            if (debugLogs)
            {
                Debug.Log("[DeliveryBag] resetSandwichAfterDeliver is ON -> ResetSandwich().");
            }

            stack.ResetSandwich();
        }

        if (startNewOrderAfterDeliver)
        {
            if (debugLogs)
            {
                Debug.Log("[DeliveryBag] startNewOrderAfterDeliver is ON -> StartNewOrder().");
            }

            gameManager.StartNewOrder();
        }

        if (debugLogs)
        {
            Debug.Log("[DeliveryBag] OnTriggerEnter() DONE.");
        }
    }
}