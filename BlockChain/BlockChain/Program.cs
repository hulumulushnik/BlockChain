using BlockChainP411NEW.Models;
using BlockChainP411NEW.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var blockChain = new BlockChainService();
var transactionService = blockChain._transactionService;
var walletService = blockChain._walletService;

var aliceWallet = walletService.CreateWallet("Alice");
var bobWallet = walletService.CreateWallet("Bob");
var carloWallet = walletService.CreateWallet("Carlo");
var minerWallet = walletService.CreateWallet("Miner");

Console.WriteLine("==================================================");
Console.WriteLine(" ЕКОНОМІЧНІ ВРАЗЛИВОСТІ, ЛІМІТ ЕМІСІЇ ТА АУДИТ ");
Console.WriteLine("==================================================\n");

Console.WriteLine("--- ЧАСТИНА 1: Захист від Double Spend ---");
await blockChain.AddBlockAsync(new List<Transaction>(), aliceWallet.Address, CancellationToken.None);
Console.WriteLine($"Баланс Аліси після майнінгу: {blockChain.GetBalance(aliceWallet.Address)}");

var tx1 = transactionService.CreateTransaction(aliceWallet, bobWallet.Address, 50);
var tx2 = transactionService.CreateTransaction(aliceWallet, carloWallet.Address, 50);

Console.WriteLine("Аліса намагається відправити 50 Бобу і 50 Карло одночасно...");
await blockChain.AddBlockAsync(new List<Transaction> { tx1, tx2 }, minerWallet.Address, CancellationToken.None);

Console.WriteLine($"Поточний баланс Аліси: {blockChain.GetBalance(aliceWallet.Address)}");
Console.WriteLine($"Поточний баланс Боба: {blockChain.GetBalance(bobWallet.Address)}");
Console.WriteLine($"Поточний баланс Карло: {blockChain.GetBalance(carloWallet.Address)}\n");


Console.WriteLine("--- ЧАСТИНА 2: Жорсткий ліміт емісії (Hard Cap: 1000) ---");
Console.WriteLine("Майнимо 22 блоки підряд...");

for (int i = 3; i <= 22; i++)
{
    await blockChain.AddBlockAsync(new List<Transaction>(), minerWallet.Address, CancellationToken.None);

    if (i >= 19)
    {
        Console.WriteLine($"Блок #{i}. Видобуто монет: {blockChain.TotalMinted}. Баланс Майнера: {blockChain.GetBalance(minerWallet.Address)}");
    }
}
Console.WriteLine("");


Console.WriteLine("--- ЧАСТИНА 3: Proof of Reserves (Аудит) ---");
bool isEconomyValid = blockChain.ValidateEconomy();

if (isEconomyValid)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n[УСПІХ] Економіка ідеально збалансована! Жодної монети не взято з повітря.");
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("\n[КРИТИЧНО] Виявлено розбіжність балансів! Система скомпрометована.");
}
Console.ResetColor();

Console.ReadLine();