using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CustomerAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    private Seat seat;


    private bool isSitting = false;

    public Transform exitPoint; //spawnpoint, which is the restaurant exit!!

    private bool isLeaving = false;

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

    // Later you'll use this to make them leave:
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

        // ⭐ store seat before nulling it
        Seat oldSeat = seat;

        seat = null;   // prevents SitInstantly from triggering again

        // ⭐ free seat properly
        if (oldSeat != null)
            oldSeat.isOccupied = false;

        // Stop sitting animation and play walk animation
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

        // ⭐ FREE SEAT ONLY NOW
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
        // Fully exit sitting state
        isSitting = false;

        // ❌ REMOVE THIS (causes seat not to be freed)
        // seat = null;

        // Ensure we don't auto-walk
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // Force animator out of sitting state
        if (animator != null)
        {
            animator.SetBool("IsSitting", false);
            animator.SetBool("IsMoving", false);
        }
    }



}
