using BlockChainP411NEW.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP411NEW.Services
{
    public class BlockChainService
    {
        public List<Block> Chain { get; set; } // Ланцюжок блоків, який зберігає всі блоки в блокчейні

        private readonly HashingService _hashingService; // Сервіс для обчислення хешу блоків, який використовується для забезпечення цілісності даних в блокчейні
        private readonly MiningService miningService; // Сервіс для майнінгу блоків, який відповідає за пошук правильного хешу для блоку

        public int Difficulty { get; private set; } // Встановлюємо складність майнінгу
        public BlockChainService()
        {
            Chain = new List<Block>(); // Ініціалізуємо ланцюжок блоків як порожній список
            _hashingService = new HashingService(); // Ініціалізуємо сервіс для обчислення хешу
            Difficulty = 5; // Встановлюємо складність майнінгу
            miningService = new MiningService(_hashingService); // Ініціалізуємо сервіс для майнінгу, передаючи йому сервіс для обчислення хешу
            CreateGenesisBlock(); // Створюємо генезис-блок, який є першим блоком у ланцюжку і не має попереднього блоку
        }

        // Метод для створення генезис-блоку, який є першим блоком у ланцюжку і не має попереднього блоку
        private void CreateGenesisBlock()
        {
            var genesisBlock = new Block(0, DateTime.UtcNow, "Genesis Block", "0"); // Створюємо генезис-блок з індексом 0, поточним часом, даними "Genesis Block" та попереднім хешем "0"
            genesisBlock.Hash = _hashingService.ComputeHash(genesisBlock);// Обчислюємо хеш для генезис-блоку, використовуючи сервіс для обчислення хешу
            Chain.Add(genesisBlock);
        }

        // Метод для додавання нового блоку до ланцюжка блоків. Він приймає дані для нового блоку, створює новий блок з відповідними параметрами, обчислює його хеш і додає його до ланцюжка.
        public async Task<bool> AddBlockAsync(string data, CancellationToken cancellationToken)
        {
            var lastBlock = Chain.Last();
            var newBlock = new Block(lastBlock.Index + 1, DateTime.UtcNow, data, lastBlock.Hash);

            // Викликаємо асинхронний майнер
            bool isMined = await miningService.MineBlockAsync(newBlock, Difficulty, cancellationToken);

            if (isMined)
            {
                Chain.Add(newBlock);
                return true;
            }
            return false; // Блок не додано (перервано)
        }

        public bool IsValid()
        {
            for (int i = 1; i < Chain.Count; i++)
            {
                var currentBlock = Chain[i];
                var previousBlock = Chain[i - 1];

                // Повіряємо, чи хеш поточного блоку відповідає обчисленому хешу на основі його даних
                if (currentBlock.Hash != _hashingService.ComputeHash(currentBlock))
                {
                    return false; // Якщо хеш не співпадає, ланцюжок блоків є недійсним
                }

                // Повіряємо, чи попередній хеш поточного блоку відповідає хешу попереднього блоку
                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    return false; // Якщо попередній хеш не співпадає, ланцюжок блоків є недійсним
                }

                // Повіряємо, чи хеш поточного блоку відповідає вимогам складності (кількість провідних нулів)
                if (!currentBlock.Hash.StartsWith(new string('0', Difficulty)))
                {
                    return false; // Якщо хеш не відповідає вимогам складності, ланцюжок блоків є недійсним
                }
            }
            return true; // Якщо всі блоки в ланцюжку є дійсними, повертаємо true
        }
        public int GetInvalidBlockIndex()
        {
            for (int i = 1; i < Chain.Count; i++)
            {
                var currentBlock = Chain[i];
                var previousBlock = Chain[i - 1];

                if (currentBlock.Hash != _hashingService.ComputeHash(currentBlock))
                {
                    return currentBlock.Index;
                }

                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    return currentBlock.Index;
                }

                if (!currentBlock.Hash.StartsWith(new string('0', Difficulty)))
                {
                    return currentBlock.Index; 
                }
            }

            return -1;
        }
    }
}