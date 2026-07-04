using UnityEngine;

/// <summary>
/// Put this on an empty GameObject with a Collider (isTrigger = true)
/// positioned near the book stand (e.g. as a child of the stand, sized
/// to cover the "reading spot" in front of it).
///
/// Flow:
///  - Player walks into the trigger zone.
///  - Prompt appears ("Press B to read").
///  - Player presses B -> book UI opens, player movement disabled.
///  - Player presses Enter (or Esc) -> book UI closes, movement re-enabled.
///  - If the player walks out of the zone while the book is open, it
///    also auto-closes (optional, toggle with closeOnExitZone).
/// </summary>
public class BookStandInteraction : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The book UI/canvas GameObject to show/hide (your 'our book' prefab instance).")]
    public GameObject bookUI;

    [Tooltip("Optional: a UI prompt like 'Press B to read' shown while in range.")]
    public GameObject interactionPrompt;

    [Tooltip("Your player movement/controller script to disable while reading. Leave empty if not needed.")]
    public MonoBehaviour playerMovementScript;

    [Header("Input")]
    public KeyCode openKey = KeyCode.B;
    public KeyCode closeKey = KeyCode.Return;
    public KeyCode altCloseKey = KeyCode.Escape;

    [Header("Behavior")]
    [Tooltip("If true, walking out of the trigger zone while the book is open will also close it.")]
    public bool closeOnExitZone = true;

    private bool playerInRange = false;
    private bool bookOpen = false;

    void Start()
    {
        if (bookUI != null) bookUI.SetActive(false);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }

    void Update()
    {
        if (playerInRange && !bookOpen && Input.GetKeyDown(openKey))
        {
            OpenBook();
        }
        else if (bookOpen && (Input.GetKeyDown(closeKey) || Input.GetKeyDown(altCloseKey)))
        {
            CloseBook();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        if (!bookOpen && interactionPrompt != null)
            interactionPrompt.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        if (bookOpen && closeOnExitZone)
            CloseBook();
    }

    void OpenBook()
    {
        bookOpen = true;

        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (bookUI != null) bookUI.SetActive(true);
        if (playerMovementScript != null) playerMovementScript.enabled = false;

        // Optional: unlock cursor if you're in first-person mode
        // Cursor.lockState = CursorLockMode.None;
        // Cursor.visible = true;
    }

    void CloseBook()
    {
        bookOpen = false;

        if (bookUI != null) bookUI.SetActive(false);
        if (playerMovementScript != null) playerMovementScript.enabled = true;
        if (playerInRange && interactionPrompt != null) interactionPrompt.SetActive(true);

        // Optional: re-lock cursor if first-person
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }
}