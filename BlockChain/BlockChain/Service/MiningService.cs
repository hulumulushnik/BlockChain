using BlockChainP411NEW.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BlockChainP411NEW.Services
{
    public class MiningService
    {
        private readonly HashingService _hashingService;

        public MiningService(HashingService hashingService)
        {
            _hashingService = hashingService;
        }

        public async Task<bool> MineBlockAsync(Block block, int difficulty, CancellationToken cancellationToken)
        {
            string target = new string('0', difficulty);
            int coreCount = Environment.ProcessorCount;

            Console.WriteLine($"\n[Система] Запуск майнінгу. Задіяно логічних ядер: {coreCount}");
            Console.WriteLine($"[Система] Складність (Difficulty): {difficulty}");

            using var internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            int? foundNonce = null;
            string foundHash = string.Empty;
            object lockObj = new object();

            long totalHashes = 0;
            const int batchSize = 50_000;

            var tasks = new List<Task>();
            string baseData = $"{block.Index}{block.TimeStamp}{block.Data}{block.PreviousHash}";

            Stopwatch stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < coreCount; i++)
            {
                int threadOffset = i;

                tasks.Add(Task.Run(() =>
                {
                    int localNonce = threadOffset;
                    long localHashes = 0; 

                    while (!internalCts.Token.IsCancellationRequested)
                    {
                        string blockData = $"{baseData}{localNonce}";
                        string hash = _hashingService.ComputeHash(blockData);
                        localHashes++; 

                        if (hash.StartsWith(target))
                        {
                            lock (lockObj)
                            {
                                if (!internalCts.Token.IsCancellationRequested)
                                {
                                    foundNonce = localNonce;
                                    foundHash = hash;
                                    internalCts.Cancel();
                                }
                            }
                            break;
                        }

                        localNonce += coreCount;

                        if (localHashes == batchSize)
                        {
                            Interlocked.Add(ref totalHashes, localHashes);
                            localHashes = 0; // Скидаємо локальний лічильник
                        }
                    }
                    if (localHashes > 0)
                    {
                        Interlocked.Add(ref totalHashes, localHashes);
                    }

                }, internalCts.Token));
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
            }

            stopwatch.Stop();

            if (foundNonce.HasValue && !cancellationToken.IsCancellationRequested)
            {
                block.Nonce = foundNonce.Value;
                block.Hash = foundHash;

                double seconds = stopwatch.Elapsed.TotalSeconds;
                double hashRate = seconds > 0 ? totalHashes / seconds : 0;

                string formattedHashRate;
                if (hashRate >= 1_000_000)
                    formattedHashRate = $"{hashRate / 1_000_000:F2} MH/s (Мегахешів/сек)";
                else if (hashRate >= 1_000)
                    formattedHashRate = $"{hashRate / 1_000:F2} KH/s (Кілохешів/сек)";
                else
                    formattedHashRate = $"{hashRate:F2} H/s (Хешів/сек)";

                Console.WriteLine($"\n=== СТАТИСТИКА МАЙНІНГУ ===");
                Console.WriteLine($"Витрачено часу:      {seconds:F3} сек.");
                Console.WriteLine($"Перевірено комбінацій: {totalHashes:N0}");
                Console.WriteLine($"Швидкість (Hashrate):  {formattedHashRate}");
                Console.WriteLine($"Знайдений Nonce:       {foundNonce.Value}");
                Console.WriteLine($"===========================\n");

                return true;
            }

            return false;
        }
    }
}
