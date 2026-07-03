using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementController : MonoBehaviourPun
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float rotationSpeed = 12f;

    [Header("Mouse Look")]
    public Transform cameraPivot;
    public float mouseSensitivity = 2f;
    public float minPitch = -60f;
    public float maxPitch = 75f;

    [Header("Gravity")]
    public float gravity = -20f;

    [Header("Photon")]
    public bool usePhotonSync = false;

    [Header("Control")]
    public bool canMove = true;

    private CharacterController controller;
    private float verticalVelocity;
    private float pitch;
    private bool cursorLocked = true;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        SetCursorLocked(true);
    }

    private void Update()
    {
        if (!CanUseLocalInput())
        {
            return;
        }

        HandleCursorLock();

        if (!canMove)
        {
            ApplyGravityOnly();
            return;
        }

        if (cursorLocked)
        {
            HandleMouseLook();
        }

        HandleMovement();
    }

    private bool CanUseLocalInput()
    {
        if (!usePhotonSync)
        {
            return true;
        }

        if (!PhotonNetwork.IsConnected)
        {
            return true;
        }

        return photonView.IsMine;
    }

    private void HandleMouseLook()
    {
        float mouseX =
            Input.GetAxis("Mouse X") * mouseSensitivity;

        float mouseY =
            Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 玩家左右旋转
        transform.Rotate(Vector3.up * mouseX);

        // 摄像机上下旋转
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (cameraPivot != null)
        {
            cameraPivot.localRotation =
                Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection =
            transform.right * horizontal +
            transform.forward * vertical;

        moveDirection.Normalize();

        float speed =
            Input.GetKey(KeyCode.LeftShift)
                ? runSpeed
                : walkSpeed;

        if (controller.isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 movement =
            moveDirection * speed;

        movement.y = verticalVelocity;

        controller.Move(
            movement * Time.deltaTime
        );
    }

    private void ApplyGravityOnly()
    {
        if (controller.isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        controller.Move(
            Vector3.up *
            verticalVelocity *
            Time.deltaTime
        );
    }

    private void HandleCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLocked(false);
        }

        if (!cursorLocked &&
            Input.GetMouseButtonDown(0))
        {
            SetCursorLocked(true);
        }
    }

    public void SetCursorLocked(bool locked)
    {
        cursorLocked = locked;

        Cursor.visible = !locked;

        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;
    }

    public void SetCanMove(bool value)
    {
        canMove = value;

        if (!value)
        {
            SetCursorLocked(false);
        }
        else
        {
            SetCursorLocked(true);
        }
    }
}