using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum CustomerReactionType
{
    None,
    Reaction1, // Correct food/drink
    Reaction2, // Shared special food/drink
    Reaction3  // Wrong food/drink
}

public enum LevelOrderMode
{
    AutoFromSceneName,
    AllOrders,
    Level1_BeersOnly,
    Level2_CocktailsOnly
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

    [Header("Non-Repeating Orders")]
    public bool preventRepeatedOrdersInLevel = true;

    [Tooltip("Level 1 needs this ON if you have 5 customers but only 2 beer orders.")]
    public bool autoResetOrderPoolInLevel1 = true;

    [Tooltip("Level 2 should usually keep this OFF so cocktails stay unique.")]
    public bool autoResetOrderPoolInLevel2 = false;

    [HideInInspector] public bool completed = false;
    [HideInInspector] public int currentOrderIndex = -1;

    private static HashSet<string> usedOrderKeys = new HashSet<string>();
    private static bool sceneResetHookAdded = false;

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
        AddSceneResetHook();
        PrepareForWait();
    }

    private void AddSceneResetHook()
    {
        if (sceneResetHookAdded) return;

        SceneManager.sceneLoaded += OnSceneLoaded;
        sceneResetHookAdded = true;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearUsedOrdersForLevel();
    }

    public static void ClearUsedOrdersForLevel()
    {
        usedOrderKeys.Clear();
        Debug.Log("Used customer order pool cleared for this level.");
    }

    public void PrepareForWait()
    {
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
        if (orders == null || orders.Length == 0)
        {
            Debug.LogWarning("CustomerOrderUI has no orders assigned.");
            return;
        }

        HideAllIcons();

        List<int> availableOrderIndexes = GetAvailableOrderIndexes();

        if (availableOrderIndexes.Count == 0)
        {
            Debug.LogWarning("No unique allowed customer orders left for this level.");

            if (ShouldAutoResetOrderPoolForCurrentLevel())
            {
                Debug.Log("Resetting order pool for this level because all allowed orders were used.");
                usedOrderKeys.Clear();
                availableOrderIndexes = GetAvailableOrderIndexes();
            }
            else
            {
                return;
            }
        }

        if (availableOrderIndexes.Count == 0)
        {
            Debug.LogWarning("Still no allowed orders after reset. Check your order names.");
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

        if (preventRepeatedOrdersInLevel)
        {
            usedOrderKeys.Add(GetOrderKey(chosen));
        }

        completed = true;

        Debug.Log("Customer order shown: " + GetOrderName(chosen));
        Debug.Log("Used unique orders this level: " + usedOrderKeys.Count);
    }

    private List<int> GetAvailableOrderIndexes()
    {
        List<int> availableIndexes = new List<int>();

        for (int i = 0; i < orders.Length; i++)
        {
            OrderDefinition order = orders[i];

            if (order == null)
                continue;

            if (order.reaction1FoodPrefab == null)
            {
                Debug.LogWarning("Order has no correct food/drink prefab assigned: " + order.orderName);
                continue;
            }

            string orderName = GetOrderName(order);

            if (!IsOrderAllowedInCurrentLevel(orderName))
            {
                continue;
            }

            string orderKey = GetOrderKey(order);

            if (preventRepeatedOrdersInLevel && usedOrderKeys.Contains(orderKey))
            {
                continue;
            }

            availableIndexes.Add(i);
        }

        return availableIndexes;
    }

    private bool ShouldAutoResetOrderPoolForCurrentLevel()
    {
        LevelOrderMode activeMode = GetActiveLevelOrderMode();

        if (activeMode == LevelOrderMode.Level1_BeersOnly)
        {
            return autoResetOrderPoolInLevel1;
        }

        if (activeMode == LevelOrderMode.Level2_CocktailsOnly)
        {
            return autoResetOrderPoolInLevel2;
        }

        return false;
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

    private bool IsOrderAllowedInCurrentLevel(string orderName)
    {
        LevelOrderMode activeMode = GetActiveLevelOrderMode();
        string cleanName = NormalizeName(orderName);

        switch (activeMode)
        {
            case LevelOrderMode.Level1_BeersOnly:
                return IsBeerOrder(cleanName);

            case LevelOrderMode.Level2_CocktailsOnly:
                return IsCocktailOrder(cleanName);

            case LevelOrderMode.AllOrders:
                return true;

            default:
                return true;
        }
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
            return LevelOrderMode.Level1_BeersOnly;
        }

        if (sceneName.Contains("level2"))
        {
            return LevelOrderMode.Level2_CocktailsOnly;
        }

        Debug.LogWarning("Scene name does not contain Level1 or Level2. Allowing all orders.");
        return LevelOrderMode.AllOrders;
    }

    private bool IsBeerOrder(string cleanName)
    {
        return cleanName.Contains("beer");
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
            Debug.Log("Matched Reaction2. Shared special food/drink served.");
            return CustomerReactionType.Reaction2;
        }

        if (FoodMatches(servedFood, currentOrder.reaction1FoodPrefab))
        {
            Debug.Log("Matched Reaction1. Correct food/drink served.");
            return CustomerReactionType.Reaction1;
        }

        Debug.Log("No match. Wrong food/drink served.");
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
                customer.PlayHappyReaction();
                break;

            case CustomerReactionType.Reaction2:
                customer.PlayDeathReaction();
                break;

            case CustomerReactionType.Reaction3:
                customer.PlayAngryReaction();
                break;
        }
    }
}