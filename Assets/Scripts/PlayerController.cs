using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 10f;

    [Tooltip("Horizontal speed cap. Keep this higher than the KillZone knockback speed.")]
    public float maxSpeed = 8f;

    [Tooltip("Optional. If set, movement is relative to this camera's facing direction. " +
             "Leave empty for a fixed/top-down camera.")]
    public Transform cameraTransform;

    [Header("Pickups")]
    public string pickupTag = "PickUp";
    public AudioClip pickupClip;

    private Rigidbody rb;
    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // called by the PlayerInput component
    void OnMove(InputValue movementValue)
    {
        moveInput = movementValue.Get<Vector2>();
    }

    void FixedUpdate()
    {
        // no more control once the level is over.
        if (LevelEndManager.Instance != null && LevelEndManager.Instance.LevelEnded)
            return;

        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);

        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;

            direction = forward.normalized * moveInput.y + right.normalized * moveInput.x;
        }

        rb.AddForce(direction * speed);

        // cap horizontal speed so the ball can't speed up forever.
        Vector3 velocity = rb.linearVelocity;
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);

        if (flat.magnitude > maxSpeed)
        {
            flat = flat.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(flat.x, velocity.y, flat.z);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(pickupTag))
            return;

        if (LevelEndManager.Instance != null && LevelEndManager.Instance.LevelEnded)
            return;
        other.enabled = false;

        if (pickupClip != null)
            AudioSource.PlayClipAtPoint(pickupClip, other.transform.position);
        if (LevelEndManager.Instance != null)
            LevelEndManager.Instance.AddCheese();

        Destroy(other.gameObject);
    }

    public int GetCheeseCount()
    {
        return LevelEndManager.Instance != null ? LevelEndManager.Instance.CollectedCheese : 0;
    }
}