using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;

    [Header("Camera Collision")]
    public LayerMask obstacleLayer;
    public float wallPadding = 0.2f;
    public float cameraRadius = 0.2f;

    private Vector3 offset;

    void Start()
    {
        offset = transform.position - player.transform.position;
    }

    void LateUpdate()
    {
        // The position where the camera normally wants to be
        Vector3 desiredPosition = player.transform.position + offset;

        // Direction from player to the desired camera position
        Vector3 direction = desiredPosition - player.transform.position;
        float distance = direction.magnitude;

        // Check for walls/obstacles between the player and camera
        if (Physics.SphereCast(
            player.transform.position,
            cameraRadius,
            direction.normalized,
            out RaycastHit hit,
            distance,
            obstacleLayer))
        {
            // Move the camera just in front of the obstacle
            transform.position = hit.point - direction.normalized * wallPadding;
        }
        else
        {
            // No obstacle, so use the normal camera position
            transform.position = desiredPosition;
        }

        // Keep the camera facing the player
        transform.LookAt(player.transform);
    }
}