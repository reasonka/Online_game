using UnityEngine;
using Photon.Pun;

/// <summary>
/// Networked version of the draw board. Every stroke is applied locally
/// immediately (for zero-lag feedback to the person drawing) and then
/// broadcast via RPC so everyone else's copy of the texture gets painted
/// too - keeping the physical in-world screens in sync for all players.
///
/// IMPORTANT SETUP: this GameObject must be a fixed object already placed
/// in the scene (not spawned via PhotonNetwork.Instantiate), so Photon can
/// register it as a shared "scene object" that every client references by
/// the same PhotonView. It needs a PhotonView component (auto-required).
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

    /// <summary>
    /// Call this when the LOCAL player draws a point. Paints immediately
    /// on this client, then tells every other client to do the same.
    /// </summary>
    public void RequestDrawPoint(Vector2 uv, Color color, int brushSize)
    {
        DrawPointLocal(uv, color, brushSize);

        // Sent as plain floats instead of a Color/Vector4 object - those
        // aren't registered as serializable custom types in this Photon
        // setup, which was throwing "Write failed. Custom type not found"
        // on every single stroke. floats are always natively supported.
        photonView.RPC(nameof(RPC_DrawPoint), RpcTarget.OthersBuffered,
            uv, color.r, color.g, color.b, color.a, brushSize);
    }

    [PunRPC]
    void RPC_DrawPoint(Vector2 uv, float r, float g, float b, float a, int brushSize)
    {
        DrawPointLocal(uv, new Color(r, g, b, a), brushSize);
    }

    void DrawPointLocal(Vector2 uv, Color color, int brushSize)
    {
        int px = Mathf.RoundToInt(uv.x * textureSize);
        int py = Mathf.RoundToInt(uv.y * textureSize);
        int rad = Mathf.Max(1, brushSize);

        int minX = Mathf.Max(0, px - rad);
        int maxX = Mathf.Min(textureSize - 1, px + rad);
        int minY = Mathf.Max(0, py - rad);
        int maxY = Mathf.Min(textureSize - 1, py + rad);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                int dx = x - px;
                int dy = y - py;
                if (dx * dx + dy * dy > rad * rad) continue;
                _canvasTexture.SetPixel(x, y, color);
            }
        }
        _canvasTexture.Apply(false);
    }

    /// <summary>
    /// Call this when the LOCAL player clears the board.
    /// </summary>
    public void RequestClear()
    {
        ClearLocal();

        // Remove previously buffered draw-point RPCs so they don't pile up
        // forever, and so late-joining players don't replay strokes that
        // have since been cleared.
        PhotonNetwork.RemoveRPCs(photonView);

        photonView.RPC(nameof(RPC_Clear), RpcTarget.OthersBuffered);
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