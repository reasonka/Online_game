using UnityEngine;

/// <summary>
/// Put this on an empty GameObject with a Collider (Is Trigger = ON)
/// placed around the book stand. When the Player enters, it tells
/// BookOpener to open the book UI.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BookTriggerZone : MonoBehaviour
{
    [Tooltip("Reference to the BookOpener that shows/hides the 2D book UI")]
    public BookOpener bookOpener;

    [Tooltip("Tag used to identify the player object")]
    public string playerTag = "Player";

    private void Reset()
    {
        // Make sure the collider is always a trigger by default
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (bookOpener == null)
        {
            Debug.LogWarning("BookTriggerZone: bookOpener reference not set.");
            return;
        }

        bookOpener.OpenBook();
    }
}