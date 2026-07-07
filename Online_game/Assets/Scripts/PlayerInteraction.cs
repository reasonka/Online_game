using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerInteraction : MonoBehaviourPun
{
    [Header("References")]
    public PlayerInventory inventory;

    [Tooltip("��ɫģ���ϵ� Animator��")]
    public Animator animator;

    [Header("Animator Parameters")]
    [Tooltip("Animator �п����ֳ�״̬�� Bool ������")]
    public string isCarryingParameter = "IsCarrying";

    [Tooltip("Animator �в��ž��ֶ����� Trigger ������")]
    public string raiseHandTriggerParameter = "RaiseHand";

    [Header("Input")]
    [Tooltip("���á�����ʳ�Ļ������Ʒ��")]
    public KeyCode useHeldItemKey = KeyCode.F;

    [Tooltip("��������Ͱʱ�������ϵ����塣")]
    public KeyCode throwAwayKey = KeyCode.G;

    [Header("Automatic Storage UI")]
    [Tooltip("���봢��� Trigger ʱ�Զ��� UI��")]
    public bool autoOpenStorage = true;

    [Tooltip("�뿪����� Trigger ʱ�Զ��ر� UI��")]
    public bool autoCloseStorage = true;

    [Header("Raise Hand")]
    [Tooltip("��������Ʒʱ������Ҽ����ž��ֶ�����")]
    public bool enableRaiseHand = true;

    [Tooltip("0 ����������1 ������Ҽ���2 ������м���")]
    public int raiseHandMouseButton = 1;

    [Header("Photon")]
    [Tooltip("���ز���ʱ�رգ���ʽ����ʱ������")]
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

    /*
     * ��¼ÿ�� Storage ��ǰ�����˶��ٸ� Collider��
     * ��ֹһ�������ж�� Trigger/Collider ʱ��
     * �뿪����һ�� Collider �ʹ���عر� UI��
     */
    private readonly Dictionary<IngredientStorage, int> storageOverlapCounts =
        new Dictionary<IngredientStorage, int>();

    private IngredientStorage openedStorage;

    private CookingStation NearbyCookingStation
    {
        get
        {
            CleanNullReferences(nearbyCookingStations);

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
            CleanNullReferences(nearbyPlacementSurfaces);

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
            inventory = GetComponent<PlayerInventory>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        UpdateAnimatorCarryingState();
    }

    private void OnDisable()
    {
        CloseOpenedStorage();

        nearbyStorages.Clear();
        nearbyCookingStations.Clear();
        nearbyPlacementSurfaces.Clear();
        nearbyTrashBins.Clear();
        storageOverlapCounts.Clear();
    }

    private void Update()
    {
        if (!CanUseLocalInput())
        {
            return;
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
            Input.GetMouseButtonDown(raiseHandMouseButton))
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

        animator.SetBool(
            isCarryingParameter,
            true
        );

        animator.ResetTrigger(
            raiseHandTriggerParameter
        );

        animator.SetTrigger(
            raiseHandTriggerParameter
        );

        SFXManager.Instance?.PlayWaveFood();

        Log("Raise-hand animation triggered.");
    }

    private void TryAutoOpenStorage(
        IngredientStorage storage)
    {
        if (!autoOpenStorage)
        {
            return;
        }

        if (!CanUseLocalInput())
        {
            return;
        }

        if (storage == null)
        {
            return;
        }

        if (inventory == null)
        {
            LogWarning(
                "Cannot open Storage because PlayerInventory is missing."
            );

            return;
        }

        /*
         * ����ԭ�������ƣ�
         * �����ж���ʱ���򿪴����
         */
        if (inventory.HasHeldItem)
        {
            Log(
                "Storage UI was not opened because the hand is occupied by: " +
                inventory.HeldItem.name
            );

            return;
        }

        if (openedStorage == storage)
        {
            return;
        }

        /*
         * ��һ������� Trigger ֱ�ӽ�����һ������� Trigger ʱ��
         * �ȹر�ԭ���� UI���ٴ��µġ�
         */
        if (openedStorage != null)
        {
            CloseOpenedStorage();
        }

        openedStorage = storage;
        openedStorage.OpenStorage(inventory);

        Log(
            "Storage UI opened automatically: " +
            storage.name
        );
    }

    private void TryAutoCloseStorage(
        IngredientStorage storage)
    {
        if (!autoCloseStorage)
        {
            return;
        }

        if (storage == null)
        {
            return;
        }

        if (openedStorage != storage)
        {
            return;
        }

        CloseOpenedStorage();
    }

    private void CloseOpenedStorage()
    {
        if (openedStorage == null)
        {
            return;
        }

        IngredientStorage storageToClose =
            openedStorage;

        openedStorage = null;

        if (storageToClose.selectionUI != null)
        {
            storageToClose.selectionUI.CloseStorage();
        }
        else
        {
            LogWarning(
                "Cannot close Storage UI because Selection UI is missing on: " +
                storageToClose.name
            );
        }

        Log(
            "Storage UI closed automatically: " +
            storageToClose.name
        );
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
         * ����Ϊ��ʱ��
         * �������𸽽����ϵ� FinishedFood��
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
         * Ingredient��
         * ���ӵ����� CookingStation��
         */
        if (heldItem.itemType ==
            HoldableItemType.Ingredient)
        {
            TryAddIngredient(heldItem);
            return;
        }

        /*
         * CookingBase �� FinishedFood��
         * �ŵ����� PlacementSurface��
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

        SFXManager.Instance?.PlayAddIngredient();

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

        SFXManager.Instance?.PlayPickupFood();

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

        SFXManager.Instance?.PlayDropFood();

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

        if (storage != null)
        {
            RegisterStorageEnter(storage);
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
            RegisterStorageExit(storage);
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

    private void RegisterStorageEnter(
        IngredientStorage storage)
    {
        if (storage == null)
        {
            return;
        }

        if (!storageOverlapCounts.ContainsKey(
                storage))
        {
            storageOverlapCounts[storage] = 0;
        }

        storageOverlapCounts[storage]++;

        /*
         * ֻ�е�һ�ν���� Storage ʱ�ż����б����� UI��
         */
        if (storageOverlapCounts[storage] > 1)
        {
            return;
        }

        if (!nearbyStorages.Contains(storage))
        {
            nearbyStorages.Add(storage);
        }

        Log(
            "Entered Storage zone: " +
            storage.name
        );

        TryAutoOpenStorage(storage);
    }

    private void RegisterStorageExit(
        IngredientStorage storage)
    {
        if (storage == null)
        {
            return;
        }

        if (!storageOverlapCounts.ContainsKey(
                storage))
        {
            nearbyStorages.Remove(storage);
            TryAutoCloseStorage(storage);
            return;
        }

        storageOverlapCounts[storage]--;

        /*
         * ��Ȼ��� Storage ������ Collider �ص���
         * ���Բ��ر� UI��
         */
        if (storageOverlapCounts[storage] > 0)
        {
            return;
        }

        storageOverlapCounts.Remove(storage);
        nearbyStorages.Remove(storage);

        Log(
            "Exited Storage zone: " +
            storage.name
        );

        TryAutoCloseStorage(storage);
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