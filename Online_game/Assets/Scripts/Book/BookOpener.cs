using System.Collections.Generic;
using UnityEngine;

public class BookOpener : MonoBehaviour
{
    [Header("Book References")]
    public Book book;
    public HistoricalBookUI historicalBookUI;

    [Header("Book Visuals")]
    [Tooltip("The interactive world-space book canvas/root. Usually your Canvas object.")]
    public GameObject bookCanvasRoot;

    [Tooltip("Optional 3D prop, for example LoreBook_Low. This can stay visible while reading.")]
    public GameObject bookPropAsset;

    public bool keepBookPropVisibleWhileReading = true;

    [Header("Reading Camera")]
    public Camera readingCamera;

    [Tooltip("Optional fallback. If empty, the script will search inside the local player.")]
    public Camera playerCamera;

    [Header("Player Lock")]
    [Tooltip("Optional manual list. Usually you can leave this empty.")]
    public MonoBehaviour[] scriptsToDisableWhileOpen;

    [Tooltip("These scripts will be searched on the local player and disabled while reading.")]
    public string[] playerScriptNamesToDisable =
    {
        "PlayerControllerAidyn",
        "PlayerCarryPlaceFood"
    };

    [Header("Input")]
    public KeyCode closeKey = KeyCode.Return;
    public bool alsoCloseWithEscape = true;

    [Header("Cursor")]
    public bool manageCursor = true;

    public bool IsOpen { get; private set; }

    private GameObject currentReader;
    private Camera currentPlayerCamera;
    private readonly List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();

    private void Start()
    {
        if (book != null)
        {
            book.interactable = false;
        }

        if (historicalBookUI != null)
        {
            historicalBookUI.Close();
        }

        if (bookCanvasRoot != null)
        {
            bookCanvasRoot.SetActive(false);
        }

        if (bookPropAsset != null)
        {
            bookPropAsset.SetActive(true);
        }

        if (readingCamera != null)
        {
            readingCamera.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        bool pressedClose =
            Input.GetKeyDown(closeKey) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            (alsoCloseWithEscape && Input.GetKeyDown(KeyCode.Escape));

        if (pressedClose)
        {
            CloseBook();
        }
    }

    public void OpenBook()
    {
        OpenBook(null);
    }

    public void OpenBook(GameObject reader)
    {
        if (IsOpen)
            return;

        IsOpen = true;
        currentReader = reader;

        if (bookCanvasRoot != null)
        {
            bookCanvasRoot.SetActive(true);
        }

        if (bookPropAsset != null)
        {
            bookPropAsset.SetActive(keepBookPropVisibleWhileReading);
        }

        if (book != null)
        {
            book.interactable = true;
        }

        if (historicalBookUI != null)
        {
            historicalBookUI.Open();
        }

        SwitchToReadingCamera(reader);
        DisablePlayerScripts(reader);

        if (manageCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void CloseBook()
    {
        if (!IsOpen)
            return;

        IsOpen = false;

        if (book != null)
        {
            book.interactable = false;
        }

        if (historicalBookUI != null)
        {
            historicalBookUI.Close();
        }

        if (bookCanvasRoot != null)
        {
            bookCanvasRoot.SetActive(false);
        }

        if (bookPropAsset != null)
        {
            bookPropAsset.SetActive(true);
        }

        SwitchBackToPlayerCamera();
        EnablePlayerScripts();

        if (manageCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        currentReader = null;
    }

    private void SwitchToReadingCamera(GameObject reader)
    {
        if (readingCamera == null)
            return;

        currentPlayerCamera = null;

        if (reader != null)
        {
            currentPlayerCamera = reader.GetComponentInChildren<Camera>(true);
        }

        if (currentPlayerCamera == null)
        {
            currentPlayerCamera = playerCamera;
        }

        if (currentPlayerCamera != null)
        {
            currentPlayerCamera.gameObject.SetActive(false);
        }

        readingCamera.gameObject.SetActive(true);

        if (book != null && book.canvas != null)
        {
            book.canvas.worldCamera = readingCamera;
        }
    }

    private void SwitchBackToPlayerCamera()
    {
        if (readingCamera != null)
        {
            readingCamera.gameObject.SetActive(false);
        }

        if (currentPlayerCamera != null)
        {
            currentPlayerCamera.gameObject.SetActive(true);
        }

        currentPlayerCamera = null;
    }

    private void DisablePlayerScripts(GameObject reader)
    {
        disabledScripts.Clear();

        if (scriptsToDisableWhileOpen != null)
        {
            foreach (MonoBehaviour script in scriptsToDisableWhileOpen)
            {
                if (script != null && script.enabled)
                {
                    script.enabled = false;
                    disabledScripts.Add(script);
                }
            }
        }

        if (reader == null)
            return;

        MonoBehaviour[] allScripts = reader.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour script in allScripts)
        {
            if (script == null)
                continue;

            string scriptName = script.GetType().Name;

            foreach (string targetName in playerScriptNamesToDisable)
            {
                if (scriptName == targetName && script.enabled)
                {
                    script.enabled = false;
                    disabledScripts.Add(script);
                }
            }
        }
    }

    private void EnablePlayerScripts()
    {
        foreach (MonoBehaviour script in disabledScripts)
        {
            if (script != null)
            {
                script.enabled = true;
            }
        }

        disabledScripts.Clear();
    }
}