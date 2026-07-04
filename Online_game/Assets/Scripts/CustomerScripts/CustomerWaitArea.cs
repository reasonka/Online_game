using System.Collections;
using UnityEngine;

public class CustomerWaitArea : MonoBehaviour
{
    [Header("Table Seat")]
    public Seat tableSeat;
    // Assign the Seat/SeatPosition that belongs to THIS table.
    // This prevents customers walking past from being locked to the wrong table.

    [Header("Settings")]
    public float waitTime = 10f;
    public string playerTag = "Player";

    [Header("Order Start Logic")]
    public bool requirePlayerToStartOrder = false;

    private bool playerInside = false;
    private bool isCounting = false;

    private Coroutine waitCoroutine;

    private CustomerOrderUI currentCustomerUI;
    private CustomerAI currentCustomerAI;

    public CustomerOrderUI CurrentCustomerUI => currentCustomerUI;

    public bool HasAssignedCustomer => currentCustomerUI != null;

    public bool HasCompletedOrder
    {
        get
        {
            return currentCustomerUI != null && currentCustomerUI.completed;
        }
    }

    private void Awake()
    {
        if (tableSeat == null)
        {
            tableSeat = GetComponentInChildren<Seat>();

            if (tableSeat == null)
            {
                tableSeat = GetComponentInParent<Seat>();
            }
        }

        if (tableSeat == null)
        {
            Debug.LogWarning($"{name}: No tableSeat assigned. Assign the correct Seat manually.");
        }
        else
        {
            Debug.Log($"{name}: Table seat assigned = {tableSeat.name}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInside = true;
            Debug.Log($"{name}: Player entered wait area.");
            TryStartCountdown();
            return;
        }

        TryAssignCustomerFromCollider(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Important:
        // Customer may enter the trigger before sitting.
        // OnTriggerStay keeps checking until they actually sit.
        TryAssignCustomerFromCollider(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInside = false;
            Debug.Log($"{name}: Player left wait area.");

            if (requirePlayerToStartOrder)
            {
                StopCountdownIfRunning();
            }

            return;
        }

        CustomerAI exitingCustomerAI = other.GetComponentInParent<CustomerAI>();

        if (exitingCustomerAI == null)
            return;

        if (exitingCustomerAI != currentCustomerAI)
            return;

        // Do not clear here.
        // The customer can slightly leave the trigger after snapping to the chair.
        Debug.Log($"{name}: Assigned customer collider exited, but customer stays locked to this table.");
    }

    private void TryAssignCustomerFromCollider(Collider other)
    {
        if (other == null)
            return;

        CustomerAI foundCustomerAI = other.GetComponentInParent<CustomerAI>();

        if (foundCustomerAI == null)
            return;

        if (foundCustomerAI.IsLeaving)
            return;

        // If this table already has this customer, just continue countdown logic.
        if (currentCustomerAI == foundCustomerAI)
        {
            TryStartCountdown();
            return;
        }

        // If this table already has another customer, ignore this one.
        if (currentCustomerAI != null && currentCustomerAI != foundCustomerAI)
        {
            return;
        }

        // Do not lock customers while they are still walking through the trigger.
        if (!foundCustomerAI.IsSitting)
        {
            return;
        }

        // If tableSeat is assigned, only accept the customer assigned to THIS table's seat.
        if (tableSeat != null && foundCustomerAI.AssignedSeat != tableSeat)
        {
            Debug.Log($"{name}: Ignored {foundCustomerAI.name}. They are sitting, but assigned to another table.");
            return;
        }

        CustomerOrderUI foundCustomerUI = foundCustomerAI.GetComponentInChildren<CustomerOrderUI>(true);

        if (foundCustomerUI == null)
        {
            Debug.LogWarning($"{name}: Sitting customer found, but no CustomerOrderUI was found.");
            return;
        }

        currentCustomerAI = foundCustomerAI;
        currentCustomerUI = foundCustomerUI;

        Debug.Log($"{name}: Customer locked after sitting.");
        Debug.Log($"{name}: Locked customer = {currentCustomerAI.name}");
        Debug.Log($"{name}: Found CustomerOrderUI on {currentCustomerUI.gameObject.name}");

        if (!currentCustomerUI.completed && !isCounting)
        {
            currentCustomerUI.PrepareForWait();
        }

        TryStartCountdown();
    }

    private void TryStartCountdown()
    {
        if (currentCustomerUI == null)
            return;

        if (currentCustomerUI.completed)
            return;

        if (isCounting)
            return;

        if (requirePlayerToStartOrder && !playerInside)
        {
            Debug.Log($"{name}: Waiting for player before starting countdown.");
            return;
        }

        Debug.Log($"{name}: Countdown started.");
        waitCoroutine = StartCoroutine(WaitAndShowIcon());
    }

    private void StopCountdownIfRunning()
    {
        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }

        if (currentCustomerUI != null && !currentCustomerUI.completed)
        {
            currentCustomerUI.PrepareForWait();
        }

        isCounting = false;
    }

    private IEnumerator WaitAndShowIcon()
    {
        isCounting = true;

        float timer = 0f;

        while (timer < waitTime)
        {
            if (currentCustomerUI == null)
            {
                Debug.LogWarning($"{name}: Countdown stopped because CustomerOrderUI became null.");
                isCounting = false;
                waitCoroutine = null;
                yield break;
            }

            if (requirePlayerToStartOrder && !playerInside)
            {
                Debug.Log($"{name}: Countdown stopped because player left.");
                StopCountdownIfRunning();
                yield break;
            }

            timer += Time.deltaTime;
            currentCustomerUI.SetProgress(timer / waitTime);

            yield return null;
        }

        if (currentCustomerUI != null)
        {
            currentCustomerUI.EndProgress();
            currentCustomerUI.ShowRandomOrderIcon();
        }

        isCounting = false;
        waitCoroutine = null;
    }

    public void ClearCustomerAfterLeaving()
    {
        Debug.Log($"{name}: Clearing customer from table.");

        currentCustomerAI = null;
        currentCustomerUI = null;

        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }

        isCounting = false;
    }
}