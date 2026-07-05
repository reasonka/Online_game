using UnityEngine;
using Photon.Pun;

/// <summary>
/// Photon-aware draw trigger. Only reacts to the LOCAL player's avatar
/// entering/exiting - not other clients' networked copies. If no
/// PhotonView is found (e.g. solo testing without Photon connected),
/// it falls back to reacting normally.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DrawTriggerZone : MonoBehaviour
{
    [Header("References")]
    public DrawCanvasUI drawUI;
    public Camera drawCamera;

    [Tooltip("Optional: any extra scripts to disable. Usually leave empty - the player's own movement script is now found automatically at runtime, since it's spawned by Photon and can't be pre-wired in the Editor.")]
    public MonoBehaviour[] scriptsToDisableWhileDrawing;

    [Header("Settings")]
    public string playerTag = "Player";

    [Header("Role Restriction")]
    [Tooltip("Only the player whose CharacterIndex custom property equals this value can draw here. Matches the same property key used by PhotonPlayerLocalSetup.")]
    public int requiredCharacterIndex = 2;
    public string characterIndexPropertyKey = "CharacterIndex";

    private GameObject _playerInZone;
    private BasicPlayerController _playerController;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        PhotonView pv = other.GetComponentInParent<PhotonView>();
        if (pv != null && !pv.IsMine) return;

        // Only let the player assigned the "doodle" role actually open the
        // panel. If the property isn't set yet (e.g. solo testing without
        // roles configured), we allow it through rather than blocking
        // everyone - remove this leniency once role assignment is solid.
        if (pv != null && pv.Owner != null &&
            pv.Owner.CustomProperties.TryGetValue(characterIndexPropertyKey, out object idxObj) &&
            idxObj is int idx && idx != requiredCharacterIndex)
        {
            return;
        }

        _playerInZone = other.gameObject;

        // Grab the LIVE player's own movement script directly at runtime.
        // A pre-wired Inspector reference can't point to this object,
        // since Photon spawns the player clone only once Play mode starts.
        _playerController = other.GetComponentInParent<BasicPlayerController>();

        EnterDrawMode();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        PhotonView pv = other.GetComponentInParent<PhotonView>();
        if (pv != null && !pv.IsMine) return;

        if (_playerInZone == other.gameObject)
            ExitDrawMode();
    }

    void EnterDrawMode()
    {
        if (drawCamera != null) drawCamera.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (_playerController != null)
        {
            _playerController.enabled = false;

            // Force the animator back to Idle. BasicPlayerController.Update()
            // is what normally smooths the "Speed" parameter back to 0 - but
            // since it's disabled here, if she was mid-walk-blend right when
            // drawing started, the parameter would otherwise stay frozen at
            // whatever value it had, making her appear to walk in place.
            if (_playerController.animator != null)
                _playerController.animator.SetFloat(_playerController.speedParameter, 0f);
        }

        foreach (var s in scriptsToDisableWhileDrawing)
            if (s != null) s.enabled = false;

        drawUI.Open();
    }

    public void ExitDrawMode()
    {
        if (drawCamera != null) drawCamera.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (_playerController != null) _playerController.enabled = true;

        foreach (var s in scriptsToDisableWhileDrawing)
            if (s != null) s.enabled = true;

        drawUI.Close();
        _playerInZone = null;
        _playerController = null;
    }
}