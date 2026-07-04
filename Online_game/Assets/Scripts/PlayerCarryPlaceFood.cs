using System.Collections.Generic;
using UnityEngine;

public class PlayerCarryPlaceFood : MonoBehaviour
{
    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.E;
    public KeyCode dropKey = KeyCode.Q;

    public float pickupRange = 2f;
    public LayerMask interactMask = ~0;

    [Header("Hold Settings")]
    public Transform holdPoint;
    public Vector3 holdLocalPosition = new Vector3(0f, 1.2f, 1f);
    public Vector3 holdLocalRotation = Vector3.zero;

    [Header("Drop Settings")]
    public float dropForwardDistance = 1f;
    public float dropUpOffset = 0.5f;
    public float dropPushForce = 1.5f;

    [Header("Serve Rules")]
    public bool requireCustomerWithOrderToPlace = true;
    // If true, the player can only place food on a table that has a customer with an active order.

    private FoodItem heldFood;
    private Rigidbody heldFoodRb;
    private Collider[] heldFoodColliders;

    private readonly List<TableServeArea> nearbyServeAreas = new List<TableServeArea>();

    private void Start()
    {
        if (holdPoint == null)
        {
            GameObject newHoldPoint = new GameObject("FoodHoldPoint");
            newHoldPoint.transform.SetParent(transform);
            newHoldPoint.transform.localPosition = holdLocalPosition;
            newHoldPoint.transform.localRotation = Quaternion.Euler(holdLocalRotation);

            holdPoint = newHoldPoint.transform;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            Debug.Log("Pressed interaction key.");

            if (heldFood == null)
            {
                TryPickUpNearestFood();
            }
            else
            {
                TryPlaceFoodOnBestTable();
            }
        }

        if (Input.GetKeyDown(dropKey))
        {
            if (heldFood != null)
            {
                DropHeldFood();
            }
        }
    }

    public void RegisterServeArea(TableServeArea serveArea)
    {
        if (serveArea == null)
            return;

        if (!nearbyServeAreas.Contains(serveArea))
        {
            nearbyServeAreas.Add(serveArea);
            Debug.Log("Player entered serve area: " + serveArea.name);
        }
    }

    public void UnregisterServeArea(TableServeArea serveArea)
    {
        if (serveArea == null)
            return;

        if (nearbyServeAreas.Contains(serveArea))
        {
            nearbyServeAreas.Remove(serveArea);
            Debug.Log("Player left serve area: " + serveArea.name);
        }
    }

    private void TryPickUpNearestFood()
    {
        FoodItem nearestFood = FindNearestFood();

        if (nearestFood == null)
        {
            Debug.LogWarning("No food found nearby to pick up.");
            return;
        }

        if (!nearestFood.canBePickedUp)
        {
            Debug.LogWarning("This food cannot be picked up: " + nearestFood.name);
            return;
        }

        if (nearestFood.isHeld)
        {
            Debug.LogWarning("This food is already being held: " + nearestFood.name);
            return;
        }

        PickUpFood(nearestFood);
    }

