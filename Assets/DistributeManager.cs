using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using Solana.Unity.Programs;
using BonkEscrowFinal.Program;
using Solana.Unity.SDK;   // generated IDL namespace

public class DepositManager
{
    private readonly IRpcClient rpcClient;
    private readonly PublicKey programId;
    private readonly PublicKey tokenMint;
    private readonly PublicKey vaultAta;
    private readonly PublicKey vaultAuth;

    public DepositManager(string tokenMintAddress, string VaultAta, string VaultAuth, string rpcUrl = "https://api.devnet.solana.com")
    {
        rpcClient = ClientFactory.GetClient(rpcUrl);
        programId = new PublicKey(BonkEscrowFinalProgram.ID);
        tokenMint = new PublicKey(tokenMintAddress);
        vaultAta = new PublicKey(VaultAta);
        vaultAuth = new PublicKey(VaultAuth);
    }

    // ✅ Derive Escrow PDA
    private PublicKey DeriveEscrowPda(string escrowName)
    {
        PublicKey.TryFindProgramAddress(new[]
        {
            Encoding.UTF8.GetBytes("escrow"),
            new PublicKey("HivwsM8SkM6TELYg2VYFFGEYgTRWmbtsaEH2iX22qsSp").KeyBytes,
            Encoding.UTF8.GetBytes(escrowName)
        }, programId, out PublicKey escrowPda, out _);
        return escrowPda;
    }

    // ✅ Derive VaultAuth PDA
    private PublicKey DeriveVaultAuth(PublicKey escrowPda)
    {
        PublicKey.TryFindProgramAddress(new[]
        {
            Encoding.UTF8.GetBytes("vault_auth"),
            escrowPda.KeyBytes
        }, programId, out PublicKey vaultAuth, out _);
        return vaultAuth;
    }

    // ✅ Get Vault ATA
  

    // ✅ Get Contributor ATA
    private PublicKey GetContributorAta()
    {
        return AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(Web3.Wallet.Account.PublicKey, tokenMint);
    }

    // ✅ Send Deposit Instruction
    public async Task<string> CallDeposit(string escrowName, ulong amount)
    {
        // ✅ 1. Derive PDAs
        PublicKey escrowPda = DeriveEscrowPda(escrowName);
        PublicKey contributorAta = GetContributorAta();

        // ✅ 2. Prepare Accounts
        var accounts = new DepositAccounts
        {
            Escrow = escrowPda,
            Contributor = Web3.Wallet.Account.PublicKey,
            ContributorAta = contributorAta,
            VaultAta = vaultAta,
            VaultAuth = vaultAuth,
            TokenProgram = TokenProgram.ProgramIdKey,
        };

        // ✅ 3. Build Instruction
        TransactionInstruction ix = BonkEscrowFinalProgram.Deposit(accounts, escrowName, amount, programId);

        // ✅ 4. Get Latest Blockhash
        var rpcClient = ClientFactory.GetClient(Cluster.DevNet);
        var recentHash = (await rpcClient.GetLatestBlockHashAsync()).Result.Value.Blockhash;

        // ✅ 5. Build Transaction with TransactionBuilder
        var tx = new TransactionBuilder()
            .SetFeePayer(Web3.Wallet.Account)
            .SetRecentBlockHash(recentHash)
            .AddInstruction(ix)
            .Build(Web3.Wallet.Account);

        // ✅ 6. Send Transaction
        var sendResult = await rpcClient.SendTransactionAsync(tx);
        if (sendResult.WasSuccessful)
        {
            Debug.Log($"✅ Deposit Tx Sent: {sendResult.Result}");
            return sendResult.Result;
        }
        else
        {
            Debug.LogError($"❌ Tx Failed: {sendResult.Reason} | Full Error: {sendResult.RawRpcResponse}");
            return null;
        }
    }

}
