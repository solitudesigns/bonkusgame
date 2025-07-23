using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    [Header("Touch Input")]
    public LeftTouchController touchController; // Assign in Inspector

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            Debug.LogWarning("No SpriteRenderer found on the Player!");
    }

    void FixedUpdate()
    {
        Vector2 move = Vector2.zero;

        // Old Unity Input System: Keyboard + Gamepad axes
        float horizontal = Input.GetAxisRaw("Horizontal"); // includes A/D, ←/→, left stick
        float vertical = Input.GetAxisRaw("Vertical");     // includes W/S, ↑/↓, left stick

        move = new Vector2(horizontal, vertical);

        // Touch fallback if no input detected
        if (move == Vector2.zero && touchController != null)
        {
            move = touchController.direction;
        }

        move = move.normalized;
        rb.velocity = move * speed;

        // Flip sprite
        if (spriteRenderer != null)
        {
            if (move.x < -0.1f) spriteRenderer.flipX = true;
            else if (move.x > 0.1f) spriteRenderer.flipX = false;
        }
    }
}
