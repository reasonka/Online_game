using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementController : MonoBehaviourPun
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;

    [Header("Mouse Look")]
    [Tooltip("摄像机上下旋转的父物体。")]
    public Transform cameraPivot;

    public float mouseSensitivity = 2f;
    public float minPitch = -60f;
    public float maxPitch = 75f;

    [Header("Gravity")]
    public float gravity = -20f;

    [Header("Animator")]
    [Tooltip("角色模型上的 Animator。")]
    public Animator animator;

    [Tooltip("Animator 中的 Float 参数名称。")]
    public string speedParameter = "Speed";

    [Tooltip("动画速度参数平滑变化的速度。")]
    public float animatorSpeedSmooth = 12f;

    [Header("Photon")]
    [Tooltip("本地测试时关闭，正式联网时开启。")]
    public bool usePhotonSync = false;

    [Header("Control")]
    public bool canMove = true;

    [Header("Cursor")]
    public bool lockCursorOnStart = true;

    private CharacterController controller;

    private float verticalVelocity;
    private float cameraPitch;
    private float currentAnimatorSpeed;

    private bool cursorLocked;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        SetCursorLocked(lockCursorOnStart);
    }

    private void Update()
    {
        if (!CanUseLocalInput())
        {
            return;
        }

        HandleCursorInput();

        if (!canMove)
        {
            UpdateAnimatorSpeed(0f);
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
            Input.GetAxis("Mouse X") *
            mouseSensitivity;

        float mouseY =
            Input.GetAxis("Mouse Y") *
            mouseSensitivity;

        /*
         * 鼠标左右移动时，旋转整个玩家根物体。
         */
        transform.Rotate(
            Vector3.up * mouseX
        );

        /*
         * 鼠标上下移动时，只旋转 CameraPivot。
         */
        cameraPitch -= mouseY;

        cameraPitch = Mathf.Clamp(
            cameraPitch,
            minPitch,
            maxPitch
        );

        if (cameraPivot != null)
        {
            cameraPivot.localRotation =
                Quaternion.Euler(
                    cameraPitch,
                    0f,
                    0f
                );
        }
    }

    private void HandleMovement()
    {
        float horizontal =
            Input.GetAxisRaw("Horizontal");

        float vertical =
            Input.GetAxisRaw("Vertical");

        Vector3 moveDirection =
            transform.right * horizontal +
            transform.forward * vertical;

        moveDirection = Vector3.ClampMagnitude(
            moveDirection,
            1f
        );

        bool hasMovementInput =
            moveDirection.sqrMagnitude > 0.01f;

        bool isRunning =
            hasMovementInput &&
            Input.GetKey(KeyCode.LeftShift);

        float currentSpeed =
            isRunning
                ? runSpeed
                : walkSpeed;

        float targetAnimatorSpeed;

        if (!hasMovementInput)
        {
            targetAnimatorSpeed = 0f;
        }
        else if (isRunning)
        {
            targetAnimatorSpeed = 1f;
        }
        else
        {
            targetAnimatorSpeed = 0.5f;
        }

        UpdateAnimatorSpeed(targetAnimatorSpeed);

        if (controller.isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity +=
            gravity * Time.deltaTime;

        Vector3 finalMovement =
            moveDirection * currentSpeed;

        finalMovement.y = verticalVelocity;

        controller.Move(
            finalMovement * Time.deltaTime
        );
    }

    private void ApplyGravityOnly()
    {
        if (controller.isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity +=
            gravity * Time.deltaTime;

        controller.Move(
            Vector3.up *
            verticalVelocity *
            Time.deltaTime
        );
    }

    private void UpdateAnimatorSpeed(float targetSpeed)
    {
        currentAnimatorSpeed =
            Mathf.MoveTowards(
                currentAnimatorSpeed,
                targetSpeed,
                animatorSpeedSmooth *
                Time.deltaTime
            );

        if (animator != null)
        {
            animator.SetFloat(
                speedParameter,
                currentAnimatorSpeed
            );
        }
    }

    private void HandleCursorInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLocked(false);
        }

        if (!cursorLocked &&
            canMove &&
            Input.GetMouseButtonDown(0))
        {
            SetCursorLocked(true);
        }
    }

    public void SetCanMove(bool value)
    {
        canMove = value;

        if (!canMove)
        {
            UpdateAnimatorSpeed(0f);
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

    public bool IsCursorLocked()
    {
        return cursorLocked;
    }
}