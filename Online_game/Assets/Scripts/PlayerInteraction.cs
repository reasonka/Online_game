using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerInteraction : MonoBehaviourPun
{
    [Header("References")]
    public PlayerInventory inventory;

    [Header("Input")]
    public KeyCode interactKey = KeyCode.F;

    [Header("Photon")]
    public bool usePhotonSync = false;

    [Header("Debug")]
    public bool showDebugLog = true;

    private readonly List<CookingStation> nearbyCookingStations =
        new List<CookingStation>();

    private readonly List<PlacementSurface> nearbyPlacementSurfaces =
        new List<PlacementSurface>();

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

        if (Input.GetKeyDown(interactKey))
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

        /*
         * 优先级 1：
         * Ingredient 永远优先交给 CookingStation。
         *
         * 即使下面的桌子已经 occupied，
         * 也不会调用桌子的 TryPlaceFromInventory。
         */
        if (item.itemType == HoldableItemType.Ingredient)
        {
            CookingStation station = NearbyCookingStation;

            if (station == null)
            {
                Log("Holding ingredient, but no CookingStation is nearby.");
                return;
            }

            string ingredientId = item.GetIngredientId();

            if (string.IsNullOrEmpty(ingredientId))
            {
                LogWarning(
                    "Held ingredient has no valid IngredientId: " +
                    item.name
                );
                return;
            }

            bool accepted = station.TryAddIngredient(ingredientId);

            if (accepted)
            {
                Log(
                    "Ingredient accepted by CookingStation: " +
                    ingredientId
                );

                inventory.ConsumeHeldItem();
            }
            else
            {
                Log(
                    "CookingStation did not accept ingredient: " +
                    ingredientId
                );
            }

            return;
        }

        /*
         * 优先级 2：
         * CookingBase 和 FinishedFood 才尝试放到桌面。
         */
        if (item.itemType == HoldableItemType.CookingBase ||
            item.itemType == HoldableItemType.FinishedFood)
        {
            PlacementSurface surface = NearbyPlacementSurface;

            if (surface == null)
            {
                Log("No PlacementSurface is nearby.");
                return;
            }

            bool placed = surface.TryPlaceFromInventory(inventory);

            if (!placed)
            {
                Log(
                    "Could not place object. Surface may be occupied " +
                    "or item type is not allowed."
                );
            }

            return;
        }

        Log("Held item has no supported interaction: " + item.name);
    }

    private void OnTriggerEnter(Collider other)
    {
        CookingStation station =
            other.GetComponent<CookingStation>();

        if (station == null)
        {
            station = other.GetComponentInParent<CookingStation>();
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
            surface = other.GetComponentInParent<PlacementSurface>();
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
        CookingStation station =
            other.GetComponent<CookingStation>();

        if (station == null)
        {
            station = other.GetComponentInParent<CookingStation>();
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
            surface = other.GetComponentInParent<PlacementSurface>();
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
            Debug.Log("[PlayerInteraction] " + message, this);
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