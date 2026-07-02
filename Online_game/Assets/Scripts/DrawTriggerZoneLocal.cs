using UnityEngine;

/// <summary>
/// TEST-ONLY version of DrawTriggerZone with no Photon dependency.
/// Reacts to any collider entering with tag "Player" instead of checking PhotonView.IsMine.
/// Make sure your test player object has the tag "Player" set in the Inspector.
///
/// Once Photon is working, switch back to DrawTriggerZone.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DrawTriggerZoneLocal : MonoBehaviour
{
    [Header("References")]
    public DrawCanvasUILocal drawUI;
    public Camera drawCamera;
    public MonoBehaviour[] scriptsToDisableWhileDrawing;

    [Header("Test Settings")]
    public string playerTag = "Player";

    private GameObject _playerInZone;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _playerInZone = other.gameObject;
        EnterDrawMode();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (_playerInZone == other.gameObject)
            ExitDrawMode();
    }

    void EnterDrawMode()
    {
        if (drawCamera != null) drawCamera.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        foreach (var s in scriptsToDisableWhileDrawing)
            if (s != null) s.enabled = false;

        drawUI.Open();
    }

    public void ExitDrawMode()
    {
        if (drawCamera != null) drawCamera.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        foreach (var s in scriptsToDisableWhileDrawing)
            if (s != null) s.enabled = true;

        drawUI.Close();
        _playerInZone = null;
    }
}