using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerInteraction : MonoBehaviourPun
{
    [Header("References")]
    public PlayerInventory inventory;

    [Header("Input")]
    public KeyCode openStorageKey = KeyCode.E;
    public KeyCode useHeldItemKey = KeyCode.F;
    public KeyCode throwAwayKey = KeyCode.G;

    [Header("Photon")]
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

    private void Start()
    {
        if (inventory == null)
        {
            inventory = GetComponent<PlayerInventory>();
        }

        if (inventory == null)
        {
            LogWarning("PlayerInventory is missing.");
        }
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
            TryUseHeldItem();
        }

        if (Input.GetKeyDown(throwAwayKey))
        {
            TryThrowAwayHeldItem();
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

    private void TryOpenStorage()
    {
        IngredientStorage storage = NearbyStorage;

        if (storage == null)
        {
            Log("No IngredientStorage is nearby.");
            return;
        }

        if (inventory == null)
        {
            LogWarning("PlayerInventory is missing.");
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

    public void TryUseHeldItem()
    {
        if (inventory == null)
        {
            LogWarning("PlayerInventory is missing.");
            return;
        }

        /*
         * 手上为空：
         * 尝试拿起附近桌上的 FinishedFood。
         */
        if (!inventory.HasHeldItem)
        {
            TryPickupFinishedFood();
            return;
        }

        HoldableItem item = inventory.HeldItem;

        if (item == null)
        {
            LogWarning("Held item is null.");
            return;
        }

        /*
         * Ingredient：
         * 优先交给 CookingStation。
         */
        if (item.itemType == HoldableItemType.Ingredient)
        {
            TrySendIngredientToCookingStation(item);
            return;
        }

        /*
         * CookingBase 或 FinishedFood：
         * 放到桌子的 PlacementPoint。
         */
        if (item.itemType == HoldableItemType.CookingBase ||
            item.itemType == HoldableItemType.FinishedFood)
        {
            TryPlaceHeldObjectOnSurface();
            return;
        }

        Log(
            "This item type cannot use F interaction: " +
            item.itemType
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
            surface.TryPickupFinishedFood(inventory);

        if (!pickedUp)
        {
            Log("No FinishedFood was picked up.");
            return;
        }

        Log("Picked up FinishedFood from table.");
    }

    private void TrySendIngredientToCookingStation(
        HoldableItem item)
    {
        CookingStation station =
            NearbyCookingStation;

        if (station == null)
        {
            Log(
                "Holding an Ingredient, but no CookingStation is nearby."
            );

            return;
        }

        string ingredientId =
            item.GetIngredientId();

        if (string.IsNullOrEmpty(ingredientId))
        {
            LogWarning(
                "Held Ingredient has no IngredientId: " +
                item.name
            );

            return;
        }

        bool accepted =
            station.TryAddIngredient(ingredientId);

        if (!accepted)
        {
            Log(
                "CookingStation rejected ingredient: " +
                ingredientId
            );

            return;
        }

        inventory.ConsumeHeldItem();

        Log(
            "Ingredient sent to CookingStation: " +
            ingredientId
        );
    }

    private void TryPlaceHeldObjectOnSurface()
    {
        PlacementSurface surface =
            NearbyPlacementSurface;

        if (surface == null)
        {
            Log("No PlacementSurface is nearby.");
            return;
        }

        bool placed =
            surface.TryPlaceFromInventory(inventory);

        if (!placed)
        {
            Log(
                "Could not place held object on surface."
            );

            return;
        }

        Log("Held object placed on surface.");
    }

    private void TryThrowAwayHeldItem()
    {
        if (inventory == null)
        {
            LogWarning("PlayerInventory is missing.");
            return;
        }

        TrashBin trashBin = NearbyTrashBin;

        if (trashBin == null)
        {
            Log("No TrashBin is nearby.");
            return;
        }

        if (!inventory.HasHeldItem)
        {
            Log("Player has nothing to throw away.");
            return;
        }

        trashBin.TryThrowAway(inventory);
    }

    private void OnTriggerEnter(Collider other)
    {
        IngredientStorage storage =
            FindComponent<IngredientStorage>(other);

        if (storage != null &&
            !nearbyStorages.Contains(storage))
        {
            nearbyStorages.Add(storage);

            Log(
                "Entered Storage zone: " +
                storage.name
            );
        }

        CookingStation station =
            FindComponent<CookingStation>(other);

        if (station != null &&
            !nearbyCookingStations.Contains(station))
        {
            nearbyCookingStations.Add(station);

            Log(
                "Entered CookingStation zone: " +
                station.name
            );
        }

        PlacementSurface surface =
            FindComponent<PlacementSurface>(other);

        if (surface != null &&
            !nearbyPlacementSurfaces.Contains(surface))
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
            !nearbyTrashBins.Contains(trashBin))
        {
            nearbyTrashBins.Add(trashBin);

            Log(
                "Entered TrashBin zone: " +
                trashBin.name
            );
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IngredientStorage storage =
            FindComponent<IngredientStorage>(other);

        if (storage != null)
        {
            nearbyStorages.Remove(storage);

            Log(
                "Exited Storage zone: " +
                storage.name
            );
        }

        CookingStation station =
            FindComponent<CookingStation>(other);

        if (station != null)
        {
            nearbyCookingStations.Remove(station);

            Log(
                "Exited CookingStation zone: " +
                station.name
            );
        }

        PlacementSurface surface =
            FindComponent<PlacementSurface>(other);

        if (surface != null)
        {
            nearbyPlacementSurfaces.Remove(surface);

            Log(
                "Exited PlacementSurface zone: " +
                surface.name
            );
        }

        TrashBin trashBin =
            FindComponent<TrashBin>(other);

        if (trashBin != null)
        {
            nearbyTrashBins.Remove(trashBin);

            Log(
                "Exited TrashBin zone: " +
                trashBin.name
            );
        }
    }

    private T FindComponent<T>(Collider other)
        where T : Component
    {
        if (other == null)
        {
            return null;
        }

        T component = other.GetComponent<T>();

        if (component == null)
        {
            component =
                other.GetComponentInParent<T>();
        }

        if (component == null)
        {
            component =
                other.GetComponentInChildren<T>();
        }

        return component;
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
                "[PlayerInteraction] " + message,
                this
            );
        }
    }

    private void LogWarning(string message)
    {
        if (showDebugLog)
        {
            Debug.LogWarning(
                "[PlayerInteraction] " + message,
                this
            );
        }
    }
}