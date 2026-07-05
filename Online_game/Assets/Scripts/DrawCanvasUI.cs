using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Draw panel UI. Only the local player currently in the trigger zone has
/// this panel open, so only their clicks/drags reach OnPointerDown/OnDrag -
/// but every stroke they make gets broadcast by NetworkedDrawBoard so all
/// players see the result on the shared in-world screens.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DrawCanvasUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Setup")]
    public GameObject panelRoot;
    public RawImage drawSurface;
    public NetworkedDrawBoard drawBoard;
    public DrawTriggerZone triggerZone;

    [Header("Brush")]
    public Color brushColor = Color.black;
    public int brushSize = 3;
    public int eraserSize = 14;

    private RectTransform _rect;
    private bool _isDragging;
    private Vector2 _lastUV;

    void Awake()
    {
        _rect = drawSurface.rectTransform;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void Update()
    {
        // Guaranteed way out: since movement is disabled while drawing,
        // the player can't physically walk out of the trigger zone to
        // fire OnTriggerExit. Escape always works regardless of whether
        // the Exit button is correctly wired in the Inspector.
        if (panelRoot != null && panelRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            OnExitButtonPressed();
        }
    }

    public void Open()
    {
        // Bind here (not in Awake) so it's guaranteed drawBoard has already
        // created its texture, regardless of script execution order.
        if (drawSurface.texture == null)
            drawSurface.texture = drawBoard.CanvasTexture;

        if (panelRoot != null) panelRoot.SetActive(true);

        // Force the OS cursor free so the player can actually click/drag
        // on the panel. Some player controllers re-lock the cursor on the
        // next left-click if they're not disabled - make sure this
        // GameObject's DrawTriggerZone has the player's movement script
        // listed in "Scripts To Disable While Drawing" (see setup notes).
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        _isDragging = false;
    }

    public void OnExitButtonPressed()
    {
        triggerZone.ExitDrawMode();
    }

    public void OnClearButtonPressed()
    {
        drawBoard.RequestClear();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle) return;

        if (TryGetUV(eventData, out Vector2 uv))
        {
            _isDragging = true;
            _lastUV = uv;
            bool erasing = eventData.button == PointerEventData.InputButton.Right;
            Color color = erasing ? drawBoard.backgroundColor : brushColor;
            int size = erasing ? eraserSize : brushSize;
            drawBoard.RequestDrawPoint(uv, color, size);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        if (eventData.button == PointerEventData.InputButton.Middle) return;

        if (TryGetUV(eventData, out Vector2 uv))
        {
            bool erasing = eventData.button == PointerEventData.InputButton.Right;
            Color color = erasing ? drawBoard.backgroundColor : brushColor;
            int size = erasing ? eraserSize : brushSize;

            float dist = Vector2.Distance(_lastUV, uv);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist * drawBoard.textureSize / 4f));
            for (int i = 1; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(_lastUV, uv, (float)i / steps);
                drawBoard.RequestDrawPoint(p, color, size);
            }
            _lastUV = uv;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;
    }

    bool TryGetUV(PointerEventData eventData, out Vector2 uv)
    {
        uv = Vector2.zero;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return false;

        Rect rect = _rect.rect;
        float u = (local.x - rect.x) / rect.width;
        float v = (local.y - rect.y) / rect.height;
        uv = new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
        return true;
    }
}