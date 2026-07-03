using System.Collections;
using UnityEngine;

public class CustomerWaitArea : MonoBehaviour
{
    [Header("Settings")]
    public float waitTime = 10f;
    public string playerTag = "Player";

    [Header("Order Start Logic")]
    public bool requirePlayerToStartOrder = false;

    private bool playerInside = false;
    private bool customerInside = false;
    private bool isCounting = false;

    private Coroutine waitCoroutine;

    private CustomerOrderUI currentCustomerUI;
    private CustomerAI currentCustomerAI;

    public CustomerOrderUI CurrentCustomerUI
    {
        get
        {
            if (currentCustomerUI == null)
                return null;

            return currentCustomerUI;
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

        CustomerAI foundCustomerAI = other.GetComponentInParent<CustomerAI>();

        if (foundCustomerAI == null)
            return;

        CustomerOrderUI foundCustomerUI = foundCustomerAI.GetComponentInChildren<CustomerOrderUI>(true);

        if (foundCustomerUI == null)
        {
            Debug.LogWarning($"{name}: Customer entered, but no CustomerOrderUI was found.");
            return;
        }

        currentCustomerAI = foundCustomerAI;
        currentCustomerUI = foundCustomerUI;
        customerInside = true;

        Debug.Log($"{name}: Customer entered wait area.");
        Debug.Log($"{name}: Found CustomerOrderUI on {currentCustomerUI.gameObject.name}");

        if (!currentCustomerUI.completed && !isCounting)
        {
            currentCustomerUI.PrepareForWait();
        }

        TryStartCountdown();
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

        customerInside = false;

        Debug.Log($"{name}: Customer collider left wait area.");

        // Important:
        // If the customer already has an order, DO NOT clear them.
        // Their collider may leave slightly after sitting, but they still belong to this table.
        if (currentCustomerUI != null && currentCustomerUI.completed)
        {
            Debug.Log($"{name}: Customer already ordered, keeping customer assigned to this table.");
            return;
        }

        StopCountdownIfRunning();

        currentCustomerAI = null;
        currentCustomerUI = null;
    }

    private void TryStartCountdown()
    {
        if (currentCustomerUI == null)
        {
            Debug.Log($"{name}: Countdown not started yet, waiting for customer.");
            return;
        }

        if (currentCustomerUI.completed)
        {
            Debug.Log($"{name}: Customer already has an order.");
            return;
        }

        if (isCounting)
        {
            return;
        }

        if (requirePlayerToStartOrder && !playerInside)
        {
            Debug.Log($"{name}: Waiting for player before starting order countdown.");
            return;
        }

        Debug.Log($"{name}: Countdown started for customer order.");
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
                isCounting = false;
                waitCoroutine = null;
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
        currentCustomerAI = null;
        currentCustomerUI = null;
        customerInside = false;
        isCounting = false;
        waitCoroutine = null;
    }
}