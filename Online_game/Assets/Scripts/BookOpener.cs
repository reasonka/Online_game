using UnityEngine;

/// <summary>
/// Controls "opening" and "closing" a world-space book that already exists
/// in the scene (e.g. the Book on BookStand > Stand > Canvas > OurBook > Book).
///
/// Opening = freeze the player, enable book interaction, optionally switch
/// to a close-up "reading camera" aimed at the book.
/// Closing (Enter key) = reverse all of that.
/// </summary>
public class BookOpener : MonoBehaviour
{
    [Header("Book Reference")]
    [Tooltip("The existing world-space Book script on the stand (Book (Script) in the Inspector)")]
    public Book book;

    [Header("Closed Book Visual")]
    [Tooltip("The pretty static closed-book model shown when nobody is reading (e.g. LoreBook_Low). Gets hidden while the 2D book is open.")]
    public GameObject closedBookVisual;

    [Tooltip("The root of the interactive 2D book (e.g. BookStand 1, or just its Canvas). Gets shown only while reading.")]
    public GameObject openBookVisual;

    [Header("Reading Camera (optional)")]
    [Tooltip("A camera positioned/angled to frame the book up close. Leave empty to skip camera switching.")]
    public Camera readingCamera;

    [Tooltip("The player's normal gameplay camera. Gets disabled while reading if Reading Camera is set.")]
    public Camera playerCamera;

    [Header("Player Lock")]
    [Tooltip("Drag player movement / mouse-look scripts here to disable them while the book is open")]
    public MonoBehaviour[] scriptsToDisableWhileOpen;

    [Tooltip("Unlock and show the mouse cursor while the book is open (useful for FPS-style controllers)")]
    public bool manageCursor = true;

    public bool IsOpen { get; private set; }

    private void Start()
    {
        // Make sure we start in the "closed" state regardless of scene setup
        if (closedBookVisual != null) closedBookVisual.SetActive(true);
        if (openBookVisual != null) openBookVisual.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            CloseBook();
        }
    }

    public void OpenBook()
    {
        if (IsOpen) return;
        IsOpen = true;

        if (book != null)
            book.interactable = true;

        if (closedBookVisual != null) closedBookVisual.SetActive(false);
        if (openBookVisual != null) openBookVisual.SetActive(true);

        SwitchCamera(toReadingCamera: true);
        SetPlayerScripts(enabledState: false);

        if (manageCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void CloseBook()
    {
        if (!IsOpen) return;
        IsOpen = false;

        if (book != null)
            book.interactable = false;

        if (openBookVisual != null) openBookVisual.SetActive(false);
        if (closedBookVisual != null) closedBookVisual.SetActive(true);

        SwitchCamera(toReadingCamera: false);
        SetPlayerScripts(enabledState: true);

        if (manageCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void SwitchCamera(bool toReadingCamera)
    {
        if (readingCamera == null || playerCamera == null) return;

        readingCamera.gameObject.SetActive(toReadingCamera);
        playerCamera.gameObject.SetActive(!toReadingCamera);
    }

    private void SetPlayerScripts(bool enabledState)
    {
        if (scriptsToDisableWhileOpen == null) return;
        foreach (var script in scriptsToDisableWhileOpen)
        {
            if (script != null)
                script.enabled = enabledState;
        }
    }
}