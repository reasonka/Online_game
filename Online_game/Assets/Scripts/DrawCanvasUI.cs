using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Put this on the RawImage that shows the shared canvas texture inside your popup panel.
/// Handles pointer drawing input locally and forwards points to NetworkedDrawBoard,
/// which syncs them to every client (inner + outer screens both update live).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DrawCanvasUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Setup")]
    public GameObject panelRoot;      // whole popup, toggled by Open/Close
    public RawImage drawSurface;      // RawImage displaying drawBoard's texture
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
        drawSurface.texture = drawBoard.CanvasTexture;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void Open()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        _isDragging = false;
    }

    /// <summary>Hook this up to the Exit button's OnClick.</summary>
    public void OnExitButtonPressed()
    {
        triggerZone.ExitDrawMode();
    }

    /// <summary>Optional: hook up to a Clear button (wipes the whole canvas).</summary>
    public void OnClearButtonPressed()
    {
        drawBoard.RequestClear();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle) return; // ignore middle click

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

            // Interpolate between last and current point so fast drags don't leave gaps
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