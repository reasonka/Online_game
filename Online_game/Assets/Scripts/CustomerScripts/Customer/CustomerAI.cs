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

    public Transform exitPoint; // spawnpoint, which is the restaurant exit!!

    private bool isLeaving = false;

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

        // Refill the pool with colors that are not currently being used
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

        // If all colors are already being used, repeats become unavoidable
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
        // Return the color back into the available pool when this customer disappears
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

        // Walk towards seat
        agent.SetDestination(seat.transform.position);

        if (animator != null)
            animator.SetBool("IsMoving", true);
    }

    private void Update()
    {
        // If leaving, do nothing
        if (isLeaving)
            return;

        // If no seat or already sitting, stop
        if (seat == null || isSitting)
            return;

        // Sitting detection ONLY for initial seating
        if (!agent.pathPending && agent.remainingDistance <= 0.1f)
        {
            SitInstantly();
        }
    }

    private void SitInstantly()
    {
        isSitting = true;
        agent.isStopped = true;
        agent.ResetPath();

        // Snap position to exact seat location
        transform.position = seat.transform.position;

        // Snap rotation to seat rotation
        transform.rotation = seat.transform.rotation;

        // Switch to sitting animation
        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsSitting", true);
        }

        Debug.Log("Customer is now sitting (instant sit).");
    }

    public void StandAndLeave()
    {
        isSitting = false;

        if (animator != null)
            animator.SetBool("IsSitting", false);

        seat.isOccupied = false;
        Destroy(gameObject);
    }

    public void LeaveRestaurant()
    {
        Debug.Log("Customer leaving restaurant");

        if (agent == null) return;

        isSitting = false;
        isLeaving = true;

        Seat oldSeat = seat;

        seat = null;

        if (oldSeat != null)
            oldSeat.isOccupied = false;

        if (animator != null)
        {
            animator.SetBool("IsSitting", false);
            animator.SetBool("IsMoving", true);
        }

        agent.isStopped = false;

        if (exitPoint != null)
            agent.SetDestination(exitPoint.position);

        StartCoroutine(DestroyAfterReachingExit());
    }

    private IEnumerator DestroyAfterReachingExit()
    {
        while (agent.pathPending)
            yield return null;

        while (agent.remainingDistance > 0.2f)
            yield return null;

        yield return new WaitForSeconds(0.2f);

        if (seat != null)
            seat.isOccupied = false;

        Destroy(gameObject);
    }

    public void PlayHappyReaction()
    {
        PrepareForReaction();
        SetFaceMaterial(happyFace);
        animator.SetTrigger(happyTrigger);
    }

    public void PlayAngryReaction()
    {
        PrepareForReaction();
        SetFaceMaterial(angryFace);
        animator.SetTrigger(angryTrigger);
    }

    public void PlayDeathReaction()
    {
        PrepareForReaction();
        SetFaceMaterial(deadFace);
        animator.SetTrigger(deadTrigger);
    }

    private void SetFaceMaterial(Material mat)
    {
        if (faceRenderer != null && mat != null)
        {
            var mats = faceRenderer.materials;
            if (mats.Length > 1)
            {
                mats[1] = mat;   // FACE is Element 1
                faceRenderer.materials = mats;
            }
        }
    }

    private void PrepareForReaction()
    {
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