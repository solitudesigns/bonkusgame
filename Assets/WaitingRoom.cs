using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Networking;
using Solana.Unity.SDK;
using System.Collections;

public class WaitingRoomManager : MonoBehaviourPunCallbacks
{
    public TMP_Text playerCountText;
    public GameObject startButton;
    public Transform playerListContainer; // UI container for player names
    public TMP_Text playerNamePrefab;     // Prefab for showing player names

    private Dictionary<int, TMP_Text> playerNameTexts = new Dictionary<int, TMP_Text>();

    void Start()
    {
        UpdatePlayerListUI();
        UpdatePlayerCountUI();
        startButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    // Update player count UI
    void UpdatePlayerCountUI()
    {
        playerCountText.text = "Players: " + PhotonNetwork.CurrentRoom.PlayerCount + "/4";
    }

    // Refresh player name list
    void UpdatePlayerListUI()
    {
        // Clear old entries
        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);
        playerNameTexts.Clear();

        // Add current players
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            TMP_Text nameText = Instantiate(playerNamePrefab, playerListContainer);
            nameText.text = p.NickName;
            playerNameTexts[p.ActorNumber] = nameText;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerCountUI();
        UpdatePlayerListUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerCountUI();
        UpdatePlayerListUI();
    }

    void Update()
    {
        // Enable start button only if min 4 players
        if (PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
            startButton.GetComponent<UnityEngine.UI.Button>().interactable =
                PhotonNetwork.CurrentRoom.PlayerCount >= 1;
        }
    }
    public void OnStartGame()
    {
        Debug.Log("Start Game button clicked!");

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Not the host, cannot start.");
            return;
        }

        Debug.Log("Host is starting game...");

        // Close room so no one can join mid-game
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        // Assign roles to players (1 Imposter, rest Crewmates)
        AssignRolesAndColors();

        // Set custom room property that the game has started
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["GameStarted"] = true;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        // Load the gameplay scene for all players
        PhotonNetwork.LoadLevel("Game");
    }

    // Role assignment function

    void AssignRolesAndColors()
    {
        Player[] players = PhotonNetwork.PlayerList;

        // ✅ Randomly choose 1 Wolf
        int imposterIndex = Random.Range(0, players.Length);

        // ✅ Available colors
        string[] playerColors = { "Black", "Red", "Blue", "Green", "Yellow" };
        List<string> availableColors = new List<string>(playerColors);

        for (int i = 0; i < players.Length; i++)
        {
            // ✅ Assign Role
            string role = (i == imposterIndex) ? "Wolf" : "Bonk";

            // ✅ Assign unique color
            string color = availableColors.Count > 0 ? availableColors[0] : "Gray";
            availableColors.Remove(color);

            // ✅ Save both Role and Color in Custom Properties
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["Role"] = role;
            props["PlayerColor"] = color;
            players[i].SetCustomProperties(props);

            // ✅ Also update nickname to color for easy UI
            players[i].NickName = color;

             if (players[i].ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber && role == "Wolf")
            {
                Debug.Log("🟢 This client is Wolf. Sending wallet to server...");
                StartCoroutine(SendWolfWalletToServer());
            }

            Debug.Log($"🎭 Assigned {role} with color {color} to {players[i].ActorNumber}");
        }
    }

    void AssignRoles()
    {
        Player[] players = PhotonNetwork.PlayerList;
        int imposterIndex = Random.Range(0, players.Length-1);

        for (int i = 0; i < players.Length; i++)
        {
            ExitGames.Client.Photon.Hashtable roleProp = new ExitGames.Client.Photon.Hashtable();
            roleProp["Role"] = (i == imposterIndex) ? "Wolf" : "Bonk";
            players[i].SetCustomProperties(roleProp);
        }

        Debug.Log("Roles assigned. Imposter index: " + imposterIndex);
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
    IEnumerator SendWolfWalletToServer()
    {
        string joinCode = GetJoinCode();
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("❌ No JoinCode found! Cannot send Wolf wallet.");
            yield break;
        }

        if (Web3.Wallet == null)
        {
            Debug.LogError("❌ Wallet not connected!");
            yield break;
        }

        string walletAddress = Web3.Wallet.Account.PublicKey.Key;

        Debug.Log($"📡 Sending Wolf Wallet: {walletAddress} for JoinCode: {joinCode}");

        string url = "https://bonkus.solfuturenft.fun/setwolf";
        string jsonBody = JsonUtility.ToJson(new WolfRequest(walletAddress, joinCode));
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Wolf registered successfully: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError($"❌ Failed to register Wolf. Error: {request.error} | Response: {request.downloadHandler.text}");
        }
    }

    [System.Serializable]
    public class WolfRequest
    {
        public string wallet;
        public string joinCode;
        public WolfRequest(string w, string j) { wallet = w; joinCode = j; }
    }


    public override void OnJoinedRoom()
    {
        // If game already started, prevent entry
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GameStarted") &&
            (bool)PhotonNetwork.CurrentRoom.CustomProperties["GameStarted"])
        {
            Debug.Log("Game already started. Leaving room.");
            PhotonNetwork.LeaveRoom();
        }
    }
}
