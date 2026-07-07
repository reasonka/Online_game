using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CustomerAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    private Seat seat;

    private bool isSitting = false;
    private bool isLeaving = false;
    private bool isReacting = false;
    private bool isGhostHaunting = false;
    private bool isTransformingIntoGhost = false;

    public Transform exitPoint;

    public Seat AssignedSeat => seat;
    public bool IsSitting => isSitting;
    public bool IsLeaving => isLeaving;
    public bool IsGhostHaunting => isGhostHaunting;

    [Header("Customer Color Randomizer")]
    public Renderer customerColorRenderer;
    public int customerColorMaterialIndex = 0;
    public Material[] customerColorMaterials;

    private static readonly List<Material> availableColorPool = new List<Material>();
    private static readonly HashSet<Material> activeColors = new HashSet<Material>();

    private Material assignedColorMaterial;
    private bool colorTakenFromPool = false;

    [Header("Face Material Swap")]
    public SkinnedMeshRenderer faceRenderer;
    public Material happyFace;
    public Material angryFace;
    public Material deadFace;

    [Header("Animator Reaction Triggers")]
    public string happyTrigger = "Happy";
    public string angryTrigger = "Angry";
    public string deadTrigger = "Dead";

    [Header("Reaction Leave Timing")]
    public bool leaveAutomaticallyAfterReaction = true;
    public float happyReactionDuration = 2f;
    public float angryReactionDuration = 2f;
    public float deathReactionDuration = 2.5f;

    [Header("Ghost Haunting")]
    public bool turnIntoGhostOnDeath = true;

    [Tooltip("Assign the normal customer mesh/model child here. Do NOT assign the whole Customer prefab root.")]
    public GameObject normalVisualRoot;

    [Tooltip("Assign a ghost model child here. Keep it disabled in the prefab.")]
    public GameObject ghostVisualRoot;

    [Tooltip("Optional. If you do not have a ghost child already, assign a ghost prefab here.")]
    public GameObject ghostPrefab;

    public Vector3 ghostPrefabLocalPosition = Vector3.zero;
    public Vector3 ghostPrefabLocalRotation = Vector3.zero;

    [Tooltip("Order Taker player index. Order Taker is index 0.")]
    public int ghostTargetPlayerIndex = 0;

    public float ghostChaseSpeed = 1.2f;
    public float ghostStoppingDistance = 1.2f;
    public float ghostDestinationUpdateRate = 0.25f;
    public bool ghostLookAtTarget = true;

    [Tooltip("If true, the ghost can still use the first tagged Player as a fallback if playerIndex 0 is not found.")]
    public bool fallbackToTaggedPlayerIfOrderTakerNotFound = true;

    [Header("Ghost Transformation FX")]
    public AudioSource ghostAudioSource;
    public AudioClip ghostTransformSound;
    public ParticleSystem ghostTransformParticles;

    [Tooltip("How long the normal customer fades into the ghost.")]
    public float ghostTransformDuration = 1.2f;

    [Tooltip("Ghost starts from this scale and grows to normal size.")]
    public float ghostStartScale = 0.25f;

    [Tooltip("Normal model shrinks to this scale before disappearing.")]
    public float normalEndScale = 0.75f;

    [Tooltip("If true, tries to fade renderer material alpha. Materials must support transparency for this to be visible.")]
    public bool fadeMaterialsDuringGhostTransform = true;

    [Tooltip("If true, the customer floats slightly upward while becoming a ghost.")]
    public bool floatUpDuringTransform = true;

    public float ghostFloatUpAmount = 0.6f;

    private Transform ghostTarget;
    private float ghostDestinationTimer = 0f;

    private Coroutine leaveAfterReactionCoroutine;
    private Coroutine ghostAfterDeathCoroutine;

    private Vector3 normalOriginalScale = Vector3.one;
    private Vector3 ghostOriginalScale = Vector3.one;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        if (normalVisualRoot != null)
        {
            normalOriginalScale = normalVisualRoot.transform.localScale;
        }

        if (ghostVisualRoot != null)
        {
            ghostOriginalScale = ghostVisualRoot.transform.localScale;
            ghostVisualRoot.SetActive(false);
        }

        if (ghostAudioSource == null)
        {
            ghostAudioSource = GetComponent<AudioSource>();
        }

        AssignRandomCustomerColor();
    }

    private void AssignRandomCustomerColor()
    {
        if (customerColorRenderer == null)
        {
            Debug.LogWarning("Customer color renderer is not assigned.");
            return;
        }

        if (customerColorMaterials == null || customerColorMaterials.Length == 0)
        {
            Debug.LogWarning("No customer color materials assigned.");
            return;
        }

        if (availableColorPool.Count == 0)
        {
            foreach (Material mat in customerColorMaterials)
            {
                if (mat != null && !activeColors.Contains(mat) && !availableColorPool.Contains(mat))
                {
                    availableColorPool.Add(mat);
                }
            }
        }

        if (availableColorPool.Count == 0)
        {
            Debug.LogWarning("No unused customer colors left. Add more colors if you want zero repeats.");
            assignedColorMaterial = customerColorMaterials[Random.Range(0, customerColorMaterials.Length)];
            colorTakenFromPool = false;
        }
        else
        {
            int randomIndex = Random.Range(0, availableColorPool.Count);

            assignedColorMaterial = availableColorPool[randomIndex];
            availableColorPool.RemoveAt(randomIndex);

            activeColors.Add(assignedColorMaterial);
            colorTakenFromPool = true;
        }

        ApplyCustomerColor(assignedColorMaterial);
    }

    private void ApplyCustomerColor(Material mat)
    {
        if (mat == null) return;

        Material[] mats = customerColorRenderer.materials;

        if (customerColorMaterialIndex < 0 || customerColorMaterialIndex >= mats.Length)
        {
            Debug.LogWarning("Customer color material index is out of range.");
            return;
        }

        mats[customerColorMaterialIndex] = mat;
        customerColorRenderer.materials = mats;
    }

    private void OnDestroy()
    {
        if (assignedColorMaterial != null && colorTakenFromPool)
        {
            activeColors.Remove(assignedColorMaterial);

            if (!availableColorPool.Contains(assignedColorMaterial))
            {
                availableColorPool.Add(assignedColorMaterial);
            }
        }
    }

    public void AssignSeat(Seat targetSeat)
    {
        seat = targetSeat;
        isSitting = false;
        isLeaving = false;
        isReacting = false;
        isGhostHaunting = false;
        isTransformingIntoGhost = false;

        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = Mathf.Max(agent.speed, 0.1f);
            agent.SetDestination(seat.transform.position);
        }

        if (animator != null)
        {
            animator.SetBool("IsMoving", true);
            animator.SetBool("IsSitting", false);
        }
    }

    private void Update()
    {
        if (isGhostHaunting)
        {
            UpdateGhostHaunting();
            return;
        }

        if (isLeaving || isReacting || isTransformingIntoGhost)
            return;

        if (seat == null || isSitting)
            return;

        if (agent != null && !agent.pathPending && agent.remainingDistance <= 0.1f)
        {
            SitInstantly();
        }
    }

    private void SitInstantly()
    {
        isSitting = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        transform.position = seat.transform.position;
        transform.rotation = seat.transform.rotation;

        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsSitting", true);
        }

        Debug.Log("Customer is now sitting.");
    }

    public void StandAndLeave()
    {
        isSitting = false;
        isReacting = false;
        isGhostHaunting = false;
        isTransformingIntoGhost = false;

        if (animator != null)
        {
            animator.SetBool("IsSitting", false);
        }

        if (seat != null)
        {
            seat.isOccupied = false;
        }

        Destroy(gameObject);
    }

    public void LeaveRestaurant()
    {
        if (isLeaving)
            return;

        if (isGhostHaunting || isTransformingIntoGhost)
            return;

        Debug.Log("Customer leaving restaurant.");

        if (leaveAfterReactionCoroutine != null)
        {
            StopCoroutine(leaveAfterReactionCoroutine);
            leaveAfterReactionCoroutine = null;
        }

        if (ghostAfterDeathCoroutine != null)
        {
            StopCoroutine(ghostAfterDeathCoroutine);
            ghostAfterDeathCoroutine = null;
        }

        isSitting = false;
        isReacting = false;
        isLeaving = true;

        Seat oldSeat = seat;
        seat = null;

        if (oldSeat != null)
        {
            oldSeat.isOccupied = false;
        }

        if (animator != null)
        {
            animator.SetBool("IsSitting", false);
            animator.SetBool("IsMoving", true);
        }

        if (agent == null)
        {
            Destroy(gameObject);
            return;
        }

        agent.isStopped = false;

        if (exitPoint != null)
        {
            agent.SetDestination(exitPoint.position);
            StartCoroutine(DestroyAfterReachingExit());
        }
        else
        {
            Debug.LogWarning("Exit point is not assigned. Destroying customer instead.");
            Destroy(gameObject);
        }
    }

    private IEnumerator DestroyAfterReachingExit()
    {
        while (agent != null && agent.pathPending)
        {
            yield return null;
        }

        while (agent != null && agent.remainingDistance > 0.2f)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        Destroy(gameObject);
    }

    public void PlayHappyReaction()
    {
        PlayReactionAndLeave(happyFace, happyTrigger, happyReactionDuration);
    }

    public void PlayAngryReaction()
    {
        PlayReactionAndLeave(angryFace, angryTrigger, angryReactionDuration);
    }

    public void PlayDeathReaction()
    {
        PrepareForReaction();
        SetFaceMaterial(deadFace);

        if (animator != null)
        {
            animator.ResetTrigger(happyTrigger);
            animator.ResetTrigger(angryTrigger);
            animator.ResetTrigger(deadTrigger);

            animator.SetTrigger(deadTrigger);
        }

        if (turnIntoGhostOnDeath)
        {
            if (ghostAfterDeathCoroutine != null)
            {
                StopCoroutine(ghostAfterDeathCoroutine);
            }

            ghostAfterDeathCoroutine = StartCoroutine(TurnIntoGhostAfterDeathRoutine(deathReactionDuration));
        }
        else if (leaveAutomaticallyAfterReaction)
        {
            if (leaveAfterReactionCoroutine != null)
            {
                StopCoroutine(leaveAfterReactionCoroutine);
            }

            leaveAfterReactionCoroutine = StartCoroutine(LeaveAfterReactionDuration(deathReactionDuration));
        }
    }

    private void PlayReactionAndLeave(Material faceMaterial, string triggerName, float reactionDuration)
    {
        PrepareForReaction();
        SetFaceMaterial(faceMaterial);

        if (animator != null)
        {
            animator.ResetTrigger(happyTrigger);
            animator.ResetTrigger(angryTrigger);
            animator.ResetTrigger(deadTrigger);

            animator.SetTrigger(triggerName);
        }

        if (leaveAutomaticallyAfterReaction)
        {
            if (leaveAfterReactionCoroutine != null)
            {
                StopCoroutine(leaveAfterReactionCoroutine);
            }

            leaveAfterReactionCoroutine = StartCoroutine(LeaveAfterReactionDuration(reactionDuration));
        }
    }

    private IEnumerator LeaveAfterReactionDuration(float duration)
    {
        yield return new WaitForSeconds(duration);

        LeaveRestaurant();
    }

    private IEnumerator TurnIntoGhostAfterDeathRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        StartCoroutine(GhostTransformationRoutine());
    }

    private IEnumerator GhostTransformationRoutine()
    {
        if (isGhostHaunting || isTransformingIntoGhost)
            yield break;

        Debug.Log("Customer is transforming into a ghost.");

        isSitting = false;
        isReacting = false;
        isLeaving = false;
        isTransformingIntoGhost = true;

        Seat oldSeat = seat;
        seat = null;

        if (oldSeat != null)
        {
            oldSeat.isOccupied = false;
        }

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (animator != null)
        {
            animator.SetBool("IsSitting", false);
            animator.SetBool("IsMoving", false);
        }

        PlayGhostTransformFX();
        PrepareGhostVisual();

        Renderer[] normalRenderers = normalVisualRoot != null
            ? normalVisualRoot.GetComponentsInChildren<Renderer>(true)
            : GetComponentsInChildren<Renderer>(true);

        Renderer[] ghostRenderers = ghostVisualRoot != null
            ? ghostVisualRoot.GetComponentsInChildren<Renderer>(true)
            : new Renderer[0];

        if (fadeMaterialsDuringGhostTransform)
        {
            SetRendererAlpha(normalRenderers, 1f);
            SetRendererAlpha(ghostRenderers, 0f);
        }

        Vector3 startPosition = transform.position;
        Vector3 endPosition = floatUpDuringTransform
            ? startPosition + Vector3.up * ghostFloatUpAmount
            : startPosition;

        float timer = 0f;
        float duration = Mathf.Max(0.01f, ghostTransformDuration);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float smoothT = SmoothStep(t);

            if (normalVisualRoot != null)
            {
                normalVisualRoot.transform.localScale =
                    Vector3.Lerp(normalOriginalScale, normalOriginalScale * normalEndScale, smoothT);
            }

            if (ghostVisualRoot != null)
            {
                ghostVisualRoot.transform.localScale =
                    Vector3.Lerp(ghostOriginalScale * ghostStartScale, ghostOriginalScale, smoothT);
            }

            if (fadeMaterialsDuringGhostTransform)
            {
                SetRendererAlpha(normalRenderers, 1f - smoothT);
                SetRendererAlpha(ghostRenderers, smoothT);
            }

            transform.position = Vector3.Lerp(startPosition, endPosition, smoothT);

            yield return null;
        }

        if (normalVisualRoot != null)
        {
            normalVisualRoot.SetActive(false);
        }

        if (ghostVisualRoot != null)
        {
            ghostVisualRoot.SetActive(true);
            ghostVisualRoot.transform.localScale = ghostOriginalScale;
        }

        if (fadeMaterialsDuringGhostTransform)
        {
            SetRendererAlpha(ghostRenderers, 1f);
        }

        isTransformingIntoGhost = false;
        BeginGhostHaunting();
    }

    private void PlayGhostTransformFX()
    {
        if (ghostAudioSource != null && ghostTransformSound != null)
        {
            ghostAudioSource.PlayOneShot(ghostTransformSound);
        }

        if (ghostTransformParticles != null)
        {
            ghostTransformParticles.Play();
        }
    }

    private void PrepareGhostVisual()
    {
        if (ghostVisualRoot == null && ghostPrefab != null)
        {
            GameObject ghostObj = Instantiate(ghostPrefab, transform);
            ghostObj.transform.localPosition = ghostPrefabLocalPosition;
            ghostObj.transform.localRotation = Quaternion.Euler(ghostPrefabLocalRotation);

            ghostVisualRoot = ghostObj;
            ghostOriginalScale = ghostVisualRoot.transform.localScale;
        }

        if (ghostVisualRoot != null)
        {
            ghostVisualRoot.SetActive(true);
            ghostVisualRoot.transform.localScale = ghostOriginalScale * ghostStartScale;
        }
        else
        {
            Debug.LogWarning("No ghostVisualRoot or ghostPrefab assigned. Customer will haunt using current model.");
        }
    }

    private void BeginGhostHaunting()
    {
        Debug.Log("Customer became a ghost and is haunting the Order Taker.");

        isGhostHaunting = true;

        ghostTarget = FindOrderTakerTarget();

        if (animator != null)
        {
            animator.SetBool("IsSitting", false);
            animator.SetBool("IsMoving", true);
        }

        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = ghostChaseSpeed;
            agent.stoppingDistance = ghostStoppingDistance;

            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
        }
    }

    public void LeaveRestaurantFromAnimationEvent()
    {
        if (isGhostHaunting || isTransformingIntoGhost)
            return;

        LeaveRestaurant();
    }

    private void UpdateGhostHaunting()
    {
        if (ghostTarget == null)
        {
            ghostTarget = FindOrderTakerTarget();

            if (ghostTarget == null)
            {
                return;
            }
        }

        ghostDestinationTimer -= Time.deltaTime;

        if (ghostDestinationTimer <= 0f)
        {
            ghostDestinationTimer = ghostDestinationUpdateRate;
            MoveGhostTowardTarget();
        }

        if (ghostLookAtTarget)
        {
            LookAtGhostTarget();
        }
    }

    private void MoveGhostTowardTarget()
    {
        if (ghostTarget == null)
            return;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = ghostChaseSpeed;
            agent.stoppingDistance = ghostStoppingDistance;
            agent.SetDestination(ghostTarget.position);
        }
        else
        {
            Vector3 targetPosition = ghostTarget.position;
            targetPosition.y = transform.position.y;

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                ghostChaseSpeed * Time.deltaTime
            );
        }
    }

    private void LookAtGhostTarget()
    {
        if (ghostTarget == null)
            return;

        Vector3 direction = ghostTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 4f);
    }

    private Transform FindOrderTakerTarget()
    {
        PhotonPlayerLocalSetup[] players = FindObjectsOfType<PhotonPlayerLocalSetup>(true);

        foreach (PhotonPlayerLocalSetup player in players)
        {
            if (player == null)
                continue;

            if (!player.gameObject.activeInHierarchy)
                continue;

            if (player.playerIndex == ghostTargetPlayerIndex)
            {
                Debug.Log("Ghost target found: " + player.name);
                return player.transform;
            }
        }

        if (fallbackToTaggedPlayerIfOrderTakerNotFound)
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");

            if (taggedPlayer != null)
            {
                Debug.LogWarning("Order Taker playerIndex 0 not found. Falling back to tagged Player.");
                return taggedPlayer.transform;
            }
        }

        Debug.LogWarning("Ghost could not find Order Taker player with index " + ghostTargetPlayerIndex);
        return null;
    }

    private void SetFaceMaterial(Material mat)
    {
        if (faceRenderer != null && mat != null)
        {
            Material[] mats = faceRenderer.materials;

            if (mats.Length > 1)
            {
                mats[1] = mat;
                faceRenderer.materials = mats;
            }
        }
    }

    private void PrepareForReaction()
    {
        isReacting = true;
        isSitting = false;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (animator != null)
        {
            animator.SetBool("IsSitting", false);
            animator.SetBool("IsMoving", false);
        }
    }

    private void SetRendererAlpha(Renderer[] renderers, float alpha)
    {
        if (renderers == null)
            return;

        foreach (Renderer rend in renderers)
        {
            if (rend == null)
                continue;

            Material[] mats = rend.materials;

            foreach (Material mat in mats)
            {
                if (mat == null)
                    continue;

                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;
                    color.a = alpha;
                    mat.color = color;
                }
            }
        }
    }

    private float SmoothStep(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}