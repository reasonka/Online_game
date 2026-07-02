using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject customerPrefab;
    public float spawnInterval = 4f;

    [Header("Exit Point (defaults to this spawner)")]
    public Transform exitPoint;   // ← NEW (optional)

    private float timer = 0f;

    private void Awake()
    {
        // If exitPoint isn't assigned, use the spawner's transform
        if (exitPoint == null)
            exitPoint = this.transform;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            TrySpawnCustomer();
            timer = 0f;
        }
    }

    private void TrySpawnCustomer()
    {
        Seat freeSeat = SeatManager.Instance.GetFirstAvailableSeat();

        if (freeSeat == null)
        {
            Debug.Log("No seat available — customer not spawned.");
            return;
        }

        // Spawn customer at spawner position
        GameObject customerObj = Instantiate(customerPrefab, transform.position, Quaternion.identity);

        // Assign seat
        CustomerAI customer = customerObj.GetComponent<CustomerAI>();
        customer.AssignSeat(freeSeat);

        // ⭐ NEW: Assign exit point for leaving
        customer.exitPoint = exitPoint;

        // Mark seat as occupied
        freeSeat.isOccupied = true;

        Debug.Log("Customer spawned. Exit point assigned.");
    }
}
