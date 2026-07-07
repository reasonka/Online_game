using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum CustomerReactionType
{
    None,
    Reaction1, // Correct order
    Reaction2, // Shared special order
    Reaction3  // Wrong order
}

public enum LevelOrderMode
{
    AutoFromSceneName,
    AllOrders,
    Level1_PizzaHotdogOneBeer,
    Level2_BurgerPancakeCocktail
}

[System.Serializable]
public class OrderDefinition
{
    public string orderName;
    public Image finalIcon;
    public GameObject reaction1FoodPrefab;
}

public class CustomerOrderUI : MonoBehaviour
{
    [Header("This customer's own UI")]
    public Image loadingCircle;

    [Header("All possible order definitions")]
    public OrderDefinition[] orders;

    [Header("Shared Reaction 2 Food")]
    public GameObject reaction2FoodPrefab;

    [Header("Level Order Filtering")]
    public LevelOrderMode levelOrderMode = LevelOrderMode.AutoFromSceneName;

    [Header("Level 1 Rule")]
    public int level1CustomerLimit = 5;

    [Header("Level 2 Rule")]
    public int level2FirstFoodOnlyOrders = 3;

    [Header("Level Objective Reporting")]
    public float reactionDelayBeforeCounting = 2.5f;

    [HideInInspector] public bool completed = false;
    [HideInInspector] public int currentOrderIndex = -1;

    private bool hasReportedServedToLevel = false;

    private static HashSet<string> usedOrderKeys = new HashSet<string>();
    private static int ordersGivenThisLevel = 0;
    private static int level1BeerOrderNumber = -1;
    private static bool level1BeerAlreadyUsed = false;
    private static string lastSceneName = "";

    public GameObject CurrentReaction1Food
    {
        get
        {
            if (currentOrderIndex >= 0 && currentOrderIndex < (orders?.Length ?? 0))
                return orders[currentOrderIndex].reaction1FoodPrefab;

            return null;
        }
    }

    public GameObject CurrentReaction2Food
    {
        get
        {
            return reaction2FoodPrefab;
        }
    }

    private void Awake()
    {
        EnsureSceneStateIsFresh();
        PrepareForWait();
    }

    private void EnsureSceneStateIsFresh()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (lastSceneName != currentSceneName)
        {
            ResetOrderStateForLevel();
            lastSceneName = currentSceneName;
        }

