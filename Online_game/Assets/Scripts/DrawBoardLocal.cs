using UnityEngine;

/// <summary>
/// TEST-ONLY version of NetworkedDrawBoard with no Photon dependency.
/// Paints directly onto the shared texture with no RPC step — lets you verify
/// drawing, erasing, and the inner+outer screens updating together, in a single
/// Play session with no networking set up yet.
///
/// Once Photon (connection + player spawning) is working, switch back to
/// NetworkedDrawBoard and swap the reference on DrawCanvasUI's "Draw Board" field.
/// </summary>
public class DrawBoardLocal : MonoBehaviour
{
    [Header("Canvas Settings")]
    public int textureSize = 512;
    public Color backgroundColor = Color.white;

    [Header("Screens that display the drawing (assign BOTH inner + outer projector mesh renderers)")]
    public Renderer[] targetRenderers;

    private Texture2D _canvasTexture;
    private Color[] _clearColors;

    public Texture2D CanvasTexture => _canvasTexture;

    void Awake()
    {
        _canvasTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        _clearColors = new Color[textureSize * textureSize];
        for (int i = 0; i < _clearColors.Length; i++) _clearColors[i] = backgroundColor;

        ClearLocal();
        ApplyTextureToRenderers();
    }

    void ApplyTextureToRenderers()
    {
        foreach (var r in targetRenderers)
        {
            if (r != null) r.material.mainTexture = _canvasTexture;
        }
    }

    public void RequestDrawPoint(Vector2 uv, Color color, int brushSize)
    {
        int px = Mathf.RoundToInt(uv.x * textureSize);
        int py = Mathf.RoundToInt(uv.y * textureSize);
        int r = Mathf.Max(1, brushSize);

        int minX = Mathf.Max(0, px - r);
        int maxX = Mathf.Min(textureSize - 1, px + r);
        int minY = Mathf.Max(0, py - r);
        int maxY = Mathf.Min(textureSize - 1, py + r);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                int dx = x - px;
                int dy = y - py;
                if (dx * dx + dy * dy > r * r) continue;
                _canvasTexture.SetPixel(x, y, color);
            }
        }
        _canvasTexture.Apply(false);
    }

    public void RequestClear()
    {
        ClearLocal();
    }

    void ClearLocal()
    {
        _canvasTexture.SetPixels(_clearColors);
        _canvasTexture.Apply(false);
    }
}