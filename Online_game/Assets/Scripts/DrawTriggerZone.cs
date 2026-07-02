using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Collider))]
public class DrawTriggerZone : MonoBehaviour
{
    [Header("References")]
    public DrawCanvasUI drawUI;
    public Camera drawCamera;
    public MonoBehaviour[] scriptsToDisableWhileDrawing;

    private GameObject _playerInZone;

    void OnTriggerEnter(Collider other)
    {
        PhotonView pv = other.GetComponentInParent<PhotonView>();
        if (pv == null || !pv.IsMine) return;

        _playerInZone = other.gameObject;
        EnterDrawMode();
    }

    void OnTriggerExit(Collider other)
    {
        PhotonView pv = other.GetComponentInParent<PhotonView>();
        if (pv == null || !pv.IsMine) return;

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