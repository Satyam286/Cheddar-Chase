using UnityEngine;

public class MoveUpDown : MonoBehaviour
{
    public float moveAmount = 2f;
    public float speed = 2f;
    public bool moveDown = false;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        float movement = Mathf.PingPong(Time.time * speed, moveAmount);

        if (moveDown)
        {
            transform.position = startPosition + Vector3.down * movement;
        }
        else
        {
            transform.position = startPosition + Vector3.up * movement;
        }
    }
}