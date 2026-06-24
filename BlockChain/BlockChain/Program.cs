using System.Text;
using System.Threading;
using BlockChainP411NEW.Models;
using BlockChainP411NEW.Services;

Console.OutputEncoding = Encoding.UTF8;

// Services
var blockChain = new BlockChainService();
var walletService = blockChain._walletService;
var transactionService = blockChain._transactionService;
var displayService = new BlockChainDisplayService();

// Wallets
var walletAlice = walletService.CreateWallet("Alice");
var walletBob = walletService.CreateWallet("Bob");
var walletCharlie = walletService.CreateWallet("Charlie");
var walletHacker = walletService.CreateWallet("Hacker");

// Даємо гроші учасникам — майнимо 2 блоки
Console.WriteLine("=== Майнимо стартові блоки ===");
await blockChain.AddBlockAsync(new List<Transaction>(), walletAlice.Address, CancellationToken.None);
await blockChain.AddBlockAsync(new List<Transaction>(), walletBob.Address, CancellationToken.None);
Console.WriteLine($"Баланс Аліси:  {blockChain.GetBalance(walletAlice.Address)}");
Console.WriteLine($"Баланс Боба:   {blockChain.GetBalance(walletBob.Address)}");
Console.WriteLine($"Баланс Хакера: {blockChain.GetBalance(walletHacker.Address)}\n");

// ЧАСТИНА 1: Спам — різні суми щоб уникнути RBF
Console.WriteLine("=== ЧАСТИНА 1: Спам-атака (10 транзакцій Fee=0) ===");
Console.WriteLine($"Ліміт мемпулу: {blockChain.MaxMempoolSize}");

for (int i = 1; i <= 10; i++)
{
    try
    {
        // Різні суми (i) щоб RBF не спрацьовував раніше Eviction
        var spamTx = transactionService.CreateTransaction(
            walletAlice, walletHacker.Address, i, fee: 0m);
        blockChain.AddTransactionToMempool(spamTx);
        Console.WriteLine($"[Спам #{i}] Додано до мемпулу. Розмір: {blockChain.PendingTransactions.Count}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Спам #{i}] ВІДХИЛЕНО: {ex.Message}");
    }
}
Console.WriteLine($"Розмір мемпулу після спаму: {blockChain.PendingTransactions.Count}/{blockChain.MaxMempoolSize}\n");

// Аліса витісняє найдешевшу
Console.WriteLine("=== Аліса відправляє транзакцію з Fee=10 ===");
try
{
    var aliceTx = transactionService.CreateTransaction(
        walletAlice, walletBob.Address, 5m, fee: 10m);
    blockChain.AddTransactionToMempool(aliceTx);
    Console.WriteLine($"[Аліса] Транзакція додана! Розмір мемпулу: {blockChain.PendingTransactions.Count}");
    Console.WriteLine("Мемпул після витіснення:");
    foreach (var t in blockChain.PendingTransactions)
        Console.WriteLine($"  From={t.From[..6]}... Amount={t.Amount} Fee={t.Fee}");
}
catch (Exception ex)
{
    Console.WriteLine($"[Аліса] ПОМИЛКА: {ex.Message}");
}

// ЧАСТИНА 2: RBF — Боб має достатньо коштів (50)
Console.WriteLine("\n=== ЧАСТИНА 2: RBF — Боб прискорює транзакцію ===");
try
{
    var bobTx1 = transactionService.CreateTransaction(
        walletBob, walletCharlie.Address, 10m, fee: 1m); // 10+1=11, Боб має 50
    blockChain.AddTransactionToMempool(bobTx1);
    Console.WriteLine($"[Боб] Транзакція з Fee=1 додана. Розмір мемпулу: {blockChain.PendingTransactions.Count}");
}
catch (Exception ex)
{
    Console.WriteLine($"[Боб Fee=1] ПОМИЛКА: {ex.Message}");
}

