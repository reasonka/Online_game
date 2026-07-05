using UnityEngine;

/// <summary>
/// Trigger zone for the drawing minigame. Purely handles opening/closing the
/// draw UI and pausing player control while drawing — no movement or animation
/// logic lives here. Disables BasicPlayerController on enter (so WASD, mouse
/// look, and animation all pause) and re-enables it on exit.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DrawTriggerZonePlayer : MonoBehaviour
{
    [Header("References")]
    public DrawCanvasUILocal drawUI;          // the draw UI controller (unrelated to movement)
    public BasicPlayerController playerController; // the player's movement/look script to pause

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
        if (playerController != null)
        {
            playerController.SetCursorLocked(false);
            playerController.enabled = false;
        }

        if (drawUI != null) drawUI.Open();
    }

    /// <summary>Also called by the draw UI's Exit button.</summary>
    public void ExitDrawMode()
    {
        if (playerController != null)
        {
            playerController.enabled = true;
            playerController.SetCursorLocked(true);
        }

        if (drawUI != null) drawUI.Close();
        _playerInZone = null;
    }
}