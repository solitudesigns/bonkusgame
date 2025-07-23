using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Reference to the player/character
    public float smoothSpeed = 0.125f; // How smooth the camera follows
    public Vector3 offset; // Optional offset from the player (e.g., slightly above)

    void LateUpdate()
    {
        // Step 1: Calculate the position where the camera should move to
        Vector3 desiredPosition = target.position + offset;

        // Step 2: Smoothly interpolate between current and desired positions
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Step 3: Apply the new smoothed position
        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, transform.position.z);
    }
}
