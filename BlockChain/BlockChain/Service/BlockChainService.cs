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
        public decimal RewardAmount { get; } = 50;
        public decimal MaxSupply { get; } = 1000;
        public decimal TotalMinted { get; private set; } = 0;

        private readonly double _targetBlockTime = 10.0;
        private readonly int _adjustmentInterval = 2;

        public BlockChainService()
        {
            Chain = new List<Block>();
            _hashingService = new HashingService();
            _miningService = new MiningService(_hashingService);
            _walletService = new WalletService();
            _transactionService = new TransactionService(_walletService);

            _transactionService.BlockChain = this;

            Difficulty = 4;
            CreateGenesisBlock();
        }

        public decimal GetBalance(string address)
        {
            decimal balance = 0;
            foreach (var block in Chain)
            {
                foreach (var transaction in block.Transactions)
                {
                    if (transaction.From == address) balance -= transaction.Amount;
                    if (transaction.To == address) balance += transaction.Amount;
                }
            }
            return balance;
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

            if (avgTime < _targetBlockTime)
            {

                Difficulty = Math.Min(Difficulty + 1, 4);
            }
            else if (avgTime > _targetBlockTime)
            {
                Difficulty = Math.Max(1, Difficulty - 1);
            }
        }

        public async Task<bool> AddBlockAsync(List<Transaction> pendingTransactions, string minerAddress, CancellationToken cancellationToken)
        {
            AdjustDifficulty();

            var validTransactions = new List<Transaction>();
            var tempBalances = new Dictionary<string, decimal>();

            decimal GetTempBalance(string address)
            {
                if (!tempBalances.ContainsKey(address))
                    tempBalances[address] = GetBalance(address);
                return tempBalances[address];
            }

            foreach (var tx in pendingTransactions)
            {
                decimal currentBalance = GetTempBalance(tx.From);
                if (currentBalance < tx.Amount)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ЗАХИСТ] Транзакція {tx.Id} ВІДХИЛЕНА: Double Spend! (На балансі: {currentBalance}, спроба витратити: {tx.Amount})");
                    Console.ResetColor();
                    continue;
                }

                tempBalances[tx.From] -= tx.Amount;
                validTransactions.Add(tx);
            }

            decimal currentReward = 0;
            if (TotalMinted < MaxSupply)
            {
                decimal availableToMint = MaxSupply - TotalMinted;
                currentReward = Math.Min(RewardAmount, availableToMint);
                TotalMinted += currentReward;

                var rewardTransaction = new Transaction("COINBASE", minerAddress, currentReward, null);
                validTransactions.Insert(0, rewardTransaction);
            }

            List<Transaction> acceptedTransactions = new List<Transaction>();
            int currentBlockSizeBytes = 0;

            foreach (var tx in validTransactions)
            {
                int txSize = System.Text.Encoding.UTF8.GetByteCount(tx.ToRawString());
                if (currentBlockSizeBytes + txSize <= MaxBlockSizeBytes)
                {
                    acceptedTransactions.Add(tx);
                    currentBlockSizeBytes += txSize;
                }
                else break;
            }

            var lastBlock = Chain.Last();
            var newBlock = new Block(lastBlock.Index + 1, DateTime.UtcNow, acceptedTransactions, lastBlock.Hash, Difficulty);

            bool isMined = await _miningService.MineBlockAsync(newBlock, Difficulty, cancellationToken);

            if (isMined)
            {
                Chain.Add(newBlock);
                return true;
            }
            return false;
        }

        public bool ValidateEconomy()
        {
            HashSet<string> uniqueAddresses = new HashSet<string>();

            foreach (var block in Chain)
            {
                foreach (var tx in block.Transactions)
                {
                    if (tx.From != "COINBASE") uniqueAddresses.Add(tx.From);
                    uniqueAddresses.Add(tx.To);
                }
            }

            decimal totalCirculating = 0;
            foreach (var address in uniqueAddresses)
            {
                totalCirculating += GetBalance(address);
            }

            Console.WriteLine($"\n[АУДИТ ЕКОНОМІКИ]");
            Console.WriteLine($"Загальна емісія (TotalMinted): {TotalMinted}");
            Console.WriteLine($"Грошей на руках (Circulating): {totalCirculating}");

            return totalCirculating == TotalMinted;
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