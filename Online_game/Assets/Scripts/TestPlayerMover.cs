using UnityEngine;

/// <summary>
/// TEST-ONLY minimal player mover, just so you can walk into the trigger zone
/// and test drawing without your real player prefab / networking set up yet.
/// WASD to move, mouse to look (while cursor is locked).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class TestPlayerMover : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSpeed = 2f;

    private CharacterController _controller;
    private float _pitch;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Only look around while the cursor is locked (i.e. not currently drawing)
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

            transform.Rotate(Vector3.up * mouseX);

            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, -80f, 80f);
            if (Camera.main != null)
                Camera.main.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = transform.right * h + transform.forward * v;
        _controller.SimpleMove(move * moveSpeed);
    }
}