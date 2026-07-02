using UnityEngine;

public enum HoldableItemType
{
    CookingBase,
    Ingredient,
    FinishedFood,
    Other
}

public class HoldableItem : MonoBehaviour
{
    [Header("Item Type")]
    public HoldableItemType itemType = HoldableItemType.Other;

    [Header("Optional Ingredient")]
    [Tooltip("Ingredient 类型的物体必须拥有 IngredientId。")]
    public IngredientId ingredientIdComponent;

    private Collider[] cachedColliders;
    private bool[] originalColliderStates;

    private Rigidbody[] cachedRigidbodies;
    private bool[] originalKinematicStates;
    private bool[] originalGravityStates;

    private void Awake()
    {
        CachePhysicsComponents();

        if (ingredientIdComponent == null)
        {
            ingredientIdComponent = GetComponent<IngredientId>();

            if (ingredientIdComponent == null)
            {
                ingredientIdComponent = GetComponentInChildren<IngredientId>();
            }
        }
    }

    private void CachePhysicsComponents()
    {
        cachedColliders = GetComponentsInChildren<Collider>(true);
        originalColliderStates = new bool[cachedColliders.Length];

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            originalColliderStates[i] = cachedColliders[i].enabled;
        }

        cachedRigidbodies = GetComponentsInChildren<Rigidbody>(true);
        originalKinematicStates = new bool[cachedRigidbodies.Length];
        originalGravityStates = new bool[cachedRigidbodies.Length];

        for (int i = 0; i < cachedRigidbodies.Length; i++)
        {
            originalKinematicStates[i] = cachedRigidbodies[i].isKinematic;
            originalGravityStates[i] = cachedRigidbodies[i].useGravity;
        }
    }

    public string GetIngredientId()
    {
        if (ingredientIdComponent == null)
        {
            return "";
        }

        return ingredientIdComponent.ingredientId;
    }

    public void SetHeldState(bool isHeld)
    {
        if (cachedColliders == null || cachedRigidbodies == null)
        {
            CachePhysicsComponents();
        }

        if (isHeld)
        {
            // 拿在手上时关闭所有 Collider，
            // 防止食材在手上时自动撞进 CookingStation Trigger。
            foreach (Collider col in cachedColliders)
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }

            foreach (Rigidbody rb in cachedRigidbodies)
            {
                if (rb == null)
                {
                    continue;
                }

                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
                rb.isKinematic = true;
            }
        }
        else
        {
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                {
                    cachedColliders[i].enabled = originalColliderStates[i];
                }
            }

            for (int i = 0; i < cachedRigidbodies.Length; i++)
            {
                if (cachedRigidbodies[i] == null)
                {
                    continue;
                }

                cachedRigidbodies[i].isKinematic = originalKinematicStates[i];
                cachedRigidbodies[i].useGravity = originalGravityStates[i];
            }
        }
    }

    public void SetPlacedState(bool keepFixed)
    {
        SetHeldState(false);

        if (!keepFixed)
        {
            return;
        }

        foreach (Rigidbody rb in cachedRigidbodies)
        {
            if (rb == null)
            {
                continue;
            }

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }
}