        if (GetActiveLevelOrderMode() == LevelOrderMode.Level1_PizzaHotdogOneBeer &&
            level1BeerOrderNumber <= 0)
        {
            level1BeerOrderNumber = Random.Range(1, level1CustomerLimit + 1);
            Debug.Log("Level 1 beer order will be customer/order number: " + level1BeerOrderNumber);
        }
    }

    public static void ResetOrderStateForLevel()
    {
        usedOrderKeys.Clear();
        ordersGivenThisLevel = 0;
        level1BeerOrderNumber = -1;
        level1BeerAlreadyUsed = false;

        Debug.Log("Customer order state reset for this level.");
    }

    public void PrepareForWait()
    {
        hasReportedServedToLevel = false;

        if (loadingCircle != null)
        {
            loadingCircle.fillAmount = 0f;
            loadingCircle.gameObject.SetActive(false);
        }

        HideAllIcons();

        currentOrderIndex = -1;
        completed = false;
    }

    public void SetProgress(float t)
    {
        if (loadingCircle != null)
        {
            loadingCircle.gameObject.SetActive(true);
            loadingCircle.fillAmount = Mathf.Clamp01(t);
        }
    }

    public void EndProgress()
    {
        if (loadingCircle != null)
        {
            loadingCircle.gameObject.SetActive(false);
        }
    }

    public void ShowRandomOrderIcon()
    {
        EnsureSceneStateIsFresh();

        if (orders == null || orders.Length == 0)
        {
            Debug.LogWarning("CustomerOrderUI has no orders assigned.");
            return;
        }

        HideAllIcons();

        List<int> availableOrderIndexes = GetAvailableOrderIndexes();

        if (availableOrderIndexes.Count == 0)
        {
            Debug.LogWarning("No valid unique orders left for this level/order position.");
            return;
        }

        int randomListIndex = Random.Range(0, availableOrderIndexes.Count);
        currentOrderIndex = availableOrderIndexes[randomListIndex];

        OrderDefinition chosen = orders[currentOrderIndex];

        if (chosen == null)
        {
            Debug.LogWarning("Chosen order is empty.");
            return;
        }

        if (chosen.finalIcon != null)
        {
            chosen.finalIcon.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Chosen order has no final icon assigned: " + GetOrderName(chosen));
        }

        string chosenOrderName = GetOrderName(chosen);
        string chosenKey = GetOrderKey(chosen);

        usedOrderKeys.Add(chosenKey);
        ordersGivenThisLevel++;

        if (IsBeerOrder(NormalizeName(chosenOrderName)))
        {
            level1BeerAlreadyUsed = true;
        }

        completed = true;

        Debug.Log("Customer order shown: " + chosenOrderName);

        SFXManager.Instance?.PlayNewOrder();

        Debug.Log("Order number this level: " + ordersGivenThisLevel);
        Debug.Log("Used unique orders this level: " + usedOrderKeys.Count);
    }

    private List<int> GetAvailableOrderIndexes()
    {
        List<int> availableIndexes = new List<int>();

        int nextOrderNumber = ordersGivenThisLevel + 1;

        for (int i = 0; i < orders.Length; i++)
        {
            OrderDefinition order = orders[i];

            if (order == null)
                continue;

            if (order.reaction1FoodPrefab == null)
            {
                Debug.LogWarning("Order has no correct prefab assigned: " + order.orderName);
                continue;
            }

            string orderName = GetOrderName(order);
            string cleanName = NormalizeName(orderName);
            string orderKey = GetOrderKey(order);

            if (usedOrderKeys.Contains(orderKey))
                continue;

            if (!IsOrderAllowedForThisLevelAndPosition(cleanName, nextOrderNumber))
                continue;

            availableIndexes.Add(i);
        }

        return availableIndexes;
    }

    private bool IsOrderAllowedForThisLevelAndPosition(string cleanOrderName, int nextOrderNumber)
    {
        LevelOrderMode activeMode = GetActiveLevelOrderMode();

        switch (activeMode)
        {
            case LevelOrderMode.Level1_PizzaHotdogOneBeer:
                return IsAllowedLevel1Order(cleanOrderName, nextOrderNumber);

            case LevelOrderMode.Level2_BurgerPancakeCocktail:
                return IsAllowedLevel2Order(cleanOrderName, nextOrderNumber);

            case LevelOrderMode.AllOrders:
                return true;

            default:
                return true;
        }
    }

    private bool IsAllowedLevel1Order(string cleanOrderName, int nextOrderNumber)
    {
        bool isBeer = IsBeerOrder(cleanOrderName);
        bool isFood = IsPizzaOrder(cleanOrderName) || IsHotdogOrder(cleanOrderName);

        if (level1BeerAlreadyUsed)
        {
            return isFood;
        }

        if (nextOrderNumber == level1BeerOrderNumber)
        {
            return isBeer;
        }

        if (nextOrderNumber >= level1CustomerLimit && !level1BeerAlreadyUsed)
        {
            return isBeer;
        }

        return isFood;
    }

    private bool IsAllowedLevel2Order(string cleanOrderName, int nextOrderNumber)
    {
        bool isFood = IsBurgerOrder(cleanOrderName) || IsPancakeOrder(cleanOrderName);
        bool isDrink = IsCocktailOrder(cleanOrderName);

        if (nextOrderNumber <= level2FirstFoodOnlyOrders)
        {
            return isFood;
        }

        return isFood || isDrink;
    }

    private LevelOrderMode GetActiveLevelOrderMode()
    {
        if (levelOrderMode != LevelOrderMode.AutoFromSceneName)
        {
            return levelOrderMode;
        }

        string sceneName = NormalizeName(SceneManager.GetActiveScene().name);

        if (sceneName.Contains("level1"))
        {
            return LevelOrderMode.Level1_PizzaHotdogOneBeer;
        }

        if (sceneName.Contains("level2"))
        {
            return LevelOrderMode.Level2_BurgerPancakeCocktail;
        }

        Debug.LogWarning("Scene name does not contain Level1 or Level2. Allowing all orders.");
        return LevelOrderMode.AllOrders;
    }

    private string GetOrderName(OrderDefinition order)
    {
        if (order == null)
            return "";

        if (!string.IsNullOrWhiteSpace(order.orderName))
            return order.orderName;

        if (order.reaction1FoodPrefab != null)
            return order.reaction1FoodPrefab.name;

        return "";
    }

    private string GetOrderKey(OrderDefinition order)
    {
        return NormalizeName(GetOrderName(order));
    }

    private bool IsBurgerOrder(string cleanName)
    {
        return cleanName.StartsWith("burger");
    }

    private bool IsHotdogOrder(string cleanName)
    {
        return cleanName.StartsWith("hotdog");
    }

    private bool IsBeerOrder(string cleanName)
    {
        return cleanName.Contains("beer");
    }

    private bool IsPizzaOrder(string cleanName)
    {
        return cleanName.StartsWith("pizza");
    }

    private bool IsPancakeOrder(string cleanName)
    {
        return cleanName.StartsWith("pancake");
    }

    private bool IsCocktailOrder(string cleanName)
    {
        return cleanName.Contains("cocktail");
    }

    public CustomerReactionType EvaluateFood(FoodItem servedFood)
    {
        if (currentOrderIndex < 0 ||
            orders == null ||
            currentOrderIndex >= orders.Length ||
            servedFood == null)
        {
            return CustomerReactionType.None;
        }

        OrderDefinition currentOrder = orders[currentOrderIndex];

        Debug.Log(
            $"Customer wants: {GetOrderName(currentOrder)} / " +
            $"Correct={currentOrder.reaction1FoodPrefab?.name} / " +
            $"Shared Reaction2={reaction2FoodPrefab?.name} / " +
            $"Served Food Object={servedFood.name} / " +
            $"Served Prefab ID={servedFood.foodPrefabId?.name} / " +
            $"Served Food Type={servedFood.foodType}"
        );

        if (FoodMatches(servedFood, reaction2FoodPrefab))
        {
            Debug.Log("Matched Reaction2. Shared special order served.");
            return CustomerReactionType.Reaction2;
        }

        if (FoodMatches(servedFood, currentOrder.reaction1FoodPrefab))
        {
            Debug.Log("Matched Reaction1. Correct order served.");
            return CustomerReactionType.Reaction1;
        }

        Debug.Log("No match. Wrong order served.");
        return CustomerReactionType.Reaction3;
    }

    public CustomerReactionType EvaluateFood(GameObject servedFoodPrefab)
    {
        if (currentOrderIndex < 0 ||
            orders == null ||
            currentOrderIndex >= orders.Length ||
            servedFoodPrefab == null)
        {
            return CustomerReactionType.None;
        }

        OrderDefinition currentOrder = orders[currentOrderIndex];

        if (GameObjectMatches(servedFoodPrefab, reaction2FoodPrefab))
        {
            return CustomerReactionType.Reaction2;
        }

        if (GameObjectMatches(servedFoodPrefab, currentOrder.reaction1FoodPrefab))
        {
            return CustomerReactionType.Reaction1;
        }

        return CustomerReactionType.Reaction3;
    }

    private bool FoodMatches(FoodItem servedFood, GameObject targetPrefab)
    {
        if (servedFood == null || targetPrefab == null)
            return false;

        if (servedFood.foodPrefabId == targetPrefab)
            return true;

        string targetKey = NormalizeName(targetPrefab.name);

        string servedPrefabKey = "";
        if (servedFood.foodPrefabId != null)
        {
            servedPrefabKey = NormalizeName(servedFood.foodPrefabId.name);
        }

        string servedObjectKey = NormalizeName(servedFood.gameObject.name);
        string servedFoodTypeKey = NormalizeName(servedFood.foodType.ToString());

        if (!string.IsNullOrEmpty(servedPrefabKey) && servedPrefabKey == targetKey)
            return true;

        if (!string.IsNullOrEmpty(servedObjectKey) && servedObjectKey == targetKey)
            return true;

        if (!string.IsNullOrEmpty(servedFoodTypeKey) && servedFoodTypeKey != "none" && servedFoodTypeKey == targetKey)
            return true;

        return false;
    }

    private bool GameObjectMatches(GameObject servedPrefab, GameObject targetPrefab)
    {
        if (servedPrefab == null || targetPrefab == null)
            return false;

        if (servedPrefab == targetPrefab)
            return true;

        string servedKey = NormalizeName(servedPrefab.name);
        string targetKey = NormalizeName(targetPrefab.name);

        return servedKey == targetKey;
    }

    private string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "";

        return name
            .ToLower()
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "")
            .Replace("(clone)", "");
    }

    public void ResetForNextTime()
    {
        PrepareForWait();
    }

    private void HideAllIcons()
    {
        if (orders == null) return;

        foreach (OrderDefinition order in orders)
        {
            if (order != null && order.finalIcon != null)
            {
                order.finalIcon.gameObject.SetActive(false);
            }
        }
    }

    public void PlayReaction(CustomerReactionType reaction)
    {
        if (reaction == CustomerReactionType.None)
        {
            Debug.LogWarning("No valid reaction. Customer will not react.");
            return;
        }

        CustomerAI customer = GetComponentInParent<CustomerAI>();

        if (customer == null)
        {
            Debug.LogError("No CustomerAI found!");
            return;
        }

        switch (reaction)
        {
            case CustomerReactionType.Reaction1:
                SFXManager.Instance?.PlayCorrectOrderServed();
                SFXManager.Instance?.PlayCustomerPraise();
                customer.PlayHappyReaction();
                break;

            case CustomerReactionType.Reaction2:
                SFXManager.Instance?.PlayCustomerDisappointed();
                customer.PlayDeathReaction();
                break;

            case CustomerReactionType.Reaction3:
                SFXManager.Instance?.PlayWrongOrderServed();
                SFXManager.Instance?.PlayCustomerDisappointed();
                customer.PlayAngryReaction();
                break;
        }

        ReportServedToLevelAfterReaction(reaction, reactionDelayBeforeCounting);
    }

    public void ReportServedToLevelAfterReaction(CustomerReactionType reaction, float reactionDelay)
    {
        if (hasReportedServedToLevel) return;

        hasReportedServedToLevel = true;

        if (LevelPerformanceTracker.Instance != null)
        {
            LevelPerformanceTracker.Instance.ReportCustomerReaction(reaction, reactionDelay);
        }

        // Keep your old Level 1 completion system working.
        if (Level1ObjectiveManager.Instance != null)
        {
            Level1ObjectiveManager.Instance.CustomerServedAfterReaction(reactionDelay);
        }
    }
}