using Photon.Pun;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Transform target;

    void Start()
    {
        // Find the local player
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            PhotonView view = player.GetComponent<PhotonView>();
            if (view != null && view.IsMine)
            {
                target = player.transform;
                break;
            }
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 newPos = target.position;
            newPos.z = -10f; // for 2D; use camera's default z position
            transform.position = newPos;
        }
    }
}
