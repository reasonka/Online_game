using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerInteraction : MonoBehaviourPun
{
    [Header("References")]
    public PlayerInventory inventory;

    [Tooltip("角色模型上的 Animator。")]
    public Animator animator;

    [Header("Animator Parameters")]
    [Tooltip("Animator 中控制手持状态的 Bool 参数。")]
    public string isCarryingParameter = "IsCarrying";

    [Tooltip("Animator 中播放举手动画的 Trigger 参数。")]
    public string raiseHandTriggerParameter = "RaiseHand";

    [Header("Input")]
    [Tooltip("打开附近储物柜。")]
    public KeyCode openStorageKey = KeyCode.E;

    [Tooltip("放置、添加食材或拿起成品。")]
    public KeyCode useHeldItemKey = KeyCode.F;

    [Tooltip("靠近垃圾桶时丢弃手上的物体。")]
    public KeyCode throwAwayKey = KeyCode.G;

    [Header("Raise Hand")]
    [Tooltip("手上有物品时，鼠标右键播放举手动画。")]
    public bool enableRaiseHand = true;

    [Tooltip("0 代表鼠标左键，1 代表鼠标右键，2 代表鼠标中键。")]
    public int raiseHandMouseButton = 1;

    [Header("Photon")]
    [Tooltip("本地测试时关闭，正式联网时开启。")]
    public bool usePhotonSync = false;

    [Header("Debug")]
    public bool showDebugLog = true;

    private readonly List<IngredientStorage> nearbyStorages =
        new List<IngredientStorage>();

    private readonly List<CookingStation> nearbyCookingStations =
        new List<CookingStation>();

    private readonly List<PlacementSurface> nearbyPlacementSurfaces =
        new List<PlacementSurface>();

    private readonly List<TrashBin> nearbyTrashBins =
        new List<TrashBin>();

    private IngredientStorage NearbyStorage
    {
        get
        {
            CleanNullReferences(nearbyStorages);

            if (nearbyStorages.Count == 0)
            {
                return null;
            }

            return nearbyStorages[
                nearbyStorages.Count - 1
            ];
        }
    }

    private CookingStation NearbyCookingStation
    {
        get
        {
            CleanNullReferences(
                nearbyCookingStations
            );

            if (nearbyCookingStations.Count == 0)
            {
                return null;
            }

            return nearbyCookingStations[
                nearbyCookingStations.Count - 1
            ];
        }
    }

    private PlacementSurface NearbyPlacementSurface
    {
        get
        {
            CleanNullReferences(
                nearbyPlacementSurfaces
            );

            if (nearbyPlacementSurfaces.Count == 0)
            {
                return null;
            }

            return nearbyPlacementSurfaces[
                nearbyPlacementSurfaces.Count - 1
            ];
        }
    }

    private TrashBin NearbyTrashBin
    {
        get
        {
            CleanNullReferences(nearbyTrashBins);

            if (nearbyTrashBins.Count == 0)
            {
                return null;
            }

            return nearbyTrashBins[
                nearbyTrashBins.Count - 1
            ];
        }
    }

    private void Awake()
    {
        if (inventory == null)
        {
            inventory =
                GetComponent<PlayerInventory>();
        }

        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        UpdateAnimatorCarryingState();
    }

    private void Update()
    {
        if (!CanUseLocalInput())
        {
            return;
        }

        if (Input.GetKeyDown(openStorageKey))
        {
            TryOpenStorage();
        }

        if (Input.GetKeyDown(useHeldItemKey))
        {
            TryUseInteraction();
        }

        if (Input.GetKeyDown(throwAwayKey))
        {
            TryThrowAway();
        }

        if (enableRaiseHand &&
            Input.GetMouseButtonDown(
                raiseHandMouseButton
            ))
        {
            TryRaiseHand();
        }
    }

    private bool CanUseLocalInput()
    {
        if (!usePhotonSync)
        {
            return true;
        }

        if (!PhotonNetwork.IsConnected)
        {
            return true;
        }

        return photonView.IsMine;
    }

    private void TryRaiseHand()
    {
        if (inventory == null)
        {
            LogWarning(
                "Cannot raise hand because PlayerInventory is missing."
            );

            return;
        }

        /*
         * 只有玩家正在拿东西时才允许播放举手动画。
         */
        if (!inventory.HasHeldItem)
        {
            Log(
                "Cannot raise hand because the player is not carrying anything."
            );

            return;
        }

        if (animator == null)
        {
            LogWarning(
                "Cannot raise hand because Animator is missing."
            );

            return;
        }

        /*
         * 保证 Animator 当前的手持状态正确。
         */
        animator.SetBool(
            isCarryingParameter,
            true
        );

        /*
         * 防止快速点击右键导致旧 Trigger 残留。
         */
        animator.ResetTrigger(
            raiseHandTriggerParameter
        );

        animator.SetTrigger(
            raiseHandTriggerParameter
        );

        Log("Raise-hand animation triggered.");
    }

    private void TryOpenStorage()
    {
        if (inventory == null)
        {
            LogWarning(
                "PlayerInventory is missing."
            );

            return;
        }

        IngredientStorage storage =
            NearbyStorage;

        if (storage == null)
        {
            Log(
                "No IngredientStorage is nearby."
            );

            return;
        }

        if (inventory.HasHeldItem)
        {
            Log(
                "Cannot open storage because hand is occupied by: " +
                inventory.HeldItem.name
            );

            return;
        }

        storage.OpenStorage(inventory);
    }

    private void TryUseInteraction()
    {
        if (inventory == null)
        {
            LogWarning(
                "PlayerInventory is missing."
            );

            return;
        }

        /*
         * 手上为空：
         * 尝试从附近桌子拿起 FinishedFood。
         */
        if (!inventory.HasHeldItem)
        {
            TryPickupFinishedFood();
            return;
        }

        HoldableItem heldItem =
            inventory.HeldItem;

        if (heldItem == null)
        {
            LogWarning("HeldItem is null.");
            return;
        }

        /*
         * Ingredient：
         * 添加到附近 CookingStation。
         */
        if (heldItem.itemType ==
            HoldableItemType.Ingredient)
        {
            TryAddIngredient(heldItem);
            return;
        }

        /*
         * CookingBase 或 FinishedFood：
         * 放到附近 PlacementSurface。
         */
        if (heldItem.itemType ==
                HoldableItemType.CookingBase ||
            heldItem.itemType ==
                HoldableItemType.FinishedFood)
        {
            TryPlaceOnSurface();
            return;
        }

        Log(
            "This held item type has no F interaction: " +
            heldItem.itemType
        );
    }

    private void TryPlaceOnSurface()
    {
        PlacementSurface surface =
            NearbyPlacementSurface;

        if (surface == null)
        {
            Log(
                "No PlacementSurface is nearby."
            );

            return;
        }

        bool placed =
            surface.TryPlaceFromInventory(
                inventory
            );

        if (!placed)
        {
            Log(
                "Could not place held item on surface."
            );

            return;
        }

        UpdateAnimatorCarryingState();

        Log("Held item placed on surface.");
    }

    private void TryAddIngredient(
        HoldableItem heldItem)
    {
        CookingStation station =
            NearbyCookingStation;

        if (station == null)
        {
            Log(
                "Holding Ingredient, but no CookingStation is nearby."
            );

            return;
        }

        string ingredientId =
            heldItem.GetIngredientId();

        if (string.IsNullOrEmpty(
                ingredientId))
        {
            LogWarning(
                "Held Ingredient has no valid IngredientId: " +
                heldItem.name
            );

            return;
        }

        bool accepted =
            station.TryAddIngredient(
                ingredientId
            );

        if (!accepted)
        {
            Log(
                "CookingStation rejected Ingredient: " +
                ingredientId
            );

            return;
        }

        inventory.ConsumeHeldItem();

        UpdateAnimatorCarryingState();

        Log(
            "Ingredient added to CookingStation: " +
            ingredientId
        );
    }

    private void TryPickupFinishedFood()
    {
        PlacementSurface surface =
            NearbyPlacementSurface;

        if (surface == null)
        {
            Log(
                "Hand is empty, but no PlacementSurface is nearby."
            );

            return;
        }

        bool pickedUp =
            surface.TryPickupFinishedFood(
                inventory
            );

        if (!pickedUp)
        {
            Log(
                "There is no FinishedFood available to pick up."
            );

            return;
        }

        UpdateAnimatorCarryingState();

        Log(
            "Picked up FinishedFood from surface."
        );
    }

    private void TryThrowAway()
    {
        if (inventory == null)
        {
            LogWarning(
                "PlayerInventory is missing."
            );

            return;
        }

        if (!inventory.HasHeldItem)
        {
            Log(
                "Player has nothing to throw away."
            );

            return;
        }

        TrashBin trashBin =
            NearbyTrashBin;

        if (trashBin == null)
        {
            Log(
                "No TrashBin is nearby."
            );

            return;
        }

        bool thrownAway =
            trashBin.TryThrowAway(
                inventory
            );

        if (!thrownAway)
        {
            Log(
                "Throw-away interaction failed."
            );

            return;
        }

        UpdateAnimatorCarryingState();

        Log("Held item thrown away.");
    }

    private void UpdateAnimatorCarryingState()
    {
        if (animator == null ||
            inventory == null)
        {
            return;
        }

        animator.SetBool(
            isCarryingParameter,
            inventory.HasHeldItem
        );
    }

    private void OnTriggerEnter(
        Collider other)
    {
        IngredientStorage storage =
            FindComponent<IngredientStorage>(
                other
            );

        if (storage != null &&
            !nearbyStorages.Contains(storage))
        {
            nearbyStorages.Add(storage);

            Log(
                "Entered Storage zone: " +
                storage.name
            );
        }

        CookingStation cookingStation =
            FindComponent<CookingStation>(
                other
            );

        if (cookingStation != null &&
            !nearbyCookingStations.Contains(
                cookingStation
            ))
        {
            nearbyCookingStations.Add(
                cookingStation
            );

            Log(
                "Entered CookingStation zone: " +
                cookingStation.name
            );
        }

        PlacementSurface surface =
            FindComponent<PlacementSurface>(
                other
            );

        if (surface != null &&
            !nearbyPlacementSurfaces.Contains(
                surface
            ))
        {
            nearbyPlacementSurfaces.Add(surface);

            Log(
                "Entered PlacementSurface zone: " +
                surface.name
            );
        }

        TrashBin trashBin =
            FindComponent<TrashBin>(other);

        if (trashBin != null &&
            !nearbyTrashBins.Contains(
                trashBin
            ))
        {
            nearbyTrashBins.Add(trashBin);

            Log(
                "Entered TrashBin zone: " +
                trashBin.name
            );
        }
    }

    private void OnTriggerExit(
        Collider other)
    {
        IngredientStorage storage =
            FindComponent<IngredientStorage>(
                other
            );

        if (storage != null)
        {
            nearbyStorages.Remove(storage);
        }

        CookingStation cookingStation =
            FindComponent<CookingStation>(
                other
            );

        if (cookingStation != null)
        {
            nearbyCookingStations.Remove(
                cookingStation
            );
        }

        PlacementSurface surface =
            FindComponent<PlacementSurface>(
                other
            );

        if (surface != null)
        {
            nearbyPlacementSurfaces.Remove(
                surface
            );
        }

        TrashBin trashBin =
            FindComponent<TrashBin>(other);

        if (trashBin != null)
        {
            nearbyTrashBins.Remove(trashBin);
        }
    }

    private T FindComponent<T>(
        Collider other)
        where T : Component
    {
        if (other == null)
        {
            return null;
        }

        T result =
            other.GetComponent<T>();

        if (result == null)
        {
            result =
                other.GetComponentInParent<T>();
        }

        if (result == null)
        {
            result =
                other.GetComponentInChildren<T>();
        }

        return result;
    }

    private void CleanNullReferences<T>(
        List<T> list)
        where T : Object
    {
        for (int i = list.Count - 1;
             i >= 0;
             i--)
        {
            if (list[i] == null)
            {
                list.RemoveAt(i);
            }
        }
    }

    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log(
                "[PlayerInteraction] " +
                message,
                this
            );
        }
    }

    private void LogWarning(string message)
    {
        if (showDebugLog)
        {
            Debug.LogWarning(
                "[PlayerInteraction] " +
                message,
                this
            );
        }
    }
}