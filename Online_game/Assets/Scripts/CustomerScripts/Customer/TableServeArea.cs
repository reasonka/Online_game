using System.Collections;
using UnityEngine;

public class TableServeArea : MonoBehaviour
{
    [Header("Center point on the table where food is placed")]
    public Transform platePoint;

    [Header("Connected customer wait area")]
    public CustomerWaitArea waitArea;

    [Header("Serve Area Settings")]
    public bool debugThisTable = true;

    private void Awake()
    {
        if (waitArea == null)
        {
            waitArea = GetComponentInParent<CustomerWaitArea>();
        }

        if (waitArea == null)
        {
            waitArea = GetComponentInChildren<CustomerWaitArea>();
        }

        if (waitArea != null)
        {
            Debug.Log($"{name}: Connected to wait area {waitArea.name}");
        }
        else
        {
            Debug.LogWarning($"{name}: No CustomerWaitArea found. Assign it manually.");
        }
    }

    public void PlaceFood(FoodItem food)
    {
        if (food == null)
            return;

        food.transform.SetParent(null);

        if (platePoint != null)
        {
            food.transform.position = platePoint.position;
            food.transform.rotation = platePoint.rotation;
        }

        Rigidbody rb = food.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (debugThisTable)
        {
            Debug.Log("========== SERVING DEBUG ==========");
            Debug.Log("TableServeArea used: " + name);
            Debug.Log("Connected WaitArea: " + (waitArea != null ? waitArea.name : "NULL"));
            Debug.Log("Placed food: " + food.name);
            Debug.Log("Food Type = " + food.foodType);
            Debug.Log("Food Prefab ID = " + food.foodPrefabId?.name);
        }

        CustomerOrderUI customerOrderUI = waitArea != null ? waitArea.CurrentCustomerUI : null;

        if (customerOrderUI == null)
        {
            Debug.LogWarning($"{name}: No customer assigned to this table. Food cannot trigger reaction.");
            StartCoroutine(DestroyFoodAfterSeconds(food, 3f));
            return;
        }

        if (!customerOrderUI.completed)
        {
            Debug.LogWarning($"{name}: Customer exists, but they have not finished ordering yet.");
            StartCoroutine(DestroyFoodAfterSeconds(food, 3f));
            return;
        }

        Debug.Log($"{name}: Found customer order UI: {customerOrderUI.name}");

        CustomerReactionType reaction = customerOrderUI.EvaluateFood(food);

        switch (reaction)
        {
            case CustomerReactionType.Reaction1:
                Debug.Log("Reaction1: Correct food served, customer is happy.");
                break;

            case CustomerReactionType.Reaction2:
                Debug.Log("Reaction2: Shared special food served, customer collapses.");
                break;

            case CustomerReactionType.Reaction3:
                Debug.Log("Reaction3: Wrong food served, customer is angry.");
                break;

            default:
                Debug.LogWarning("No valid reaction returned.");
                break;
        }

        customerOrderUI.PlayReaction(reaction);

        StartCoroutine(ClearTableCustomerAfterDelay(6f));
        StartCoroutine(DestroyFoodAfterSeconds(food, 3f));
    }

    private IEnumerator ClearTableCustomerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (waitArea != null)
        {
            waitArea.ClearCustomerAfterLeaving();
        }
    }

    private IEnumerator DestroyFoodAfterSeconds(FoodItem food, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (food != null)
        {
            Destroy(food.gameObject);
        }
    }
}