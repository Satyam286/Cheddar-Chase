using UnityEngine;

/// <summary>
/// Put on the red ball and on any hazard/obstacle that should cost a life.
/// The file MUST be named KillZone.cs (Unity requires the file name to match the class name).
/// </summary>
public class KillZone : MonoBehaviour
{
    [Tooltip("Speed the player is bounced away at when hit (0 = no knockback). " +
             "Keep below PlayerController.maxSpeed.")]
    public float knockbackSpeed = 6f;

    void OnCollisionEnter(Collision collision)
    {
        TryHit(collision);
    }

    // Without this, a player who stays in contact after the invincibility
    // window ends would never be hit again.
    void OnCollisionStay(Collision collision)
    {
        TryHit(collision);
    }

    void TryHit(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        PlayerHealth health = collision.gameObject.GetComponent<PlayerHealth>();
        if (health == null)
            return;

        // False while invincible / level over -> no knockback, no repeated hits.
        if (!health.LoseLife())
            return;

        Rigidbody playerBody = collision.rigidbody;
        if (playerBody == null || knockbackSpeed <= 0f)
            return;

        // Push away from the point of contact so the player can escape.
        Vector3 direction = collision.transform.position - collision.GetContact(0).point;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = collision.transform.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        Vector3 velocity = playerBody.linearVelocity;
        playerBody.linearVelocity = new Vector3(
            direction.x * knockbackSpeed,
            velocity.y,
            direction.z * knockbackSpeed);
    }
}