try
{
    var bobTx2 = transactionService.CreateTransaction(
        walletBob, walletCharlie.Address, 10m, fee: 15m); // RBF: та сама сума, більша комісія
    blockChain.AddTransactionToMempool(bobTx2);
    Console.WriteLine($"[Боб RBF] Fee=15 прийнято. Розмір мемпулу: {blockChain.PendingTransactions.Count}");
}
catch (Exception ex)
{
    Console.WriteLine($"[Боб RBF] ПОМИЛКА: {ex.Message}");
}

Console.WriteLine("Мемпул після RBF:");
foreach (var t in blockChain.PendingTransactions)
    Console.WriteLine($"  From={t.From[..6]}... To={t.To[..6]}... Amount={t.Amount} Fee={t.Fee}");

// ЧАСТИНА 3: Тіньовий баланс
Console.WriteLine("\n=== ЧАСТИНА 3: Тіньовий баланс (Pending Balance) ===");
Console.WriteLine($"Реальний баланс Аліси:  {blockChain.GetBalance(walletAlice.Address)}");
Console.WriteLine($"Тіньовий баланс Аліси:  {blockChain.GetPendingBalance(walletAlice.Address)}");

// Спочатку очищуємо мемпул від спаму Аліси для чистого тесту
// АБО просто беремо суму що точно влізе в тіньовий баланс
decimal alicePending = blockChain.GetPendingBalance(walletAlice.Address);
Console.WriteLine($"(Тіньовий баланс перед тестом: {alicePending})");

try
{
    // Відправляємо більшу частину тіньового балансу
    decimal sendAmount = Math.Floor(alicePending * 0.7m); // 70% від тіньового балансу
    var aliceTx2 = transactionService.CreateTransaction(
        walletAlice, walletBob.Address, sendAmount, fee: 2m);
    blockChain.AddTransactionToMempool(aliceTx2);
    Console.WriteLine($"[Аліса->Боб {sendAmount}] Додано. Тіньовий баланс: {blockChain.GetPendingBalance(walletAlice.Address)}");

    // Тепер намагаємось відправити більше ніж залишилось
    decimal remaining = blockChain.GetPendingBalance(walletAlice.Address);
    decimal overAmount = remaining + 10m; // Явно більше ніж є
    var aliceTx3 = transactionService.CreateTransaction(
        walletAlice, walletCharlie.Address, overAmount, fee: 2m);
    blockChain.AddTransactionToMempool(aliceTx3);
    Console.WriteLine($"[Аліса->Карло {overAmount}] Додано.");
}
catch (Exception ex)
{
    Console.WriteLine($"[Аліса->Карло] ВІДХИЛЕНО (тіньовий баланс): {ex.Message}");
}

// ФІНАЛ
Console.WriteLine("\n=== ФІНАЛ: Майнінг блоку (ліміт 2 транзакції) ===");
Console.WriteLine($"Мемпул до майнінгу: {blockChain.PendingTransactions.Count} транзакцій");

var top2 = blockChain.PendingTransactions
    .OrderByDescending(t => t.Fee)
    .Take(2)
    .ToList();

Console.WriteLine("Майнер забирає:");
foreach (var t in top2)
    Console.WriteLine($"  From={t.From[..6]}... Amount={t.Amount} Fee={t.Fee}");

await blockChain.AddBlockAsync(top2, walletAlice.Address, CancellationToken.None);
blockChain.PendingTransactions.RemoveAll(t => top2.Contains(t));

Console.WriteLine($"\nМемпул після майнінгу: {blockChain.PendingTransactions.Count} транзакцій (спам залишився)");
Console.WriteLine("Залишок у мемпулі:");
foreach (var t in blockChain.PendingTransactions)
    Console.WriteLine($"  From={t.From[..6]}... Amount={t.Amount} Fee={t.Fee}");

Console.WriteLine("\n=== Фінальні баланси ===");
Console.WriteLine($"Аліса:   {blockChain.GetBalance(walletAlice.Address)}");
Console.WriteLine($"Боб:     {blockChain.GetBalance(walletBob.Address)}");
Console.WriteLine($"Карло:   {blockChain.GetBalance(walletCharlie.Address)}");