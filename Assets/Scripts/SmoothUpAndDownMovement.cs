using UnityEngine;

public class SmoothUpAndDownMovement : MonoBehaviour
{
    public float amplitude = 1f;   // The distance the object will move up and down
    public float speed = 1f;       // The speed at which the object will move

    private Vector3 initialPosition;    // The initial position of the object

    void Start()
    {
        // Store the initial position of the object
        initialPosition = transform.localPosition;
    }

    void Update()
    {
        // Calculate the new position based on the sine wave
        float newY = initialPosition.y + amplitude * Mathf.Sin(Time.time * speed);
        Vector3 newPosition = new Vector3(initialPosition.x, newY, initialPosition.z);

        // Move the object to the new position
        transform.localPosition = newPosition;
    }
}