using UnityEngine;

public class FallTrigger : MonoBehaviour
{
    public FallingObject fallingObject;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            fallingObject.Drop();
        }
    }
}