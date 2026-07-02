using UnityEngine;
using Photon.Pun;

/// <summary>
/// Shared drawing canvas. One instance in the scene, with a PhotonView on the same GameObject.
/// Any client can request a draw point; it gets RPC'd to ALL clients (including the sender)
/// and painted identically onto a local Texture2D that both the inner and outer projector
/// screens reference, so everyone always sees the same image at the same time.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class NetworkedDrawBoard : MonoBehaviourPun
{
    [Header("Canvas Settings")]
    public int textureSize = 512;
    public Color backgroundColor = Color.white;

    [Header("Screens that display the drawing (assign BOTH inner + outer projector mesh renderers)")]
    public Renderer[] targetRenderers;

    private Texture2D _canvasTexture;
    private Color[] _clearColors;

    /// <summary>The live shared canvas texture, so UI scripts can display it (e.g. RawImage.texture).</summary>
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

    /// <summary>Call this locally from the drawer's UI script (uv in 0..1 range).</summary>
    public void RequestDrawPoint(Vector2 uv, Color color, int brushSize)
    {
        photonView.RPC(nameof(RPC_DrawPoint), RpcTarget.All, uv, (Vector4)color, brushSize);
    }

    public void RequestClear()
    {
        photonView.RPC(nameof(RPC_Clear), RpcTarget.All);
    }

    [PunRPC]
    void RPC_DrawPoint(Vector2 uv, Vector4 colorV, int brushSize)
    {
        Color color = colorV;
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

    [PunRPC]
    void RPC_Clear()
    {
        ClearLocal();
    }

    void ClearLocal()
    {
        _canvasTexture.SetPixels(_clearColors);
        _canvasTexture.Apply(false);
    }
}