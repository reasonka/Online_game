using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ServeAreaTrigger : MonoBehaviour
{
    private TableServeArea tableServeArea;

    private void Awake()
    {
        tableServeArea = GetComponent<TableServeArea>();

        if (tableServeArea == null)
        {
            tableServeArea = GetComponentInParent<TableServeArea>();
        }

        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCarryPlaceFood player = other.GetComponentInParent<PlayerCarryPlaceFood>();

        if (player != null)
        {
            player.RegisterServeArea(tableServeArea);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCarryPlaceFood player = other.GetComponentInParent<PlayerCarryPlaceFood>();

        if (player != null)
        {
            player.UnregisterServeArea(tableServeArea);
        }
    }
}