using UnityEngine;

public class TestDeposit : MonoBehaviour
{
    [System.Obsolete]
    async void Start()
    {
        string tokenMint = "FB8uPKBRWedgFcEbKR51W4Z7hu1MfjocLiyHxLCModbC";
        string escrowName = "escrow-5e986fcd";   // must match `initialize`
        string vault_auth = "HbwvNq6kMPzddgkd6W29Ps6n9NRPfCYxyaVWpde5frv1";
        string vaultAta = "5kw63qR44hKdgnPfpbv24vGBm1xfngpcCNKpvM5sHEz5"; // Vault ATA address

        DepositManager manager = new DepositManager(tokenMint, vaultAta,vault_auth);

        string txSig = await manager.CallDeposit(escrowName, 5000000000);
        Debug.Log($"Deposit Tx Signature: {txSig}");
    }
}
