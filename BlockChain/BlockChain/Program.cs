using BlockChainP411NEW.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

var blockChain = new BlockChainService();
var displayService = new BlockChainDisplayService();

string choice;

do
{
    Console.WriteLine("\n=== МЕНЮ БЛОКЧЕЙНУ ===");
    Console.WriteLine("1. Додати блок (Майнінг)");
    Console.WriteLine("2. Перевірити валідність блокчейну");
    Console.WriteLine("3. Відобразити блокчейн");
    Console.WriteLine("0. Вийти");
    Console.Write("Ваш вибір: ");

    choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            Console.WriteLine("Введіть дані для нового блоку:");
            var data = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(data))
            {
                using var cts = new CancellationTokenSource();

                // Симуляція мережі (Асинхронне скасування)
                var networkSimulationTask = Task.Run(async () =>
                {
                    var rnd = new Random();
                    // Імітуємо, що хтось інший знайде блок через 2-7 секунд
                    int delay = rnd.Next(2000, 7000);
                    await Task.Delay(delay, cts.Token);

                    if (!cts.Token.IsCancellationRequested)
                    {
                        Console.WriteLine("\n\n[Мережа] Хто не вспiв, той i апаздав");
                        cts.Cancel(); // Надсилаємо сигнал скасування нашому майнеру
                    }
                });

                // Запускаємо локальний майнінг, передаючи токен
                bool isSuccess = await blockChain.AddBlockAsync(data, cts.Token);

                if (isSuccess)
                {
                    Console.WriteLine("\n[Успіх] +15% до соціональних кредитiв.");
                    cts.Cancel(); // Зупиняємо симуляцію мережі, бо ми виграли гонку
                }
                else
                {
                    Console.WriteLine("\n[Відхилено] Не сьогодні рідний)");
                }
            }
            break;

        case "2":
            displayService.PrintValidationResult(blockChain.IsValid());
            break;

        case "3":
            displayService.PrintBlockChain(blockChain.Chain);
            break;

        case "0":
            Console.WriteLine("Завершення роботи...");
            break;

        default:
            Console.WriteLine("Невірний вибір. Будь ласка, введіть число від 0 до 3.");
            break;
    }
} while (choice != "0");