using BlockChainP411NEW.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

// Встановлюємо кодування UTF-8 для коректного відображення кирилиці в консолі
Console.OutputEncoding = System.Text.Encoding.UTF8;

var blockChain = new BlockChainService();
var displayService = new BlockChainDisplayService();

string? choice;

do
{
    Console.WriteLine("\n=== МЕНЮ БЛОКЧЕЙНУ ===");
    Console.WriteLine("1. Додати блок (Запустити багатопотоковий майнінг)");
    Console.WriteLine("2. Перевірити валідність ланцюга (IsValid)");
    Console.WriteLine("3. Знайти пошкоджений блок (GetInvalidBlockIndex)");
    Console.WriteLine("4. Відобразити весь блокчейн");
    Console.WriteLine("5. Симулювати хакерську атаку (змінити блок №1)");
    Console.WriteLine("0. Вийти");
    Console.Write("Ваш вибір: ");

    choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            Console.WriteLine("\nВведіть дані для нового блоку:");
            var data = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(data))
            {
                // Запускаємо асинхронний майнінг. Передаємо CancellationToken.None, 
                // оскільки в цьому завданні ми не перериваємо його мережею.
                await blockChain.AddBlockAsync(data, CancellationToken.None);
                Console.WriteLine("[Успіх] Блок успішно додано до ланцюга!");
            }
            else
            {
                Console.WriteLine("Помилка: Дані не можуть бути порожніми.");
            }
            break;

        case "2":
            Console.WriteLine();
            displayService.PrintValidationResult(blockChain.IsValid());
            break;

        case "3":
            Console.WriteLine("\n--- ДЕТЕКТОР ПОШКОДЖЕНЬ ---");
            int invalidIndex = blockChain.GetInvalidBlockIndex();
            if (invalidIndex != -1)
            {
                Console.WriteLine($"[КРИТИЧНО] Знайдено порушення цілісності! Підроблений блок під номером: {invalidIndex}");
            }
            else
            {
                Console.WriteLine("[ОК] Ланцюг абсолютно валідний. Помилок не знайдено.");
            }
            break;

        case "4":
            Console.WriteLine();
            displayService.PrintBlockChain(blockChain.Chain);
            break;

        case "5":
            if (blockChain.Chain.Count > 1)
            {
                Console.WriteLine("\n[Увага] Виконується втручання в блокчейн...");
                blockChain.Chain[1].Data = "ПІДРОБЛЕНІ ДАНІ: Переказ 1000000 монет хакеру!";
                Console.WriteLine("Дані в блоці №1 було навмисно змінено. Тепер використайте пункт 2 або 3 для перевірки.");
            }
            else
            {
                Console.WriteLine("\nСпочатку додайте хоча б один блок (Пункт 1), щоб було що підробляти.");
            }
            break;

        case "0":
            Console.WriteLine("\nЗавершення роботи системи...");
            break;

        default:
            Console.WriteLine("\nНевірний вибір. Будь ласка, введіть число від 0 до 5.");
            break;
    }
} while (choice != "0");
