using BlockChainP411NEW.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlockChainP411NEW.Services
{
    public class BlockChainService
    {
        public List<Block> Chain { get; set; }

        private readonly HashingService _hashingService;
        private readonly MiningService _miningService;
        public readonly TransactionService _transactionService;
        public readonly WalletService _walletService;

        public int Difficulty { get; private set; }
        public int MaxBlockSizeBytes { get; } = 256;
        private readonly double _targetBlockTime = 10.0;
        private readonly int _adjustmentInterval = 2;

        public BlockChainService()
        {
            Chain = new List<Block>();
            _hashingService = new HashingService();
            _miningService = new MiningService(_hashingService);
            _walletService = new WalletService();
            _transactionService = new TransactionService(_walletService);
            Difficulty = 4;
            CreateGenesisBlock();
        }

        private void CreateGenesisBlock()
        {
            var genesisBlock = new Block(0, DateTime.UtcNow, new List<Transaction>(), "0", Difficulty);
            genesisBlock.Hash = _hashingService.ComputeHash(genesisBlock);
            Chain.Add(genesisBlock);
        }

        private void AdjustDifficulty()
        {
            if (Chain.Count < _adjustmentInterval) return;

            var recentBlocks = Chain.Skip(Math.Max(0, Chain.Count - _adjustmentInterval)).ToList();
            double avgTime = recentBlocks.Average(b => b.MiningDuration);

            if (avgTime < _targetBlockTime) Difficulty++;
            else if (avgTime > _targetBlockTime) Difficulty = Math.Max(1, Difficulty - 1);
        }

        public async Task<bool> AddBlockAsync(List<Transaction> pendingTransactions, CancellationToken cancellationToken)
        {
            AdjustDifficulty();
            var lastBlock = Chain.Last();
            var newBlock = new Block(lastBlock.Index + 1, DateTime.UtcNow, pendingTransactions, lastBlock.Hash, Difficulty);

            bool isMined = await _miningService.MineBlockAsync(newBlock, Difficulty, cancellationToken);

            if (isMined)
            {
                Chain.Add(newBlock);
                return true;
            }
            return false;
        }

        public async Task ProcessTransactionsAsync(List<Transaction> incomingTransactions)
        {
            var currentChunk = new List<Transaction>();
            int currentBytes = 0;

            Console.WriteLine($"\n[Smart Chunking] Початок пакування {incomingTransactions.Count} транзакцій...");

            foreach (var tx in incomingTransactions)
            {
                int txSize = System.Text.Encoding.UTF8.GetByteCount(tx.ToRawString());

                if (txSize > MaxBlockSizeBytes)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[Відхилено] Транзакція {tx.Id} важить {txSize} байт (більше ліміту {MaxBlockSizeBytes}).");
                    Console.ResetColor();
                    continue;
                }

                if (currentBytes + txSize > MaxBlockSizeBytes)
                {
                    Console.WriteLine($"\n[Пакування] Блок заповнено ({currentBytes}/{MaxBlockSizeBytes} байт). Запуск майнінгу...");
                    await AddBlockAsync(new List<Transaction>(currentChunk), CancellationToken.None);

                    currentChunk.Clear();
                    currentBytes = 0;
                }

                currentChunk.Add(tx);
                currentBytes += txSize;
            }

            if (currentChunk.Count > 0)
            {
                Console.WriteLine($"\n[Пакування] Пакування залишку ({currentBytes}/{MaxBlockSizeBytes} байт). Запуск майнінгу...");
                await AddBlockAsync(new List<Transaction>(currentChunk), CancellationToken.None);
            }

            Console.WriteLine("\n[Smart Chunking] Всі транзакції успішно розподілені та запаковані в блоки!\n");
        }

        public bool IsValid()
        {
            for (int i = 1; i < Chain.Count; i++)
            {
                var currentBlock = Chain[i];
                var previousBlock = Chain[i - 1];

                if (currentBlock.Hash != _hashingService.ComputeHash(currentBlock)) return false;
                if (currentBlock.PreviousHash != previousBlock.Hash) return false;
                if (!currentBlock.Hash.StartsWith(new string('0', currentBlock.Difficulty))) return false;
                if (currentBlock.TimeStamp <= previousBlock.TimeStamp) return false;
                if (currentBlock.MiningDuration <= 0) return false;

                foreach (var tx in currentBlock.Transactions)
                {
                    if (!_transactionService.ValidateTransaction(tx).IsValid) return false;
                }
            }
            return true;
        }
    }
}