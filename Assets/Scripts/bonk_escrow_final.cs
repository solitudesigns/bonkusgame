using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Solana.Unity;
using Solana.Unity.Programs.Abstract;
using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using BonkEscrowFinal;
using BonkEscrowFinal.Program;
using BonkEscrowFinal.Errors;
using BonkEscrowFinal.Accounts;
using BonkEscrowFinal.Types;

namespace BonkEscrowFinal
{
    namespace Accounts
    {
        public partial class EscrowState
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 7846821100369762835UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{19, 90, 148, 111, 55, 130, 229, 108};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "4EksRoDVFRd";
            public PublicKey Owner { get; set; }

            public PublicKey TokenMint { get; set; }

            public PublicKey[] Contributors { get; set; }

            public bool Distributed { get; set; }

            public string Name { get; set; }

            public static EscrowState Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                EscrowState result = new EscrowState();
                result.Owner = _data.GetPubKey(offset);
                offset += 32;
                result.TokenMint = _data.GetPubKey(offset);
                offset += 32;
                int resultContributorsLength = (int)_data.GetU32(offset);
                offset += 4;
                result.Contributors = new PublicKey[resultContributorsLength];
                for (uint resultContributorsIdx = 0; resultContributorsIdx < resultContributorsLength; resultContributorsIdx++)
                {
                    result.Contributors[resultContributorsIdx] = _data.GetPubKey(offset);
                    offset += 32;
                }

                result.Distributed = _data.GetBool(offset);
                offset += 1;
                offset += _data.GetBorshString(offset, out var resultName);
                result.Name = resultName;
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum BonkEscrowFinalErrorKind : uint
        {
            MaxContributorsReached = 6000U,
            AlreadyDeposited = 6001U,
            InvalidDepositAmount = 6002U,
            Unauthorized = 6003U,
            NotFull = 6004U,
            AlreadyDistributed = 6005U,
            InvalidMode = 6006U,
            NameTooLong = 6007U,
            MissingRecipientAta = 6008U,
            NameMismatch = 6009U,
            InvalidTarget = 6010U
        }
    }

    namespace Types
    {
    }

    public partial class BonkEscrowFinalClient : TransactionalBaseClient<BonkEscrowFinalErrorKind>
    {
        public BonkEscrowFinalClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId = null) : base(rpcClient, streamingRpcClient, programId ?? new PublicKey(BonkEscrowFinalProgram.ID))
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<EscrowState>>> GetEscrowStatesAsync(string programAddress = BonkEscrowFinalProgram.ID, Commitment commitment = Commitment.Confirmed)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = EscrowState.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<EscrowState>>(res);
            List<EscrowState> resultingAccounts = new List<EscrowState>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => EscrowState.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<EscrowState>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<EscrowState>> GetEscrowStateAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<EscrowState>(res);
            var resultingAccount = EscrowState.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<EscrowState>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribeEscrowStateAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, EscrowState> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                EscrowState parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = EscrowState.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        protected override Dictionary<uint, ProgramError<BonkEscrowFinalErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<BonkEscrowFinalErrorKind>>{{6000U, new ProgramError<BonkEscrowFinalErrorKind>(BonkEscrowFinalErrorKind.MaxContributorsReached, "Max 5 contributors allowed")}, {6001U, new ProgramError<BonkEscrowFinalErrorKind>(BonkEscrowFinalErrorKind.AlreadyDeposited, "Contributor already deposited")}, {6002U, new ProgramError<BonkEscrowFinalErrorKind>(BonkEscrowFinalErrorKind.InvalidDepositAmount, "Deposit must be exactly 5 tokens")}, {6003U, new ProgramError<BonkEscrowFinalErrorKind>(BonkEscrowFinalErrorKind.Unauthorized, "Unauthorized")}, {6004U, new ProgramError<BonkEscrowFinalErrorKind>(BonkEscrowFinalErrorKind.NotFull, "Not all contributors have deposited")}, {6005U, new ProgramError<BonkEscrowFinalErrorKind>(BonkEscrowFinalErrorKind.AlreadyDistributed, "Already distributed")}, {6006U, new ProgramError<BonkEscrowFinalErrorKind>(BonkEscrowFinalErrorKind.InvalidMode, "Invalid distribution mode")}, {6007U, new ProgramError<BonkEscrowFinalErrorKind>(BonkEscrowFinalErrorKind.NameTooLong, "Name too long (max 32 bytes)")}, {6008U, new ProgramError<BonkEscrowFinalErrorKind>(BonkEscrowFinalErrorKind.MissingRecipientAta, "Missing recipient ATA in remaining_accounts")}, {6009U, new ProgramError<BonkEscrowFinalErrorKind>(BonkEscrowFinalErrorKind.NameMismatch, "Escrow name does not match")}, {6010U, new ProgramError<BonkEscrowFinalErrorKind>(BonkEscrowFinalErrorKind.InvalidTarget, "Target or excluded contributor is invalid")}, };
        }
    }

    namespace Program
    {
        public class DepositAccounts
        {
            public PublicKey Escrow { get; set; }

            public PublicKey Contributor { get; set; }

            public PublicKey ContributorAta { get; set; }

            public PublicKey VaultAta { get; set; }

            public PublicKey VaultAuth { get; set; }

            public PublicKey TokenProgram { get; set; } = new PublicKey("TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA");
        }

        public class DistributeAccounts
        {
            public PublicKey Escrow { get; set; }

            public PublicKey VaultAta { get; set; }

            public PublicKey VaultAuth { get; set; }

            public PublicKey Owner { get; set; }

            public PublicKey TokenProgram { get; set; } = new PublicKey("TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA");
        }

        public class InitializeAccounts
        {
            public PublicKey Escrow { get; set; }

            public PublicKey Owner { get; set; }

            public PublicKey Mint { get; set; }

            public PublicKey VaultAuth { get; set; }

            public PublicKey VaultAta { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
            public PublicKey TokenProgram { get; set; } = new PublicKey("TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA");
            public PublicKey AssociatedTokenProgram { get; set; } = new PublicKey("ATokenGPvbdGVxr1b2hvZbsiqW5xWH25efTNsLJA8knL");
            public PublicKey Rent { get; set; } = new PublicKey("SysvarRent111111111111111111111111111111111");
        }

        public static class BonkEscrowFinalProgram
        {
            public const string ID = "9obCENSCc25Fw6ca4WZNUXQfhYM9xymQGAPkNc5Udsec";
            public static Solana.Unity.Rpc.Models.TransactionInstruction Deposit(DepositAccounts accounts, string name, ulong amount, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Escrow, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Contributor, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.ContributorAta, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.VaultAta, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.VaultAuth, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(13182846803881894898UL, offset);
                offset += 8;
                offset += _data.WriteBorshString(name, offset);
                _data.WriteU64(amount, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction Distribute(DistributeAccounts accounts, string name, byte mode, PublicKey target_pubkey, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Escrow, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.VaultAta, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.VaultAuth, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Owner, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(4431239275985448127UL, offset);
                offset += 8;
                offset += _data.WriteBorshString(name, offset);
                _data.WriteU8(mode, offset);
                offset += 1;
                _data.WritePubKey(target_pubkey, offset);
                offset += 32;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction Initialize(InitializeAccounts accounts, string name, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Escrow, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Owner, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Mint, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.VaultAuth, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.VaultAta, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Rent, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(17121445590508351407UL, offset);
                offset += 8;
                offset += _data.WriteBorshString(name, offset);
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}