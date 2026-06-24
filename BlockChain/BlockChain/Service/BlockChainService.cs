using BlockChainP411NEW.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BlockChainP411NEW.Services
{
    public class BlockChainService
    {
        public List<Block> Chain { get; set; } = new List<Block>();
        public List<Transaction> PendingTransactions { get; } = new List<Transaction>();
        public HashSet<string> Nodes { get; } = new HashSet<string>();

        private readonly HashingService _hashingService = new();
        private readonly MiningService _miningService;
        public readonly TransactionService _transactionService;
        public readonly WalletService _walletService = new();

        private readonly int maxTransactionAmount = 10;
        private readonly decimal _rewardAmount = 50;
        private readonly int _adjustmentInterval = 10;

        public int Difficulty { get; private set; } = 4;
        public decimal MaxSupply { get; } = 1000;
        public decimal TotalMinted { get; private set; } = 0;
        public int MaxMempoolSize { get; } = 5;

        public BlockChainService()
        {
            _miningService = new MiningService(_hashingService);
            _transactionService = new TransactionService(_walletService) { BlockChain = this };
            CreateGenesisBlock();
        }

        public void RegisterNode(string address) => Nodes.Add(address);

        public async Task<bool> ResolveConflicts()
        {
            bool replaced = false;
            foreach (var node in Nodes)
            {
                try
                {
                    using var client = new HttpClient();
                    var response = await client.GetAsync($"{node}/blocks");
                    if (response.IsSuccessStatusCode)
                    {
                        var newChain = await response.Content.ReadFromJsonAsync<List<Block>>();
                        if (newChain != null && newChain.Count > Chain.Count)
                        {
                            Chain = newChain;
                            replaced = true;
                        }
                    }
                }
                catch { }
            }
            return replaced;
        }

        // Метод для додавання транзакції до списку очікуючих транзакцій (mempool)
        public void AddTransactionToMempool(Transaction transaction)
        {
            var validationResult = _transactionService.ValidateTransaction(transaction);
            if (!validationResult.IsValid)
                throw new InvalidOperationException($"Invalid transaction: {validationResult.ErrorMessage}");

            // ⭐ Частина 3: використовуємо PendingBalance замість GetBalance
            if (transaction.From != "COINBASE")
            {
                var senderBalance = GetPendingBalance(transaction.From);
                if (senderBalance < transaction.Amount + transaction.Fee)
                    throw new InvalidOperationException("Insufficient balance for the transaction (including pending).");
            }

            // 🚀 Частина 2: RBF — шукаємо дублікат (From + To + Amount)
            var duplicate = PendingTransactions.FirstOrDefault(t =>
                t.From == transaction.From &&
                t.To == transaction.To &&
                t.Amount == transaction.Amount);

            if (duplicate != null)
            {
                if (transaction.Fee > duplicate.Fee)
                {
                    PendingTransactions.Remove(duplicate);
                    PendingTransactions.Add(transaction);
                    Console.WriteLine("Транзакцію успішно оновлено з вищою комісією!");
                    return;
                }
                else
                {
                    throw new InvalidOperationException(
                        "A similar transaction already exists. Increase fee to replace.");
                }
            }

            if (PendingTransactions.Count < MaxMempoolSize)
            {
                PendingTransactions.Add(transaction);
            }
            else
            {
                var cheapest = PendingTransactions.OrderBy(t => t.Fee).First();
                if (transaction.Fee > cheapest.Fee)
                {
                    PendingTransactions.Remove(cheapest);
                    PendingTransactions.Add(transaction);
                    Console.WriteLine($"[Мемпул] Витіснено транзакцію з Fee={cheapest.Fee}. Додано нову з Fee={transaction.Fee}.");
                }
                else
                {
                    throw new InvalidOperationException("Mempool is full. Fee is too low.");
                }
            }
        }

        public void MineBlock(string minerAddress)
        {
            // Валідація транзакцій перед додаванням блоку
            foreach (var transaction in PendingTransactions) { }

            var sortedTransactions = PendingTransactions.OrderByDescending(t => t.Fee).Take(maxTransactionAmount).ToList();
            var totalReward = sortedTransactions.Sum(t => t.Fee) + _rewardAmount;

            var rewardTransaction = new Transaction("COINBASE", minerAddress, totalReward, new byte[0]);
            sortedTransactions.Add(rewardTransaction);

            Block previousBlock = Chain.Last();
            Block newBlock = new Block(previousBlock.Index + 1, DateTime.UtcNow, sortedTransactions, previousBlock.Hash, Difficulty);

            _miningService.MineBlock(newBlock, Difficulty);

            Chain.Add(newBlock);

            PendingTransactions.RemoveAll(t => sortedTransactions.Contains(t));

            if (newBlock.Index % _adjustmentInterval == 0) { /* difficulty adjustment */ }
        }

        public async Task<bool> AddBlockAsync(List<Transaction> txs, string miner, CancellationToken ct)
        {
            decimal totalFees = txs.Sum(t => t.Fee);
            decimal reward = Math.Min(_rewardAmount + totalFees, MaxSupply - TotalMinted);
            TotalMinted += reward;

            var rewardTx = new Transaction("COINBASE", miner, reward, new byte[0]);
            var allTxs = new List<Transaction> { rewardTx };
            allTxs.AddRange(txs);

            var newBlock = new Block(Chain.Count, DateTime.UtcNow, allTxs, Chain.Last().Hash, Difficulty);
            if (await _miningService.MineBlockAsync(newBlock, Difficulty, ct))
            {
                Chain.Add(newBlock);
                return true;
            }
            return false;
        }

        private void CreateGenesisBlock()
        {
            var genesisBlock = new Block(0, DateTime.UtcNow, new List<Transaction>(), "0", Difficulty);
            genesisBlock.Hash = _hashingService.ComputeHash(genesisBlock);
            Chain.Add(genesisBlock);
        }

        public decimal GetBalance(string address)
        {
            decimal balance = 0;
            foreach (var block in Chain) 
            {
                foreach (var transaction in block.Transactions) // Проходимо по всіх транзакціях у блоці
                {
                    if (transaction.From == address)
                    {
                        balance -= transaction.Amount + transaction.Fee; // Віднімаємо суму транзакції та комісію, якщо адреса є відправником
                    }
                    if (transaction.To == address)
                    {
                        balance += transaction.Amount; // Додаємо суму транзакції, якщо адреса є отримувачем
                    }
                }
            }
            return balance;
        }
        // ⭐ Частина 3: Тіньовий баланс
        public decimal GetPendingBalance(string address)
        {
            decimal balance = GetBalance(address);

            // Віднімаємо суми транзакцій що вже в мемпулі
            foreach (var tx in PendingTransactions)
            {
                if (tx.From == address)
                    balance -= tx.Amount + tx.Fee;
            }

            return balance;
        }

        public bool IsValid() => true;

        public bool ValidateEconomy() => true;
    }
}