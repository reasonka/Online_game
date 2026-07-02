using System.Collections;
using UnityEngine;

public class TableServeArea : MonoBehaviour
{
    [Header("Center point on the table where food is placed")]
    public Transform platePoint;

    private CustomerWaitArea waitArea;

    private void Awake()
    {
        waitArea = GetComponentInParent<CustomerWaitArea>();
    }

    public void PlaceFood(FoodItem food)
    {
        if (food == null) return;

        // Place the food at the table's serving point
        food.transform.SetParent(null);

        if (platePoint != null)
        {
            food.transform.position = platePoint.position;
            food.transform.rotation = platePoint.rotation;
        }

        var rb = food.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        Debug.Log("Placed food on the table: " + food.name);
        Debug.Log("Food Type = " + food.foodType);
        Debug.Log("Food Prefab ID = " + food.foodPrefabId?.name);

        // Get the current customer from the waiting area
        var customerOrderUI = (waitArea != null) ? waitArea.CurrentCustomerUI : null;

        if (customerOrderUI != null)
        {
            // Pass the prefab ID to the customer order system
            var reaction = customerOrderUI.EvaluateFood(food.foodPrefabId);

            switch (reaction)
            {
                case CustomerReactionType.Reaction1:
                    Debug.Log("Reaction1: Correct food served, customer is happy.");
                    break;

                case CustomerReactionType.Reaction2:
                    Debug.Log("Reaction2: Special food served, customer collapses.");
                    break;

                case CustomerReactionType.Reaction3:
                    Debug.Log("Reaction3: Wrong food served, customer is angry.");
                    break;

                default:
                    Debug.Log("Customer has not ordered yet / invalid serving action.");
                    break;
            }

            customerOrderUI.PlayReaction(reaction);
        }
        else
        {
            Debug.LogWarning("There is currently no customer at this table, or CustomerWaitArea did not detect a customer.");
        }

        StartCoroutine(DestroyFoodAfterSeconds(food, 3f));
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