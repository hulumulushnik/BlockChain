using BlockChainP411NEW.Models;
using System;
using System.Security.Cryptography;

namespace BlockChainP411NEW.Services
{
    public class WalletService
    {
        public Wallet CreateWallet(string name)
        {
            using var ecdsa = ECDsa.Create();

            byte[] privateKey = ecdsa.ExportECPrivateKey();
            byte[] publicKey = ecdsa.ExportSubjectPublicKeyInfo();

            string hexKey = Convert.ToHexString(publicKey).ToLower();
            string address = "0x" + hexKey.Substring(0, 40);

            return new Wallet(name, address, publicKey, privateKey);
        }

        public bool VerifySignature(string publicKeyBase64, byte[] data, byte[] signature)
        {
            try
            {
                using var ecdsa = ECDsa.Create();
                byte[] publicKey = Convert.FromBase64String(publicKeyBase64);

                ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
                return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
            }
            catch
            {
                return false;
            }
        }
    }
}