using UnityEngine;

public class FallingObject : MonoBehaviour
{
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // keep the object up until the player gets beneath it
        rb.isKinematic = true;
    }

    public void Drop()
    {
        rb.isKinematic = false;
        rb.useGravity = true;
    }
}