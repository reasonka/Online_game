using System.Collections.Generic;
using UnityEngine;

public class PlayerCarryPlaceFood : MonoBehaviour
{
    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.E;
    public float pickupRange = 2f;
    public LayerMask interactMask = ~0;

    [Header("Hold Settings")]
    public Transform holdPoint;
    public Vector3 holdLocalPosition = new Vector3(0f, 1.2f, 1f);
    public Vector3 holdLocalRotation = Vector3.zero;

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
            Debug.Log("Pressed E.");

            if (heldFood == null)
            {
                TryPickUpNearestFood();
            }
            else
            {
                TryPlaceFoodOnCurrentTable();
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

    private void TryPlaceFoodOnCurrentTable()
    {
        TableServeArea targetTable = GetClosestNearbyServeArea();

        if (targetTable == null)
        {
            Debug.LogWarning("You are not inside any table serve area.");
            return;
        }

        PlaceHeldFood(targetTable);
    }

    private TableServeArea GetClosestNearbyServeArea()
    {
        nearbyServeAreas.RemoveAll(area => area == null);

        TableServeArea closestArea = null;
        float closestDistance = Mathf.Infinity;

        foreach (TableServeArea area in nearbyServeAreas)
        {
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

        foreach (Collider col in heldFoodColliders)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }

        heldFood.isHeld = false;

        FoodItem foodToPlace = heldFood;

        heldFood = null;
        heldFoodRb = null;
        heldFoodColliders = null;

        table.PlaceFood(foodToPlace);

        Debug.Log("Placed food on table: " + table.name);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}