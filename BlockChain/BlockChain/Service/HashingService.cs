using BlockChainP411NEW.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP411NEW.Services
{
    public class HashingService
    {
        // Метод для обчислення хешу блоку
        public string ComputeHash(Block block)
        {
            // Створюємо рядок, який містить всі дані блоку для обчислення хешу
            string blockData = $"{block.Index}{block.TimeStamp}{block.Data}{block.PreviousHash}{block.Nonce}";
            return ComputeHash(blockData);
        }

        // Метод для обчислення хешу на основі рядка
        public string ComputeHash(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = SHA256.HashData(inputBytes);
            return Convert.ToHexString(hashBytes);
        }
    }
}