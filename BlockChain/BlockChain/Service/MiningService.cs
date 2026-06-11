using BlockChainP411NEW.Models;
using System;
using System.Collections.Generic;
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

        // Тепер метод асинхронний і повертає bool (успіх або скасування)
        public async Task<bool> MineBlockAsync(Block block, int difficulty, CancellationToken cancellationToken)
        {
            string target = new string('0', difficulty);
            int coreCount = Environment.ProcessorCount; // Отримуємо кількість доступних ядер
            Console.WriteLine($"\n[Система] Запуск майнінгу. Задіяно логічних ядер: {coreCount}");

            // Об'єднуємо зовнішній токен (мережа) з внутрішнім (щоб зупинити інші потоки, якщо один знайде хеш)
            using var internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            int? foundNonce = null;
            string foundHash = string.Empty;
            object lockObj = new object(); // Об'єкт для блокування доступу до спільних змінних при знаходженні

            var tasks = new List<Task>();

            // Формуємо базовий рядок даних ОДИН раз, щоб уникнути Race Condition та зайвих обчислень
            string baseData = $"{block.Index}{block.TimeStamp}{block.Data}{block.PreviousHash}";

            for (int i = 0; i < coreCount; i++)
            {
                int threadOffset = i; // Локальна копія змінного циклу для замикання (closure)

                tasks.Add(Task.Run(() =>
                {
                    int localNonce = threadOffset;

                    // Цикл працює, поки не буде запиту на скасування (від мережі або іншого потоку)
                    while (!internalCts.Token.IsCancellationRequested)
                    {
                        // Кожен потік формує власний унікальний рядок
                        string blockData = $"{baseData}{localNonce}";
                        string hash = _hashingService.ComputeHash(blockData);

                        if (hash.StartsWith(target))
                        {
                            // Критична секція: фіксуємо результат
                            lock (lockObj)
                            {
                                if (!internalCts.Token.IsCancellationRequested)
                                {
                                    foundNonce = localNonce;
                                    foundHash = hash;
                                    internalCts.Cancel(); // Зупиняємо всі інші потоки
                                }
                            }
                            break;
                        }

                        // Стратегія кроку (stride)
                        localNonce += coreCount;

                        // Вивід крапок для розуміння, що процес іде (зменшено частоту для багатопотоковості)
                        if (localNonce % 1_000_000 == threadOffset)
                        {
                            Console.Write(".");
                        }
                    }
                }, internalCts.Token));
            }

            try
            {
                await Task.WhenAll(tasks); // Чекаємо завершення всіх потоків
            }
            catch (OperationCanceledException)
            {
                // Очікуваний виняток при скасуванні задач
            }

            // Перевіряємо, чи ми знайшли блок, чи нас скасувала зовнішня мережа
            if (foundNonce.HasValue && !cancellationToken.IsCancellationRequested)
            {
                // Лише тепер, коли все обчислено безпечно, оновлюємо стан об'єкта Block
                block.Nonce = foundNonce.Value;
                block.Hash = foundHash;
                return true;
            }

            return false; // Майнінг перервано ззовні
        }
    }
}