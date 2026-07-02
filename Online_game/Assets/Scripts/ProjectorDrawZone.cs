using UnityEngine;

// Attach to the "inner side" trigger zone in front of the projector -
// the spot the mute player stands in to draw.
// No networking here at all: the other two players never need a popup,
// they just look at the physical screen (which UIDrawNetworkSync keeps in sync).
[RequireComponent(typeof(Collider))]
public class ProjectorDrawZone : MonoBehaviour
{
    [Header("References")]
    public GameObject drawingPanelUI; // the DrawingPanel canvas (parent of DrawSurface)

    [Header("Role")]
    // Wire this up to whatever assigns roles at match start
    // (e.g. read from PhotonNetwork.LocalPlayer.CustomProperties["role"]).
    public bool localPlayerIsMute;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!localPlayerIsMute) return;
        if (!IsLocalPlayer(other)) return;

        SetDrawMode(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!localPlayerIsMute) return;
        if (!IsLocalPlayer(other)) return;

        SetDrawMode(false);
    }

    void SetDrawMode(bool on)
    {
        if (drawingPanelUI != null)
            drawingPanelUI.SetActive(on);

        // Free the cursor to paint, relock it for normal camera look on exit.
        Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = on;
    }

    bool IsLocalPlayer(Collider other)
    {
        // Swap this for however you tag your player object, e.g.:
        // var pv = other.GetComponentInParent<PhotonView>();
        // return pv != null && pv.IsMine;
        return other.CompareTag("Player");
    }
}