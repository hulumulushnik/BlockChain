namespace BlockChainP411NEW.Models
{
    public class Block
    {
        // Порядковий номер блоку в ланцюжку
        public int Index { get; set; }

        // Час створення блоку
        public DateTime TimeStamp { get; set; }

        // Дані, які зберігаються в блоці
        public string Data { get; set; }

        // Хеш попереднього блоку в ланцюжку
        public string PreviousHash { get; set; }

        // Число, яке використовується для процесу майнінгу (пошуку правильного хешу)
        public int Nonce { get; set; }

        // Хеш поточного блоку, який обчислюється на основі даних блоку та хешу попереднього блоку
        public string Hash { get; set; }

        // Конструктор для створення нового блоку
        public Block(int index, DateTime timeStamp, string data, string previousHash)
        {
            Index = index;
            TimeStamp = timeStamp;
            Data = data;
            PreviousHash = previousHash;
            Hash = string.Empty;
            Nonce = 0;
        }
    }
}