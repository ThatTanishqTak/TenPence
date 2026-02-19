using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum IngredientMatchMode
    {
        IgnoreOrder,
        ExactOrder
    }

    [Serializable]
    public class OrderDefinition
    {
        public string orderName;
        public List<string> ingredientIds = new();
    }

    [Serializable]
    public class SandwichScore
    {
        public string orderName;
        public int requiredCount;
        public int deliveredCount;
        public int correctCount;
        public int wrongCount;
        public int missingCount;
        public int stars;
        public List<string> required = new();
        public List<string> delivered = new();
    }

    [Header("Orders")]
    [SerializeField] private List<OrderDefinition> availableOrders = new();
    [SerializeField] private bool pickRandomOrderOnStart = true;
    [SerializeField] private IngredientMatchMode matchMode = IngredientMatchMode.IgnoreOrder;
    [SerializeField] private bool perfectRequiresNoExtras = false;

    [Header("Star Rating")]
    [SerializeField] private int maxStars = 3;
    [SerializeField] private float oneStarAt = 0.34f;
    [SerializeField] private float twoStarsAt = 0.67f;
    [SerializeField] private float threeStarsAt = 1.0f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private OrderDefinition currentOrder;
    private int currentOrderIndex = -1;

    public event Action<OrderDefinition> OnNewOrder;
    public event Action<SandwichScore> OnSandwichSubmitted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (debugLogs)
            {
                Debug.LogWarning("[GameManager] Duplicate instance detected. Destroying: " + name);
            }

            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (debugLogs)
        {
            Debug.Log("[GameManager] Awake() Instance set: " + name);
            Debug.Log("[GameManager] availableOrders count: " + (availableOrders != null ? availableOrders.Count : 0));
            Debug.Log("[GameManager] matchMode: " + matchMode + " perfectRequiresNoExtras: " + perfectRequiresNoExtras);
            Debug.Log("[GameManager] star thresholds: 1*=" + oneStarAt + " 2*=" + twoStarsAt + " 3*=" + threeStarsAt + " maxStars: " + maxStars);
        }
    }

    private void Start()
    {
        if (debugLogs)
        {
            Debug.Log("[GameManager] Start() pickRandomOrderOnStart: " + pickRandomOrderOnStart);
        }

        if (pickRandomOrderOnStart)
        {
            StartNewOrder();
        }
    }

    public OrderDefinition GetCurrentOrder()
    {
        return currentOrder;
    }

    public void StartNewOrder(int index = -1)
    {
        if (availableOrders == null || availableOrders.Count == 0)
        {
            if (debugLogs)
            {
                Debug.LogWarning("[GameManager] StartNewOrder() aborted. No availableOrders configured in Inspector.");
            }

            currentOrder = null;
            currentOrderIndex = -1;
            return;
        }

        if (index < 0)
        {
            index = UnityEngine.Random.Range(0, availableOrders.Count);
        }

        index = Mathf.Clamp(index, 0, availableOrders.Count - 1);
        currentOrderIndex = index;
        currentOrder = availableOrders[index];

        if (debugLogs)
        {
            Debug.Log("[GameManager] New Order Selected. Index: " + currentOrderIndex);
            Debug.Log("[GameManager] Order Name: " + (currentOrder != null ? currentOrder.orderName : "NULL"));
            Debug.Log("[GameManager] Required Ingredients: " + (currentOrder != null ? string.Join(", ", currentOrder.ingredientIds) : "NULL"));
        }

        OnNewOrder?.Invoke(currentOrder);
    }

    public SandwichScore EvaluateSandwich(GameObject sandwichRoot)
    {
        if (debugLogs)
        {
            Debug.Log("[GameManager] EvaluateSandwich() sandwichRoot: " + (sandwichRoot != null ? sandwichRoot.name : "NULL"));
        }

        SandwichScore score = new();

        if (currentOrder == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning("[GameManager] EvaluateSandwich() aborted. currentOrder is NULL.");
            }

            score.orderName = "NO_ORDER";
            return score;
        }

        score.orderName = currentOrder.orderName;
        score.required = new List<string>(currentOrder.ingredientIds);
        score.requiredCount = score.required.Count;

        if (sandwichRoot == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning("[GameManager] EvaluateSandwich() sandwichRoot is NULL. Returning 0 score.");
            }

            score.deliveredCount = 0;
            score.correctCount = 0;
            score.wrongCount = 0;
            score.missingCount = score.requiredCount;
            score.stars = 0;
            return score;
        }

        Ingredient[] deliveredIngredients = sandwichRoot.GetComponentsInChildren<Ingredient>(true);

        if (debugLogs)
        {
            Debug.Log("[GameManager] EvaluateSandwich() Found delivered Ingredients: " + (deliveredIngredients != null ? deliveredIngredients.Length : 0));
        }

        for (int i = 0; i < deliveredIngredients.Length; i++)
        {
            Ingredient ing = deliveredIngredients[i];

            if (ing == null)
            {
                continue;
            }

            score.delivered.Add(ing.GetId());
        }

        score.deliveredCount = score.delivered.Count;

        if (matchMode == IngredientMatchMode.ExactOrder)
        {
            if (debugLogs)
            {
                Debug.Log("[GameManager] EvaluateSandwich() Using ExactOrder matching.");
            }

            int min = Mathf.Min(score.required.Count, score.delivered.Count);
            int correct = 0;

            for (int i = 0; i < min; i++)
            {
                if (string.Equals(score.required[i], score.delivered[i], StringComparison.OrdinalIgnoreCase))
                {
                    correct++;
                }
            }

            score.correctCount = correct;
            score.wrongCount = score.deliveredCount - correct;
            score.missingCount = score.requiredCount - correct;
        }
        else
        {
            if (debugLogs)
            {
                Debug.Log("[GameManager] EvaluateSandwich() Using IgnoreOrder matching (multiset). ");
            }

            Dictionary<string, int> requiredCounts = new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < score.required.Count; i++)
            {
                string id = score.required[i];

                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (requiredCounts.ContainsKey(id))
                {
                    requiredCounts[id]++;
                }
                else
                {
                    requiredCounts[id] = 1;
                }
            }

            int correct = 0;
            int wrong = 0;

            for (int i = 0; i < score.delivered.Count; i++)
            {
                string got = score.delivered[i];

                if (string.IsNullOrWhiteSpace(got))
                {
                    wrong++;
                    continue;
                }

                if (requiredCounts.TryGetValue(got, out int count) && count > 0)
                {
                    correct++;
                    requiredCounts[got] = count - 1;
                }
                else
                {
                    wrong++;
                }
            }

            int missing = 0;

            foreach (var kvp in requiredCounts)
            {
                missing += kvp.Value;
            }

            score.correctCount = correct;
            score.wrongCount = wrong;
            score.missingCount = missing;
        }

        score.stars = CalculateStars(score);

        if (debugLogs)
        {
            Debug.Log("[GameManager] EvaluateSandwich() RESULT");
            Debug.Log("[GameManager] Order: " + score.orderName);
            Debug.Log("[GameManager] Required(" + score.requiredCount + "): " + string.Join(", ", score.required));
            Debug.Log("[GameManager] Delivered(" + score.deliveredCount + "): " + string.Join(", ", score.delivered));
            Debug.Log("[GameManager] Correct: " + score.correctCount + " Wrong: " + score.wrongCount + " Missing: " + score.missingCount + " Stars: " + score.stars + "/" + maxStars);
        }

        return score;
    }

    public SandwichScore EvaluateSandwich(SandwichStack stack)
    {
        return EvaluateSandwich(stack != null ? stack.gameObject : null);
    }

    public SandwichScore SubmitSandwich(SandwichStack stack)
    {
        return SubmitSandwich(stack != null ? stack.gameObject : null);
    }

    public SandwichScore SubmitSandwich(GameObject sandwichRoot)
    {
        if (debugLogs)
        {
            Debug.Log("[GameManager] SubmitSandwich() called.");
        }

        SandwichScore score = EvaluateSandwich(sandwichRoot);

        if (debugLogs)
        {
            Debug.Log("[GameManager] SubmitSandwich() DONE. Stars: " + score.stars);
        }

        OnSandwichSubmitted?.Invoke(score);
        return score;
    }

    private int CalculateStars(SandwichScore score)
    {
        if (score == null)
        {
            return 0;
        }

        if (score.requiredCount <= 0)
        {
            return 0;
        }

        float ratio = score.correctCount / (float)score.requiredCount;

        if (debugLogs)
        {
            Debug.Log("[GameManager] CalculateStars() ratio: " + ratio);
        }

        if (perfectRequiresNoExtras)
        {
            if (ratio >= threeStarsAt && score.wrongCount == 0)
            {
                return Mathf.Clamp(3, 0, maxStars);
            }

            if (ratio >= twoStarsAt)
            {
                return Mathf.Clamp(2, 0, maxStars);
            }

            if (ratio >= oneStarAt)
            {
                return Mathf.Clamp(1, 0, maxStars);
            }

            return 0;
        }

        if (ratio >= threeStarsAt)
        {
            return Mathf.Clamp(3, 0, maxStars);
        }

        if (ratio >= twoStarsAt)
        {
            return Mathf.Clamp(2, 0, maxStars);
        }

        if (ratio >= oneStarAt)
        {
            return Mathf.Clamp(1, 0, maxStars);
        }

        return 0;
    }
}