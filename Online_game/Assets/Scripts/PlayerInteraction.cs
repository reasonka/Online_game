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
            LogWarning("Inventory reference is missing.");
            return;
        }

        if (!inventory.HasHeldItem)
        {
            Log("Player is not holding anything.");
            return;
        }

        HoldableItem item = inventory.HeldItem;

        // Ingredient 必须优先进入 CookingStation。
        if (item.itemType == HoldableItemType.Ingredient)
        {
            CookingStation station = NearbyCookingStation;

            if (station == null)
            {
                Log(
                    "Holding an Ingredient, but no CookingStation is nearby."
                );

                return;
            }

            string ingredientId = item.GetIngredientId();

            if (string.IsNullOrEmpty(ingredientId))
            {
                LogWarning(
                    "Held Ingredient has no valid IngredientId: " +
                    item.name
                );

                return;
            }

            bool accepted =
                station.TryAddIngredient(ingredientId);

            if (accepted)
            {
                Log(
                    "Ingredient sent to CookingStation: " +
                    ingredientId
                );

                inventory.ConsumeHeldItem();
            }

            return;
        }

        // Food Base 和成品才走桌子的 PlacementPoint。
        if (item.itemType == HoldableItemType.CookingBase ||
            item.itemType == HoldableItemType.FinishedFood)
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
                    "Could not place object on surface."
                );
            }

            return;
        }

        Log(
            "Held item type is not supported: " +
            item.itemType
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        IngredientStorage storage =
            other.GetComponent<IngredientStorage>();

        if (storage == null)
        {
            storage =
                other.GetComponentInParent<IngredientStorage>();
        }

        if (storage != null &&
            !nearbyStorages.Contains(storage))
        {
            nearbyStorages.Add(storage);
            Log("Entered storage zone: " + storage.name);
        }

        CookingStation station =
            other.GetComponent<CookingStation>();

        if (station == null)
        {
            station =
                other.GetComponentInParent<CookingStation>();
        }

        if (station != null &&
            !nearbyCookingStations.Contains(station))
        {
            nearbyCookingStations.Add(station);
            Log("Entered CookingStation zone: " + station.name);
        }

        PlacementSurface surface =
            other.GetComponent<PlacementSurface>();

        if (surface == null)
        {
            surface =
                other.GetComponentInParent<PlacementSurface>();
        }

        if (surface != null &&
            !nearbyPlacementSurfaces.Contains(surface))
        {
            nearbyPlacementSurfaces.Add(surface);
            Log("Entered PlacementSurface zone: " + surface.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IngredientStorage storage =
            other.GetComponent<IngredientStorage>();

        if (storage == null)
        {
            storage =
                other.GetComponentInParent<IngredientStorage>();
        }

        if (storage != null)
        {
            nearbyStorages.Remove(storage);
            Log("Exited storage zone: " + storage.name);
        }

        CookingStation station =
            other.GetComponent<CookingStation>();

        if (station == null)
        {
            station =
                other.GetComponentInParent<CookingStation>();
        }

        if (station != null)
        {
            nearbyCookingStations.Remove(station);
            Log("Exited CookingStation zone: " + station.name);
        }

        PlacementSurface surface =
            other.GetComponent<PlacementSurface>();

        if (surface == null)
        {
            surface =
                other.GetComponentInParent<PlacementSurface>();
        }

        if (surface != null)
        {
            nearbyPlacementSurfaces.Remove(surface);
            Log("Exited PlacementSurface zone: " + surface.name);
        }
    }

    private void CleanNullReferences<T>(List<T> list)
        where T : Object
    {
        for (int i = list.Count - 1; i >= 0; i--)
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