using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
public class BasicPlayerController : MonoBehaviourPun
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;

    [Header("Mouse Look")]
    public Transform cameraPivot;

    public float mouseSensitivity = 2f;
    public float minPitch = -60f;
    public float maxPitch = 75f;

    [Header("Gravity")]
    public float gravity = -20f;

    [Header("Animator")]
    public Animator animator;
    public string speedParameter = "Speed";
    public float animatorSpeedSmooth = 12f;

    [Header("Emotions")]
    public PlayerEmotionController emotionController;
    public bool syncEmotionsOverPhoton = false;

    [Header("Photon")]
    public bool usePhotonSync = false;

    [Header("Cursor")]
    public bool lockCursorOnStart = true;
    public KeyCode unlockCursorKey = KeyCode.LeftControl;

    [Header("Footstep Sound")]
    public float walkStepInterval = 0.45f;
    public float runStepInterval = 0.28f;

    private float footstepTimer;

    private CharacterController controller;

    private float verticalVelocity;
    private float cameraPitch;
    private float currentAnimatorSpeed;

    private bool emojiInputBlocked;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (emotionController == null)
            emotionController = GetComponentInChildren<PlayerEmotionController>(true);

        if (cameraPivot == null && Camera.main != null)
        {
            if (Camera.main.transform.IsChildOf(transform))
                cameraPivot = Camera.main.transform;
        }
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

        if (emojiInputBlocked)
        {
            UpdateAnimatorSpeed(0f);
            ApplyGravityOnly();
            return;
        }

        if (Cursor.lockState == CursorLockMode.Locked)
            HandleMouseLook();

        HandleMovement();
    }

    public bool CanUseLocalInput()
    {
        if (!usePhotonSync)
            return true;

        if (!PhotonNetwork.IsConnected)
            return true;

        return photonView.IsMine;
    }

    public void SetEmojiInputBlocked(bool blocked)
    {
        emojiInputBlocked = blocked;

        if (blocked)
            UpdateAnimatorSpeed(0f);
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection =
            transform.right * horizontal +
            transform.forward * vertical;

        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        bool hasMovementInput = moveDirection.sqrMagnitude > 0.01f;

        bool isRunning =
            hasMovementInput &&
            Input.GetKey(KeyCode.LeftShift);
        
        HandleFootstepSound(hasMovementInput, isRunning);

        float movementSpeed = isRunning ? runSpeed : walkSpeed;

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

        Vector3 finalMovement = moveDirection * movementSpeed;
        finalMovement.y = verticalVelocity;

        controller.Move(finalMovement * Time.deltaTime);
    }

    private void ApplyGravityOnly()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 gravityMovement = new Vector3(0f, verticalVelocity, 0f);
        controller.Move(gravityMovement * Time.deltaTime);
    }

    private void UpdateAnimatorSpeed(float targetSpeed)
    {
        currentAnimatorSpeed = Mathf.MoveTowards(
            currentAnimatorSpeed,
            targetSpeed,
            animatorSpeedSmooth * Time.deltaTime
        );

        if (animator != null)
            animator.SetFloat(speedParameter, currentAnimatorSpeed);
    }

    private void HandleCursorInput()
    {
        if (unlockCursorKey == KeyCode.None)
            return;

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
                return;

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

    public void PlayEmotion(EmotionType emotion)
    {
        if (!CanUseLocalInput())
            return;

        if (emotionController == null)
            emotionController = GetComponentInChildren<PlayerEmotionController>(true);

        if (syncEmotionsOverPhoton && usePhotonSync && PhotonNetwork.IsConnected)
        {
            photonView.RPC(nameof(RPC_PlayEmotion), RpcTarget.All, (int)emotion);
        }
        else
        {
            PlayEmotionLocal(emotion);
        }
    }

    [PunRPC]
    private void RPC_PlayEmotion(int emotionValue)
    {
        PlayEmotionLocal((EmotionType)emotionValue);
    }

    private void PlayEmotionLocal(EmotionType emotion)
    {
        if (emotionController == null)
            emotionController = GetComponentInChildren<PlayerEmotionController>(true);

        if (emotionController != null)
            emotionController.PlayEmotion(emotion);
        else
            Debug.LogWarning("No PlayerEmotionController found on player.");
    }

    private void HandleFootstepSound(bool hasMovementInput, bool isRunning)
    {
        if (!hasMovementInput || !controller.isGrounded)
        {
            footstepTimer = 0f;

            if (SFXManager.Instance != null)
                SFXManager.Instance.StopMovementSound();

            return;
        }

        footstepTimer -= Time.deltaTime;

        if (footstepTimer > 0f)
            return;

        if (SFXManager.Instance != null)
        {
            if (isRunning)
                SFXManager.Instance.PlayRun();
            else
                SFXManager.Instance.PlayWalk();
        }

        footstepTimer = isRunning ? runStepInterval : walkStepInterval;
    }
}