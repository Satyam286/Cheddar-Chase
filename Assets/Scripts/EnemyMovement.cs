using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
    public Transform player;

    [Header("Start")]
    [Tooltip("Seconds the enemy waits at the start of the level before chasing. 0 = chase immediately.")]
    public float startDelay = 1.5f;

    [Header("Movement Speed")]
    public float minSpeed = 3f;
    public float maxSpeed = 8f;

    public float closeDistance = 3f;
    public float farDistance = 15f;

    [Header("Movement")]
    public float acceleration = 12f;
    public float steeringStrength = 20f;

    [Header("Pathfinding")]
    [Tooltip("How often (seconds) the path to the player is recalculated.")]
    public float repathInterval = 0.2f;

    [Header("Near Player")]
    public float stopDistance = 1.2f;

    [Header("Rolling")]
    public float rollTorque = 4f;

    [Header("Drift")]
    [Range(0f, 1f)]
    public float sidewaysDamping = 0.85f;

    private NavMeshAgent navMeshAgent;
    private Rigidbody rb;

    private Vector3 desiredDirection;
    private float currentSpeed;
    private float repathTimer;
    private float elapsedSinceStart;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        // The NavMeshAgent only calculates the path; the Rigidbody does the moving.
        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;

        currentSpeed = minSpeed;
    }

    void Update()
    {
        if (player == null || !navMeshAgent.isOnNavMesh)
            return;

        // Grace period so the player isn't caught instantly at spawn.
        elapsedSinceStart += Time.deltaTime;
        if (elapsedSinceStart < startDelay)
        {
            desiredDirection = Vector3.zero;
            return;
        }

        // Rubber-banding: close = slow, far = fast.
        float distance = Vector3.Distance(transform.position, player.position);
        float distancePercent = Mathf.InverseLerp(closeDistance, farDistance, distance);
        currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, distancePercent);
        navMeshAgent.speed = currentSpeed;

        // Keep the agent synced with the Rigidbody.
        navMeshAgent.nextPosition = transform.position;

        // Recalculating every frame is wasteful - a few times per second is plenty.
        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;
            navMeshAgent.SetDestination(player.position);
        }

        // Follow the path's next corner. This is what lets the enemy get around
        // walls instead of pushing into them.
        Vector3 pathDirection = Vector3.zero;
        if (!navMeshAgent.pathPending && navMeshAgent.hasPath)
            pathDirection = Flat(navMeshAgent.steeringTarget - transform.position);

        // Fall back to a straight line if there is no usable path yet.
        Vector3 directDirection = Flat(player.position - transform.position);

        desiredDirection = pathDirection.sqrMagnitude > 0.01f
            ? pathDirection.normalized
            : directDirection.normalized;
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        Vector3 toPlayer = Flat(player.position - transform.position);
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);

        // Slow down when very close to the player.
        if (toPlayer.magnitude <= stopDistance)
        {
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                Vector3.zero,
                acceleration * Time.fixedDeltaTime);

            rb.linearVelocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
            return;
        }

        if (desiredDirection.sqrMagnitude < 0.01f)
            return;

        // Turn current movement toward the target movement.
        Vector3 targetVelocity = desiredDirection * currentSpeed;
        Vector3 newVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            steeringStrength * Time.fixedDeltaTime);

        // Reduce sideways drifting.
        Vector3 forwardVelocity = desiredDirection * Vector3.Dot(newVelocity, desiredDirection);
        Vector3 sidewaysVelocity = (newVelocity - forwardVelocity) * (1f - sidewaysDamping);
        newVelocity = forwardVelocity + sidewaysVelocity;

        rb.linearVelocity = new Vector3(newVelocity.x, velocity.y, newVelocity.z);

        // Physically roll the ball.
        if (newVelocity.magnitude > 0.1f)
        {
            Vector3 rollAxis = Vector3.Cross(Vector3.up, newVelocity.normalized);
            rb.AddTorque(rollAxis * rollTorque, ForceMode.Acceleration);
        }
    }

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }
}