    private FoodItem FindNearestFood()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            pickupRange,
            interactMask,
            QueryTriggerInteraction.Collide
        );

        FoodItem nearestFood = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            FoodItem food = hit.GetComponentInParent<FoodItem>();

            if (food == null)
                continue;

            if (!food.canBePickedUp || food.isHeld)
                continue;

            float distance = Vector3.Distance(transform.position, food.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestFood = food;
            }
        }

        return nearestFood;
    }

    private void PickUpFood(FoodItem food)
    {
        heldFood = food;
        heldFood.isHeld = true;

        heldFoodRb = heldFood.GetComponent<Rigidbody>();
        heldFoodColliders = heldFood.GetComponentsInChildren<Collider>();

        if (heldFoodRb != null)
        {
            heldFoodRb.isKinematic = true;
            heldFoodRb.useGravity = false;
            heldFoodRb.velocity = Vector3.zero;
            heldFoodRb.angularVelocity = Vector3.zero;
        }

        foreach (Collider col in heldFoodColliders)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }

        heldFood.transform.SetParent(holdPoint);
        heldFood.transform.localPosition = Vector3.zero;
        heldFood.transform.localRotation = Quaternion.identity;

        Debug.Log("Picked up food: " + heldFood.name);
    }

    private void TryPlaceFoodOnBestTable()
    {
        TableServeArea targetTable = GetBestNearbyServeArea();

        if (targetTable == null)
        {
            Debug.LogWarning("No valid table nearby. Press Q to drop the food.");
            return;
        }

        if (requireCustomerWithOrderToPlace && !targetTable.HasCustomerWithOrder)
        {
            Debug.LogWarning("Cannot place food here. This table has no customer with an order.");
            return;
        }

        PlaceHeldFood(targetTable);
    }

    private TableServeArea GetBestNearbyServeArea()
    {
        nearbyServeAreas.RemoveAll(area => area == null);

        if (nearbyServeAreas.Count == 0)
            return null;

        TableServeArea bestTableWithCustomer = null;
        float closestCustomerTableDistance = Mathf.Infinity;

        foreach (TableServeArea area in nearbyServeAreas)
        {
            if (area == null)
                continue;

            if (!area.HasCustomerWithOrder)
                continue;

            float distance = Vector3.Distance(transform.position, area.transform.position);

            if (distance < closestCustomerTableDistance)
            {
                closestCustomerTableDistance = distance;
                bestTableWithCustomer = area;
            }
        }

        if (bestTableWithCustomer != null)
        {
            Debug.Log("Selected table with customer/order: " + bestTableWithCustomer.name);
            return bestTableWithCustomer;
        }

        // If placing requires a customer, do NOT return an empty table.
        if (requireCustomerWithOrderToPlace)
        {
            return null;
        }

        // Optional fallback only if requireCustomerWithOrderToPlace is false.
        TableServeArea closestArea = null;
        float closestDistance = Mathf.Infinity;

        foreach (TableServeArea area in nearbyServeAreas)
        {
            if (area == null)
                continue;

            float distance = Vector3.Distance(transform.position, area.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestArea = area;
            }
        }

        return closestArea;
    }

    private void PlaceHeldFood(TableServeArea table)
    {
        if (heldFood == null)
            return;

        EnableHeldFoodPhysics();

        heldFood.isHeld = false;

        FoodItem foodToPlace = heldFood;

        heldFood = null;
        heldFoodRb = null;
        heldFoodColliders = null;

        table.PlaceFood(foodToPlace);

        Debug.Log("Placed food on table: " + table.name);
    }

    private void DropHeldFood()
    {
        if (heldFood == null)
            return;

        Transform foodTransform = heldFood.transform;

        foodTransform.SetParent(null);

        Vector3 dropPosition = transform.position + transform.forward * dropForwardDistance + Vector3.up * dropUpOffset;
        foodTransform.position = dropPosition;

        EnableHeldFoodPhysics();

        if (heldFoodRb != null)
        {
            heldFoodRb.AddForce(transform.forward * dropPushForce, ForceMode.Impulse);
        }

        heldFood.isHeld = false;

        Debug.Log("Dropped food: " + heldFood.name);

        heldFood = null;
        heldFoodRb = null;
        heldFoodColliders = null;
    }

    private void EnableHeldFoodPhysics()
    {
        if (heldFoodColliders != null)
        {
            foreach (Collider col in heldFoodColliders)
            {
                if (col != null)
                {
                    col.enabled = true;
                }
            }
        }

        if (heldFoodRb != null)
        {
            heldFoodRb.isKinematic = false;
            heldFoodRb.useGravity = true;
            heldFoodRb.velocity = Vector3.zero;
            heldFoodRb.angularVelocity = Vector3.zero;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}