using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(CharacterController))]
public class BasicPlayerController : MonoBehaviourPun
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

    [Tooltip("Animator 中控制 Idle / Walk / Run 的 Float 参数。")]
    public string speedParameter = "Speed";

    [Tooltip("Animator Speed 参数的平滑变化速度。")]
    public float animatorSpeedSmooth = 12f;

    [Header("Photon")]
    [Tooltip("单机测试时关闭，Photon 联机时开启。")]
    public bool usePhotonSync = false;

    [Header("Cursor")]
    [Tooltip("游戏开始时锁定并隐藏鼠标。")]
    public bool lockCursorOnStart = true;

    private CharacterController controller;

    private float verticalVelocity;
    private float cameraPitch;
    private float currentAnimatorSpeed;

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
        if (!CanUseLocalInput())
        {
            return;
        }

        if (lockCursorOnStart)
        {
            SetCursorLocked(true);
        }
    }

    private void Update()
    {
        if (!CanUseLocalInput())
        {
            return;
        }

        HandleCursorInput();

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            HandleMouseLook();
        }

        HandleMovement();
    }

    private bool CanUseLocalInput()
    {
        /*
         * 单机测试时：
         * Use Photon Sync 关闭，直接允许输入。
         */
        if (!usePhotonSync)
        {
            return true;
        }

        /*
         * Photon 还没连接时，也允许本地测试。
         */
        if (!PhotonNetwork.IsConnected)
        {
            return true;
        }

        /*
         * Photon 模式下，只有本地 Owner 能读取输入。
         */
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
         * 鼠标左右移动：
         * 旋转整个玩家根物体。
         */
        transform.Rotate(
            Vector3.up * mouseX
        );

        /*
         * 鼠标上下移动：
         * 只旋转 CameraPivot。
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

        float movementSpeed =
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

        UpdateAnimatorSpeed(
            targetAnimatorSpeed
        );

        if (controller.isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity +=
            gravity * Time.deltaTime;

        Vector3 finalMovement =
            moveDirection * movementSpeed;

        finalMovement.y =
            verticalVelocity;

        controller.Move(
            finalMovement *
            Time.deltaTime
        );
    }

    private void UpdateAnimatorSpeed(
        float targetSpeed)
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
        /*
         * Esc 解锁并显示鼠标。
         */
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLocked(false);
        }

        /*
         * 左键点击 Game 窗口后重新锁定鼠标。
         */
        if (Cursor.lockState !=
                CursorLockMode.Locked &&
            Input.GetMouseButtonDown(0))
        {
            SetCursorLocked(true);
        }
    }

    public void SetCursorLocked(
        bool locked)
    {
        Cursor.visible = !locked;

        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;
    }
}