using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Photon.Pun;
using UnityEngine;

public class SpwanPlayer : MonoBehaviour
{
    public GameObject playerPrefab; // Assign this in Inspector
    void Start()
    {
        UnityEngine.Vector2 randomPosition = new UnityEngine.Vector2(0,0);
        PhotonNetwork.Instantiate(playerPrefab.name, randomPosition, UnityEngine.Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
