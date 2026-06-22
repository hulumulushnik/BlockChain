using System;
using System.Security.Cryptography;
using System.Text;

namespace BlockChainP411NEW.Models
{
    public class Transaction
    {
        public string Id { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public decimal Amount { get; set; }
        public DateTime TimeStamp { get; set; }
        public byte[]? SenderPublicKey { get; set; } // Тепер може бути null для COINBASE
        public byte[]? Signature { get; set; }

        public Transaction(string from, string to, decimal amount, byte[]? senderPublicKey)
        {
            From = from;
            To = to;
            Amount = amount;
            SenderPublicKey = senderPublicKey;
            TimeStamp = DateTime.UtcNow;
            Id = GenerateHashId();
        }

        public string ToRawString()
        {
            return $"From:{From}|To:{To}|Amount:{Amount}";
        }

        public byte[] GetDataToSign()
        {
            return Encoding.UTF8.GetBytes(ToRawString());
        }

        private string GenerateHashId()
        {
            string rawData = ToRawString();
            byte[] bytes = Encoding.UTF8.GetBytes(rawData);
            byte[] hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexString(hashBytes);
        }
    }
}