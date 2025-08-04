using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Solana.Unity.SDK;
using System.Collections;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System;

public class Launcher : MonoBehaviourPunCallbacks
{
    public TMP_InputField createInputTMP;
    public GameObject loadingScreen;
    public GameObject wallet;
   

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        ShowLoading("Connecting to server...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        ShowLoading("Joining lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        HideLoading();
        PhotonNetwork.NickName = "Player_" + UnityEngine.Random.Range(1000, 9999);
        Debug.Log("✅ Joined Lobby as " + PhotonNetwork.NickName);
    }

    public void CreateRoom()
    {
        string roomName = string.IsNullOrEmpty(createInputTMP.text)
            ? "Room_" + UnityEngine.Random.Range(1000, 9999)
            : createInputTMP.text;

        StartCoroutine(JoinEscrowCoroutine(roomName));
    }

    private IEnumerator JoinEscrowCoroutine(string joinCode)
    {
        if (Web3.Wallet == null)
        {
            Debug.LogError("❌ Wallet not connected");
            yield break;
        }

        ShowLoading("Requesting escrow info...");
        string baseUrl = "https://bonkus.solfuturenft.fun";
        string url = $"{baseUrl}/escrow/{joinCode}";

        string jsonBody = JsonUtility.ToJson(new JoinRequest(Web3.Wallet.Account.PublicKey.Key));
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Join Room Response: " + request.downloadHandler.text);
            JoinRoomResponse response = JsonUtility.FromJson<JoinRoomResponse>(request.downloadHandler.text);

            Debug.Log($"Escrow Joined: {response.escrowName}");

            // ✅ Wait 10 seconds to let server initialize escrow
            StartCoroutine(DelayedDeposit(joinCode, response));
        }
        else
        {
            Debug.LogError("❌ API Error: " + request.error);
            Debug.LogError("Response Code: " + request.responseCode);
            HideLoading();
        }
    }

    private IEnumerator DelayedDeposit(string joinCode, JoinRoomResponse response)
    {
        yield return new WaitForSeconds(10f); // ⏳ Wait 10 sec for backend setup
        Debug.Log("⏳ 10 seconds passed, calling deposit...");

        _ = JoinEscrowDepositAsync(joinCode, response);
    }

    private async Task JoinEscrowDepositAsync(string roomName, JoinRoomResponse response)
    {
        Debug.Log("🟢 [DEBUG] Entered JoinEscrowDepositAsync for room: " + roomName);
        string txSig = null;

        try
        {
            string tokenMint = "FB8uPKBRWedgFcEbKR51W4Z7hu1MfjocLiyHxLCModbC";
            DepositManager manager = new DepositManager(tokenMint, response.vaultAta, response.vaultAuthPda);

            Debug.Log("🟢 [DEBUG] Calling manager.CallDeposit...");
            try
            {
                txSig = await manager.CallDeposit(response.escrowName, 5000000000);
                Debug.Log("🟢 [DEBUG] CallDeposit completed, result: " + (txSig ?? "NULL"));
            }
            catch (Exception exInner)
            {
                Debug.LogError("❌ [DEBUG] Exception INSIDE CallDeposit: " + exInner.Message + "\n" + exInner.StackTrace);
                txSig = null;
            }

            Debug.Log("🟢 [DEBUG] About to check txSig...");
            if (txSig == null)
            {
                Debug.LogError("❌ [DEBUG] Deposit transaction failed, not proceeding to Next()");
                HideLoading();
                TxCopyButton.Instance.ShowTx("Error: Transaction failed");
            }
            else
            {
                Debug.Log($"✅ [DEBUG] Deposit successful, txSig: {txSig}");
                Next(txSig, roomName);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ [DEBUG] OUTER EXCEPTION: " + ex.Message + "\n" + ex.StackTrace);
            TxCopyButton.Instance.ShowTx("Error: " + ex.Message);
            HideLoading();
        }

        Debug.Log("🟢 [DEBUG] JoinEscrowDepositAsync END");
    }


    public override void OnJoinedRoom()
    {
        HideLoading();
        SetJoinCode(createInputTMP.text);
        PhotonNetwork.LoadLevel("JoinLobby");

    }

    private void Next(string txSig, string roomName)
    {
        Debug.Log("🟢 [DEBUG] Next() called with txSig: " + txSig + " | roomName: " + roomName);

        if (string.IsNullOrEmpty(txSig))
        {
            Debug.LogError("❌ [DEBUG] Next() received null or empty txSig, stopping here.");
            return;
        }
        else
        {

            TxCopyButton.Instance.ShowTx(txSig);
            Debug.Log("🟢 [DEBUG] TxCopyButton displayed.");

            ShowLoading("Creating / Joining room...");
            Debug.Log("🟢 [DEBUG] Loading screen shown.");

            RoomOptions options = new RoomOptions { MaxPlayers = 5 };
            Debug.Log("🟢 [DEBUG] Calling PhotonNetwork.JoinOrCreateRoom...");

            bool joinCall = PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
           
        }
    }


    public static void SetJoinCode(string code)
    {
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["JoinCode"] = code;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        Debug.Log("🟢 JoinCode stored in room properties: " + code);
    }

   

    private void ShowLoading(string message)
    {
        if (loadingScreen != null) loadingScreen.SetActive(true);
    }

    private void HideLoading()
    {
        if (loadingScreen != null) loadingScreen.SetActive(false);
    }

    public void close()
    {
        wallet.SetActive(false);
    }

    [System.Serializable]
    public class JoinRequest
    {
        public string pubkey;
        public JoinRequest(string key) { pubkey = key; }
    }

    [System.Serializable]
    public class JoinRoomResponse
    {
        public bool success;
        public string action;
        public int escrowId;
        public string escrowName;
        public string joinCode;
        public string joinURL;
        public string vaultAuthPda;
        public string vaultAta;
        public int contributorCount;
        public string initializeTxSig;
    }
}
