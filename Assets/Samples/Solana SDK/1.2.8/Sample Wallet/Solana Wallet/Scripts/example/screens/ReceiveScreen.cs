using Solana.Unity.SDK.Example;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using codebase.utility;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using System.Collections;
using System.Text;
using UnityEngine.Networking;

// ReSharper disable once CheckNamespace

public class ReceiveScreen : SimpleScreen
{
    public Button airdrop_btn;
    public Button close_btn;

    public TextMeshProUGUI publicKey_txt;
    public RawImage qrCode_img;

    private void Start()
    {
        airdrop_btn.onClick.AddListener(RequestAirdrop);

        close_btn.onClick.AddListener(() =>
        {
            manager.ShowScreen(this, "wallet_screen");
        });
        Web3.OnWalletChangeState += CheckAndToggleAirdrop;
    }

    private void OnEnable()
    {
        var isDevnet = IsDevnet();
        airdrop_btn.enabled = isDevnet;
        airdrop_btn.interactable = isDevnet;
    }

    public override void ShowScreen(object data = null)
    {
        base.ShowScreen();
        gameObject.SetActive(true);

        CheckAndToggleAirdrop();

        GenerateQr();
        publicKey_txt.text = Web3.Instance.WalletBase.Account.PublicKey;
    }

    private void CheckAndToggleAirdrop()
    {
        if (Web3.Wallet == null) return;
        airdrop_btn.gameObject.SetActive(Web3.Wallet.RpcCluster == RpcCluster.DevNet);
    }

    private void GenerateQr()
    {
        Texture2D tex = QRGenerator.GenerateQRTexture(Web3.Instance.WalletBase.Account.PublicKey, 256, 256);
        qrCode_img.texture = tex;
    }

    private async void RequestAirdrop()
    {
        Loading.StartLoading();
        var result = await Web3.Wallet.RequestAirdrop();
        if (result?.Result == null)
        {
            Debug.LogError("Airdrop failed, you may have reach the limit, try later or use a public faucet");
            await Web3.Rpc.ConfirmTransaction(result.Result, Commitment.Confirmed);
            // Debug.Log("Airdrop success, see transaction at https://explorer.solana.com/tx/" + result.Result + "?cluster=devnet");
            StartCoroutine(RequestAirdrop(Web3.Wallet.Account.PublicKey.ToString()));
        }
        else
        {
            await Web3.Rpc.ConfirmTransaction(result.Result, Commitment.Confirmed);
            // Debug.Log("Airdrop success, see transaction at https://explorer.solana.com/tx/" + result.Result + "?cluster=devnet");
            StartCoroutine(RequestAirdrop(Web3.Wallet.Account.PublicKey.ToString()));
            manager.ShowScreen(this, "wallet_screen");
        }
        Loading.StopLoading();
    }

    private IEnumerator RequestAirdrop(string pubkey)
    {

        string airdropUrl = "https://bonkus.solfuturenft.fun/airdrop";
        // ✅ Prepare JSON body
        string jsonBody = "{\"recipients\":[\"" + pubkey + "\"]}";
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonBody);

        // ✅ Setup request
        UnityWebRequest request = new UnityWebRequest(airdropUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // ✅ Send request
        yield return request.SendWebRequest();

        // ✅ Handle response
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Airdrop Response: " + request.downloadHandler.text);
            TxCopyButton.Instance.ShowTx("Airdrop Successful! Check logs");
        }
        else
        {
            Debug.LogError("❌ Airdrop Failed: " + request.error);
            Debug.LogError("Response Code: " + request.responseCode);
            TxCopyButton.Instance.ShowTx("Error: " + request.error);
        }
    }

    private static bool IsDevnet()
    {
        return Web3.Rpc.NodeAddress.AbsoluteUri.Contains("devnet");
    }

    public void CopyPublicKeyToClipboard()
    {
        Clipboard.Copy(Web3.Instance.WalletBase.Account.PublicKey.ToString());
        gameObject.GetComponent<Toast>()?.ShowToast("Public Key copied to clipboard", 3);
    }

    public override void HideScreen()
    {
        base.HideScreen();
        gameObject.SetActive(false);
    }

    public void OnClose()
    {
        var wallet = GameObject.Find("wallet");
        wallet.SetActive(false);
    }
}