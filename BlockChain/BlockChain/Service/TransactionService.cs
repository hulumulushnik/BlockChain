using BlockChainP411NEW.Models;
using System;

namespace BlockChainP411NEW.Services
{
    public class TransactionService
    {
        private readonly WalletService _walletService;
        public BlockChainService BlockChain { get; set; } // Додано для доступу до балансу

        public TransactionService(WalletService walletService)
        {
            _walletService = walletService;
        }

        public Transaction CreateTransaction(Wallet walletFrom, string to, decimal amount)
        {
            // Перевірка балансу відправника
            if (BlockChain != null)
            {
                decimal balance = BlockChain.GetBalance(walletFrom.Address);
                if (balance < amount)
                {
                    throw new ArgumentException($"Insufficient funds. Available: {balance}, Required: {amount}");
                }
            }

            var tx = new Transaction(walletFrom.Address, to, amount, walletFrom.PublicKey);
            tx.Signature = walletFrom.Sign(tx.GetDataToSign());

            var validation = ValidateTransaction(tx);
            if (!validation.IsValid)
            {
                throw new ArgumentException(validation.ErrorMessage);
            }
            return tx;
        }

        public (bool IsValid, string ErrorMessage) ValidateTransaction(Transaction transaction)
        {
            if (transaction == null) return (false, "Transaction cannot be null.");
            if (string.IsNullOrWhiteSpace(transaction.From)) return (false, "Sender cannot be empty.");
            if (string.IsNullOrWhiteSpace(transaction.To)) return (false, "Recipient cannot be empty.");
            if (transaction.Amount <= 0) return (false, "Amount must be greater than zero.");

            // Якщо це системна винагорода за майнінг - пропускаємо перевірку підпису
            if (transaction.From == "COINBASE")
                return (true, string.Empty);

            if (!IsValidCryptoAddress(transaction.From)) return (false, "Invalid Sender format.");
            if (!IsValidCryptoAddress(transaction.To)) return (false, "Invalid Recipient format.");
            if (transaction.Signature == null || transaction.Signature.Length == 0) return (false, "Signature is missing.");
            if (transaction.SenderPublicKey == null || transaction.SenderPublicKey.Length == 0) return (false, "Public key is missing.");

            string publicKeyBase64 = Convert.ToBase64String(transaction.SenderPublicKey);
            bool isSignatureValid = _walletService.VerifySignature(publicKeyBase64, transaction.GetDataToSign(), transaction.Signature);

            if (!isSignatureValid)
            {
                return (false, "Invalid digital signature! Transaction may be forged.");
            }

            return (true, string.Empty);
        }

        private bool IsValidCryptoAddress(string address)
        {
            if (address.Length != 42) return false;
            if (!address.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return false;
            for (int i = 2; i < address.Length; i++)
            {
                if (!char.IsLetterOrDigit(address[i])) return false;
            }
            return true;
        }
    }
}