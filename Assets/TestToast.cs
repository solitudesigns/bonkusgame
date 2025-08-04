using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TxCopyButton : MonoBehaviour
{
    public static TxCopyButton Instance;   // ✅ Global Access
    public Text buttonText;                // Legacy UI Text

    private Button copyButton;
    private string currentTx = "";

    void Awake()
    {
        // ✅ Singleton setup
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        copyButton = GetComponent<Button>();
        copyButton.onClick.AddListener(CopyToClipboard);

        // Hide at start
        gameObject.SetActive(false);
    }

    /// <summary>
    /// ✅ Call this from any script: TxCopyButton.Instance.ShowTx("yourTxID");
    /// </summary>
    public void ShowTx(string txSig)
    {
        currentTx = txSig;
        buttonText.text = txSig;
        gameObject.SetActive(true);

        // Auto copy
        GUIUtility.systemCopyBuffer = txSig;
        Debug.Log($"📋 Tx Copied: {txSig}");

        // Hide after 8 sec
        StopAllCoroutines();
        StartCoroutine(HideAfterDelay(8f));
    }

    private void CopyToClipboard()
    {
        GUIUtility.systemCopyBuffer = currentTx;
        Debug.Log($"📋 Tx Copied Again: {currentTx}");
    }

    private IEnumerator HideAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        gameObject.SetActive(false);
    }
}
