using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
public class PlayerOneController : MonoBehaviourPun
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;

    [Header("Mouse Look")]
    [Tooltip("Camera pivot object for vertical mouse look.")]
    public Transform cameraPivot;

    public float mouseSensitivity = 2f;
    public float minPitch = -60f;
    public float maxPitch = 75f;

    [Header("Gravity")]
    public float gravity = -20f;

    [Header("Animator")]
    [Tooltip("Animator on the character model.")]
    public Animator animator;

    [Tooltip("Animator float parameter used for Idle / Walk / Run.")]
    public string speedParameter = "Speed";

    [Tooltip("Animator speed smoothing.")]
    public float animatorSpeedSmooth = 12f;

    [Header("Photon")]
    [Tooltip("Enable this when using Photon sync.")]
    public bool usePhotonSync = false;

    [Header("Cursor")]
    [Tooltip("Lock cursor when the game starts.")]
    public bool lockCursorOnStart = true;
    public KeyCode unlockCursorKey = KeyCode.LeftControl;

    private CharacterController controller;

    private float verticalVelocity;
    private float cameraPitch;
    private float currentAnimatorSpeed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (!CanUseLocalInput())
            return;

        SetCursorLocked(lockCursorOnStart);
    }

    private void Update()
    {
        if (!CanUseLocalInput())
            return;

        HandleCursorInput();

        if (Cursor.lockState == CursorLockMode.Locked)
            HandleMouseLook();

        HandleMovement();
    }

    private bool CanUseLocalInput()
    {
        if (!usePhotonSync)
            return true;

        if (!PhotonNetwork.IsConnected)
            return true;

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

        transform.Rotate(Vector3.up * mouseX);

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
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

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
            isRunning ? runSpeed : walkSpeed;

        float targetAnimatorSpeed;

        if (!hasMovementInput)
            targetAnimatorSpeed = 0f;
        else if (isRunning)
            targetAnimatorSpeed = 1f;
        else
            targetAnimatorSpeed = 0.5f;

        UpdateAnimatorSpeed(targetAnimatorSpeed);

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalMovement =
            moveDirection * movementSpeed;

        finalMovement.y = verticalVelocity;

        controller.Move(
            finalMovement *
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
            animator.SetFloat(speedParameter, currentAnimatorSpeed);
    }

    private void HandleCursorInput()
    {
        if (Input.GetKeyDown(unlockCursorKey))
        {
            SetCursorLocked(false);
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked &&
            Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            SetCursorLocked(true);
        }
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.visible = !locked;

        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;
    }
}