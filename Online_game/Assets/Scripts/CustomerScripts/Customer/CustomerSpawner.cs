using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject customerPrefab;
    public float spawnInterval = 4f;

    [Header("Exit Point")]
    public Transform exitPoint;

    [Header("Level 1 Settings")]
    public int level1CustomerLimit = 5;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private float timer = 0f;
    private int spawnedCustomerCount = 0;
    private int maxCustomersThisLevel = -1;

    private void Awake()
    {
        if (exitPoint == null)
        {
            exitPoint = transform;
        }

        maxCustomersThisLevel = GetMaxCustomersForCurrentLevel();

        if (showDebugLogs)
        {
            if (maxCustomersThisLevel < 0)
            {
                Debug.Log("CustomerSpawner: No customer limit for this level.");
            }
            else
            {
                Debug.Log("CustomerSpawner: Max customers this level = " + maxCustomersThisLevel);
            }
        }
    }

    private void Update()
    {
        if (HasReachedCustomerLimit())
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            TrySpawnCustomer();
            timer = 0f;
        }
    }

    private void TrySpawnCustomer()
    {
        if (HasReachedCustomerLimit())
        {
            if (showDebugLogs)
            {
                Debug.Log("CustomerSpawner: Customer limit reached. No more customers will spawn.");
            }

            return;
        }

        if (customerPrefab == null)
        {
            Debug.LogError("CustomerSpawner: Customer prefab is not assigned.");
            return;
        }

        Seat freeSeat = SeatManager.Instance.GetFirstAvailableSeat();

        if (freeSeat == null)
        {
            if (showDebugLogs)
            {
                Debug.Log("No seat available — customer not spawned.");
            }

            return;
        }

        GameObject customerObj = Instantiate(customerPrefab, transform.position, Quaternion.identity);

        CustomerAI customer = customerObj.GetComponent<CustomerAI>();

        if (customer == null)
        {
            Debug.LogError("CustomerSpawner: Spawned customer has no CustomerAI script.");
            Destroy(customerObj);
            return;
        }

        freeSeat.isOccupied = true;

        customer.exitPoint = exitPoint;
        customer.AssignSeat(freeSeat);

        spawnedCustomerCount++;

        if (showDebugLogs)
        {
            if (maxCustomersThisLevel < 0)
            {
                Debug.Log("Customer spawned. Total spawned = " + spawnedCustomerCount);
            }
            else
            {
                Debug.Log("Customer spawned. Total spawned = " + spawnedCustomerCount + " / " + maxCustomersThisLevel);
            }
        }
    }

    private bool HasReachedCustomerLimit()
    {
        if (maxCustomersThisLevel < 0)
        {
            return false;
        }

        return spawnedCustomerCount >= maxCustomersThisLevel;
    }

    private int GetMaxCustomersForCurrentLevel()
    {
        string sceneName = NormalizeName(SceneManager.GetActiveScene().name);

        if (sceneName.Contains("level1"))
        {
            return level1CustomerLimit;
        }

        if (sceneName.Contains("level2"))
        {
            return CountUniqueAllowedOrdersForLevel2();
        }

        // -1 means unlimited for scenes that are not Level1 or Level2
        return -1;
    }

    private int CountUniqueAllowedOrdersForLevel2()
    {
        if (customerPrefab == null)
        {
            Debug.LogError("CustomerSpawner: Customer prefab is not assigned, so Level2 order count cannot be calculated.");
            return 0;
        }

        CustomerOrderUI orderUI = customerPrefab.GetComponentInChildren<CustomerOrderUI>(true);

        if (orderUI == null)
        {
            Debug.LogError("CustomerSpawner: Customer prefab has no CustomerOrderUI, so Level2 order count cannot be calculated.");
            return 0;
        }

        if (orderUI.orders == null || orderUI.orders.Length == 0)
        {
            Debug.LogError("CustomerSpawner: CustomerOrderUI has no orders assigned.");
            return 0;
        }

        HashSet<string> uniqueAllowedOrders = new HashSet<string>();

        foreach (OrderDefinition order in orderUI.orders)
        {
            if (order == null)
            {
                continue;
            }

            string orderName = GetOrderName(order);
            string cleanOrderName = NormalizeName(orderName);

            if (IsLevel2Food(cleanOrderName))
            {
                uniqueAllowedOrders.Add(cleanOrderName);
            }
        }

        if (showDebugLogs)
        {
            Debug.Log("CustomerSpawner: Level2 unique allowed orders = " + uniqueAllowedOrders.Count);
        }

        return uniqueAllowedOrders.Count;
    }

    private string GetOrderName(OrderDefinition order)
    {
        if (order == null)
        {
            return "";
        }

        if (!string.IsNullOrWhiteSpace(order.orderName))
        {
            return order.orderName;
        }

        if (order.reaction1FoodPrefab != null)
        {
            return order.reaction1FoodPrefab.name;
        }

        return "";
    }

    private bool IsLevel2Food(string cleanOrderName)
    {
        return cleanOrderName.StartsWith("burger") ||
               cleanOrderName.StartsWith("pancake");
    }

    private string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "";
        }

        return name
            .ToLower()
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "")
            .Replace("(clone)", "");
    }
}