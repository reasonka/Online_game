using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum CustomerReactionType
{
    None,       // Has not ordered yet / invalid
    Reaction1, // Correct food -> happy
    Reaction2, // Shared special food -> dead
    Reaction3  // Any other food -> angry
}

public enum LevelOrderMode
{
    AutoFromSceneName,
    AllFoods,
    Level1_PizzaAndHotdog,
    Level2_BurgerAndPancake
}

[System.Serializable]
public class OrderDefinition
{
    public string orderName;               // Example: "Pizza 1", "Burger 3", "Pancake 2"
    public Image finalIcon;                // Image displayed above the customer's head
    public GameObject reaction1FoodPrefab; // Correct food prefab for this order
}

public class CustomerOrderUI : MonoBehaviour
{
    [Header("This customer's own UI")]
    public Image loadingCircle;

    [Header("All possible order definitions")]
    public OrderDefinition[] orders;

    [Header("Shared Reaction 2 Food")]
    public GameObject reaction2FoodPrefab;
    // Assign ONE special prefab here.
    // This prefab triggers Reaction2 no matter what the customer ordered.

    [Header("Level Order Filtering")]
    public LevelOrderMode levelOrderMode = LevelOrderMode.AutoFromSceneName;

    [Header("Non-Repeating Orders Per Level")]
    public bool preventRepeatedOrdersInLevel = true;

    public bool resetOrderPoolWhenAllFoodsUsed = false;
    // False = no repeats in the level.
    // True = if all allowed foods are used, the pool resets.

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

            if (resetOrderPoolWhenAllFoodsUsed)
            {
                Debug.Log("Resetting order pool because all allowed foods were already used.");
                usedOrderKeys.Clear();
                availableOrderIndexes = GetAvailableOrderIndexes();
            }
            else
            {
                return;
            }
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
                Debug.LogWarning("Order has no correct food prefab assigned: " + order.orderName);
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
            case LevelOrderMode.Level1_PizzaAndHotdog:
                return cleanName.StartsWith("pizza") || cleanName.StartsWith("hotdog");

            case LevelOrderMode.Level2_BurgerAndPancake:
                return cleanName.StartsWith("burger") || cleanName.StartsWith("pancake");

            case LevelOrderMode.AllFoods:
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

        string sceneName = SceneManager.GetActiveScene().name.ToLower();

        if (sceneName.Contains("level1"))
        {
            return LevelOrderMode.Level1_PizzaAndHotdog;
        }

        if (sceneName.Contains("level2"))
        {
            return LevelOrderMode.Level2_BurgerAndPancake;
        }

        Debug.LogWarning("Scene name does not contain Level1 or Level2. Allowing all foods.");
        return LevelOrderMode.AllFoods;
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
            Debug.Log("Matched Reaction2. Shared special food served.");
            return CustomerReactionType.Reaction2;
        }

        if (FoodMatches(servedFood, currentOrder.reaction1FoodPrefab))
        {
            Debug.Log("Matched Reaction1. Correct food served.");
            return CustomerReactionType.Reaction1;
        }

        Debug.Log("No match. Wrong food served.");
        return CustomerReactionType.Reaction3;
    }

    // Backup version, in case another script still calls EvaluateFood(GameObject)
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

        Debug.Log(
            $"Customer wants: {GetOrderName(currentOrder)} / " +
            $"Correct={currentOrder.reaction1FoodPrefab?.name} / " +
            $"Shared Reaction2={reaction2FoodPrefab?.name} / " +
            $"Served={servedFoodPrefab.name}"
        );

        if (GameObjectMatches(servedFoodPrefab, reaction2FoodPrefab))
        {
            Debug.Log("Matched Reaction2. Shared special food served.");
            return CustomerReactionType.Reaction2;
        }

        if (GameObjectMatches(servedFoodPrefab, currentOrder.reaction1FoodPrefab))
        {
            Debug.Log("Matched Reaction1. Correct food served.");
            return CustomerReactionType.Reaction1;
        }

        Debug.Log("No match. Wrong food served.");
        return CustomerReactionType.Reaction3;
    }

    private bool FoodMatches(FoodItem servedFood, GameObject targetPrefab)
    {
        if (servedFood == null || targetPrefab == null)
            return false;

        // Best case: exact prefab reference match
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

        // Do NOT call LeaveRestaurant here anymore.
        // CustomerAI now leaves automatically after the reaction animation duration.
    }

}