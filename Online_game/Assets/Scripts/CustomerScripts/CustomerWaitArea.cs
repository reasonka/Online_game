using System.Collections;
using UnityEngine;

public class CustomerWaitArea : MonoBehaviour
{
    [Header("设置")]
    public float waitTime = 10f;
    public string playerTag = "Player";
    public string customerTag = "Customer";

    private bool playerInside = false;
    private bool customerInside = false;
    private bool isCounting = false;
    private Coroutine waitCoroutine;

    private CustomerOrderUI currentCustomerUI;

    // ⭐ 提供给桌子使用：当前这张桌子前的顾客 UI
    public CustomerOrderUI CurrentCustomerUI => currentCustomerUI;

    private void OnTriggerEnter(Collider other)
    {
        // 玩家进入桌前区域
        if (other.CompareTag(playerTag))
        {
            playerInside = true;
            TryStartCountdown();
        }
        // 顾客进入桌前区域
        else if (other.CompareTag(customerTag))
        {
            customerInside = true;

            currentCustomerUI = other.GetComponentInChildren<CustomerOrderUI>();

            if (currentCustomerUI != null && !currentCustomerUI.completed)
            {
                currentCustomerUI.PrepareForWait();
            }

            TryStartCountdown();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 玩家离开
        if (other.CompareTag(playerTag))
        {
            playerInside = false;
            StopCountdownIfRunning();
        }
        // 顾客离开
        else if (other.CompareTag(customerTag))
        {
            customerInside = false;
            StopCountdownIfRunning();
            currentCustomerUI = null;
        }
    }

    private void TryStartCountdown()
    {
        if (playerInside &&
            customerInside &&
            currentCustomerUI != null &&
            !currentCustomerUI.completed &&
            !isCounting)
        {
            waitCoroutine = StartCoroutine(WaitAndShowIcon());
        }
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
            if (!playerInside || !customerInside || currentCustomerUI == null)
            {
                isCounting = false;
                yield break;
            }

            timer += Time.deltaTime;
            float progress = timer / waitTime;

            currentCustomerUI.SetProgress(progress);

            yield return null;
        }

        if (currentCustomerUI != null)
        {
            currentCustomerUI.EndProgress();
            currentCustomerUI.ShowRandomOrderIcon();
        }

        isCounting = false;
    }

}
