using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// TEST-ONLY version of DrawCanvasUI, wired to DrawBoardLocal / DrawTriggerZoneLocal
/// instead of the Photon-dependent versions. Same drawing behavior:
/// right-click+drag = pen, left-click+drag = eraser.
///
/// Once Photon is working, switch back to DrawCanvasUI.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DrawCanvasUILocal : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Setup")]
    public GameObject panelRoot;
    public RawImage drawSurface;
    public DrawBoardLocal drawBoard;
    public DrawTriggerZoneLocal triggerZone;

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

    public void Open()
    {
        // Bind here (not in Awake) so it's guaranteed drawBoard has already
        // created its texture, regardless of script execution order.
        if (drawSurface.texture == null)
            drawSurface.texture = drawBoard.CanvasTexture;

        if (panelRoot != null) panelRoot.SetActive(true);
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
            bool erasing = eventData.button == PointerEventData.InputButton.Left;
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
            bool erasing = eventData.button == PointerEventData.InputButton.Left;
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