using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum CustomerReactionType
{
    None,       // 还没点单 / 无效
    Reaction1,  // 正确食物（例：正常汉堡 → 开心）
    Reaction2,  // 特殊食物（例：下毒汉堡 → 倒地）
    Reaction3   // 其他任何食物（例：他要汉堡你给他蛋糕 → 生气）
}

[System.Serializable]
public class OrderDefinition
{
    public string orderName;                 // 给你自己看的名字，比如“Burger”
    public Image finalIcon;                  // 顾客头上显示的那张图（final image）
    public GameObject reaction1FoodPrefab;   // 正常食物 prefab（开心）
    public GameObject reaction2FoodPrefab;   // 下毒版 prefab（倒地）
}

public class CustomerOrderUI : MonoBehaviour
{
    [Header("这个顾客自己的 UI")]
    public Image loadingCircle;      // 顾客头上的圆形进度条

    [Header("所有可能的点单定义（finalImage + 两个食物）")]
    public OrderDefinition[] orders; // 比如 10 个，汉堡/蛋糕/咖啡……

    [HideInInspector] public bool completed = false;
    [HideInInspector] public int currentOrderIndex = -1; // 当前顾客点的是 orders 里的哪一种

    // 需要的话可以用这两个属性拿到他当前点单对应的两个食物
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
            if (currentOrderIndex >= 0 && currentOrderIndex < (orders?.Length ?? 0))
                return orders[currentOrderIndex].reaction2FoodPrefab;
            return null;
        }
    }

    private void Awake()
    {
        PrepareForWait();
    }

    // 倒计时开始前重置 UI 状态
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

    // ⭐ 倒计时结束时调用：随机选择一个点单 + 显示对应 finalImage
    public void ShowRandomOrderIcon()
    {
        if (orders == null || orders.Length == 0)
            return;

        HideAllIcons();

        currentOrderIndex = Random.Range(0, orders.Length);
        var chosen = orders[currentOrderIndex];

        if (chosen != null && chosen.finalIcon != null)
        {
            chosen.finalIcon.gameObject.SetActive(true);
        }

        completed = true;
    }

    // ⭐ 上菜时用：传进来你给他的“食物 prefabID”，返回顾客的反应类型
    public CustomerReactionType EvaluateFood(GameObject servedFoodPrefab)
    {
        if (currentOrderIndex < 0 ||
            orders == null ||
            currentOrderIndex >= orders.Length ||
            servedFoodPrefab == null)
        {
            return CustomerReactionType.None;
        }

        var currentOrder = orders[currentOrderIndex];

        Debug.Log($"顾客当前想要：{currentOrder.orderName} / 正确={currentOrder.reaction1FoodPrefab?.name} 下毒={currentOrder.reaction2FoodPrefab?.name}，你给的是={servedFoodPrefab.name}");

        if (servedFoodPrefab == currentOrder.reaction1FoodPrefab)
        {
            Debug.Log("匹配到 Reaction1！（正确菜）");
            return CustomerReactionType.Reaction1;
        }

        if (servedFoodPrefab == currentOrder.reaction2FoodPrefab)
        {
            Debug.Log("匹配到 Reaction2！（下毒菜）");
            return CustomerReactionType.Reaction2;
        }

        Debug.Log("都匹配不上 → Reaction3（食物错误）");
        return CustomerReactionType.Reaction3;
    }

    public void ResetForNextTime()
    {
        PrepareForWait();
    }

    private void HideAllIcons()
    {
        if (orders == null) return;

        foreach (var order in orders)
        {
            if (order != null && order.finalIcon != null)
            {
                order.finalIcon.gameObject.SetActive(false);
            }
        }
    }
    public void PlayReaction(CustomerReactionType reaction)
    {
        var customer = GetComponentInParent<CustomerAI>();
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

        // After reacting, wait and leave
        StartCoroutine(CustomerLeaveRoutine(reaction, 5f)); //customer leaves after 5 seconds of recieving the food
    }


    private IEnumerator CustomerLeaveRoutine(CustomerReactionType reaction, float delay)
    {
        yield return new WaitForSeconds(delay);

        var customer = GetComponentInParent<CustomerAI>();
        if (customer != null)
        {
            customer.LeaveRestaurant();
        }
    }



}
