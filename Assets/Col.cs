using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

[RequireComponent(typeof(Collider2D))]
public class SolidCollider : MonoBehaviourPun
{
    public Color dimColor = new Color(1, 1, 1, 0.3f);   // dim color when not touching
    public Color brightColor = new Color(1, 1, 1, 1f);  // bright color when touching

    private Button useButton;
    private Image btnImage;
    private Collider2D myCollider;

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        myCollider.isTrigger = false; // ✅ Solid collider to block player
        Debug.Log("[DEBUG] Solid Collider initialized on: " + gameObject.name);
    }

    void Start()
    {
        // ✅ Automatically find the USE button in the scene
        GameObject btnObj = GameObject.Find("USE");
        if (btnObj != null)
        {
            useButton = btnObj.GetComponent<Button>();
            if (useButton != null)
            {
                btnImage = useButton.GetComponent<Image>();
                btnImage.color = dimColor;
                useButton.interactable = false;
                Debug.Log("[DEBUG] Found and linked USE button.");
            }
            else
            {
                Debug.LogError("[ERROR] USE GameObject found but no Button component attached.");
            }
        }
        else
        {
            Debug.LogError("[ERROR] No GameObject named 'USE' found in scene!");
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("[DEBUG] Collision ENTER with: " + collision.gameObject.name);

        if (collision.gameObject.CompareTag("Player") && useButton != null)
        {
            PhotonView pv = collision.gameObject.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                btnImage.color = brightColor;
                useButton.interactable = true;
                Debug.Log("[DEBUG] Local Player collided with " + gameObject.name + " → Button Bright");
            }
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("[DEBUG] Player is still touching " + gameObject.name);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        Debug.Log("[DEBUG] Collision EXIT with: " + collision.gameObject.name);

        if (collision.gameObject.CompareTag("Player") && useButton != null)
        {
            PhotonView pv = collision.gameObject.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                btnImage.color = dimColor;
                useButton.interactable = false;
                Debug.Log("[DEBUG] Local Player left " + gameObject.name + " → Button Dim");
            }
        }
    }
}
