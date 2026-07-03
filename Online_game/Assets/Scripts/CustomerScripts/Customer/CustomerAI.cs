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

    public Transform exitPoint; // Spawnpoint / restaurant exit

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

    private Coroutine leaveAfterReactionCoroutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

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

        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(seat.transform.position);
        }

        if (animator != null)
        {
            animator.SetBool("IsMoving", true);
        }
    }

    private void Update()
    {
        // Do not allow the customer to sit again while reacting or leaving
        if (isLeaving || isReacting)
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

        Debug.Log("Customer leaving restaurant.");

        if (leaveAfterReactionCoroutine != null)
        {
            StopCoroutine(leaveAfterReactionCoroutine);
            leaveAfterReactionCoroutine = null;
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
        PlayReaction(happyFace, happyTrigger, happyReactionDuration);
    }

    public void PlayAngryReaction()
    {
        PlayReaction(angryFace, angryTrigger, angryReactionDuration);
    }

    public void PlayDeathReaction()
    {
        PlayReaction(deadFace, deadTrigger, deathReactionDuration);
    }

    private void PlayReaction(Material faceMaterial, string triggerName, float reactionDuration)
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

    // Optional: use this with an Animation Event at the last frame of each reaction animation
    public void LeaveRestaurantFromAnimationEvent()
    {
        LeaveRestaurant();
    }

    private void SetFaceMaterial(Material mat)
    {
        if (faceRenderer != null && mat != null)
        {
            Material[] mats = faceRenderer.materials;

            if (mats.Length > 1)
            {
                mats[1] = mat; // FACE is Element 1
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
}