using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float minSpeed = 3f;
    public float maxSpeed = 10f;
    public float acceleration = 5f;

    private float currentSpeed;
    private Rigidbody2D rb;
    private PhotonView view;
    private Vector2 lastDir;
    private bool facingRight = true;  // 🟢 track current facing

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 0;
        rb.drag = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        view = GetComponent<PhotonView>();
        currentSpeed = minSpeed;
    }

    void FixedUpdate()
    {
        if (!view.IsMine) return;

        Vector2 dir = Vector2.zero;

        if (MobileInput.Instance != null)
        {
            if (MobileInput.Instance.left) dir.x = -1;
            if (MobileInput.Instance.right) dir.x = 1;
            if (MobileInput.Instance.up) dir.y = 1;
            if (MobileInput.Instance.down) dir.y = -1;
        }

        // ✅ Accelerate only while holding a direction
        if (dir != Vector2.zero)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.fixedDeltaTime);
            lastDir = dir.normalized;
        }
        else
        {
            currentSpeed = minSpeed;
            lastDir = Vector2.zero;
        }

        // ✅ Apply movement
        rb.velocity = (lastDir != Vector2.zero) ? lastDir * currentSpeed : Vector2.zero;

        // ✅ Flip entire character left/right
        if (dir.x < 0 && facingRight)
        {
            Flip();
        }
        else if (dir.x > 0 && !facingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;              // 🔥 invert X scale
        transform.localScale = scale;
    }
}
