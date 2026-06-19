using BlockChainP411NEW.Models;
using BlockChainP411NEW.Services;
using System;
using System.Collections.Generic;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var blockChain = new BlockChainService();
var displayService = new BlockChainDisplayService();

// Створюємо гаманці
var aliceWallet = blockChain._walletService.CreateWallet("Alice");
var bobWallet = blockChain._walletService.CreateWallet("Bob");

Console.WriteLine("==================================================");
Console.WriteLine("  ДЕМОНСТРАЦІЯ: SMART CHUNKING ТА СУВОРА ВАЛІДАЦІЯ");
Console.WriteLine("==================================================\n");

// --- ТЕСТ 1: Відхилення неправильної адреси ---
Console.WriteLine("--- ТЕСТ 1: Сувора валідація адреси ---");
try
{
    Console.WriteLine("Спроба створити транзакцію на адресу 'Боб' (неправильний формат)...");
    var invalidTx = blockChain._transactionService.CreateTransaction(aliceWallet, "Боб", 10);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[ЗАБЛОКОВАНО ВАЛІДАТОРОМ]: {ex.Message}");
    Console.ResetColor();
}

// --- ТЕСТ 2: Автоматична сегментація ---
Console.WriteLine("\n--- ТЕСТ 2: Автоматична сегментація (Smart Chunking) ---");
Console.WriteLine($"Ліміт розміру блоку: {blockChain.MaxBlockSizeBytes} байтів.\n");

var massiveTxList = new List<Transaction>();

for (int i = 1; i <= 15; i++)
{
    // Створюємо 15 правильних транзакцій від Alice до Bob
    var tx = blockChain._transactionService.CreateTransaction(aliceWallet, bobWallet.Address, i);
    massiveTxList.Add(tx);
}

Console.WriteLine($"Згенеровано {massiveTxList.Count} валідних транзакцій. Відправляємо на пакування...");

// Викликаємо метод автоматичного розбиття
await blockChain.ProcessTransactionsAsync(massiveTxList);

Console.WriteLine("\n--- ФІНАЛЬНИЙ СТАН БЛОКЧЕЙНУ ---");
displayService.PrintBlockChain(blockChain.Chain);

Console.WriteLine("\nНатисніть Enter для виходу...");
Console.ReadLine();