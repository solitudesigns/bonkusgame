using UnityEngine;
using Photon.Pun;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

public class GameLogicManager : MonoBehaviourPunCallbacks
{
    private bool gameEnded = false;
    private bool localPlayerSpawned = false;
    private bool gameStartDelayPassed = false;

    // 🔥 Add your server base URL here
    private string apiBase = "https://bonkus.solfuturenft.fun";

    void Start()
    {
        StartCoroutine(WaitForLocalPlayerSpawn());
        StartCoroutine(InitialGameStartDelay());
    }

     public static string GetJoinCode()
    {
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("JoinCode"))
        {
            return PhotonNetwork.CurrentRoom.CustomProperties["JoinCode"].ToString();
        }
        return "";
    }

    private IEnumerator WaitForLocalPlayerSpawn()
    {
        while (FindLocalPlayer() == null) yield return null;
        localPlayerSpawned = true;
    }

    private IEnumerator InitialGameStartDelay()
    {
        yield return new WaitForSeconds(10f);
        gameStartDelayPassed = true;
    }

    void Update()
    {
        if (!localPlayerSpawned || !gameStartDelayPassed) return;
        if (PhotonNetwork.IsMasterClient && !gameEnded) CheckGameOver();
    }

    private GameObject FindLocalPlayer()
    {
        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
        {
            PhotonView pv = p.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine) return p;
        }
        return null;
    }

    private bool IsPlayerDead(GameObject player)
    {
        Collider[] c3D = player.GetComponentsInChildren<Collider>();
        Collider2D[] c2D = player.GetComponentsInChildren<Collider2D>();
        foreach (var c in c3D) if (c != null && c.enabled) return false;
        foreach (var c in c2D) if (c != null && c.enabled) return false;
        return true;
    }

    private void CheckGameOver()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        int aliveCount = 0;
        bool wolfAlive = false;
        bool wolfExists = false;

        foreach (GameObject p in players)
        {
            PhotonView pv = p.GetComponent<PhotonView>();
            if (pv == null || pv.Owner == null) continue;

            bool isDead = IsPlayerDead(p);

            if (pv.Owner.CustomProperties.ContainsKey("Role") &&
                (string)pv.Owner.CustomProperties["Role"] == "Wolf")
            {
                wolfExists = true;
                if (!isDead) wolfAlive = true;
            }

            if (!isDead) aliveCount++;
        }

        // ✅ Win Conditions
        if (!wolfExists)
        {
            DeclareWinner("Bonks");
            return;
        }

        if (wolfAlive && aliveCount <= 1) // Wolf wins
        {
            DeclareWinner("Wolf");
            return;
        }

        if (!wolfAlive) // Bonks win
        {
            DeclareWinner("Bonks");
        }
    }

    private void DeclareWinner(string winner)
    {
        if (gameEnded) return;
        gameEnded = true;

        // ✅ Send distribution request before declaring winner
        int mode = (winner == "Wolf") ? 0 : 1;
        StartCoroutine(DistributeBonkAndShowTx(mode));

        photonView.RPC("RPC_SetWinner", RpcTarget.All, winner);
    }

    [PunRPC]
    private void RPC_SetWinner(string winner)
    {
        PlayerPrefs.SetString("Winner", winner);
        PlayerPrefs.Save();
        StartCoroutine(LoadWinSceneAfterDelay());
    }

    private IEnumerator LoadWinSceneAfterDelay()
    {
        yield return new WaitForSeconds(0.3f);
        PhotonNetwork.LoadLevel("WinGame");
    }

    // ✅ Bonk Distribution Logic
    private IEnumerator DistributeBonkAndShowTx(int mode)
    {
        // Prepare JSON body
        string joinCode = GetJoinCode(); // ✅ Store joinCode during match start
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogWarning("⚠️ JoinCode missing, cannot distribute Bonk.");
            yield break;
        }

        string url = $"{apiBase}/distribute";
        string json = $"{{\"joinCode\":\"{joinCode}\",\"mode\":{mode}}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("🔄 Sending Bonk distribution request...");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Distribution failed: {req.error}");
            }
            else
            {
                Debug.Log("✅ Distribution Response: " + req.downloadHandler.text);
                try
                {
                    // Parse response to extract txSig
                    string jsonResp = req.downloadHandler.text;
                    string txSig = JsonUtility.FromJson<TxResponse>(jsonResp).distributeTxSig;

                    if (!string.IsNullOrEmpty(txSig))
                    {
                        Debug.Log($"🎯 Distribution Tx: {txSig}");
                        TxCopyButton.Instance.ShowTx(txSig); // ✅ Show transaction on screen
                    }
                    else
                    {
                        Debug.LogWarning(jsonResp);
                    }
                }
                catch
                {
                    Debug.LogWarning("⚠️ Could not parse txSig from response.");
                }
            }
        }
    }

    [System.Serializable]
    private class TxResponse
    {
        public bool success;
        public string distributeTxSig;
    }
}
