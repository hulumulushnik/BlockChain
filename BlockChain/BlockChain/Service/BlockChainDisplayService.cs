using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP411NEW.Services
{
    public class BlockChainDisplayService
    {
        public void PrintBlockChain(List<Models.Block> chain)
        {
            foreach (var block in chain)
            {
                Console.WriteLine($"Index: {block.Index}");
                Console.WriteLine($"TimeStamp: {block.TimeStamp}");
                Console.WriteLine($"Data: {block.Data}");
                Console.WriteLine($"PreviousHash: {block.PreviousHash}");
                Console.WriteLine($"Hash: {block.Hash}");
                Console.WriteLine($"Nonce: {block.Nonce}");
                Console.WriteLine(new string('-', 50)); // Роздільник між блоками для кращої читабельності
            }
        }

        public void PrintValidationResult(bool isValid)
        {
            if (isValid)
            {
                Console.WriteLine("The blockchain is valid.");
            }
            else
            {
                Console.WriteLine("The blockchain is invalid.");
            }
        }
    }
}