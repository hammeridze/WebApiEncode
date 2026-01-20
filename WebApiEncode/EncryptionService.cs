namespace WebApiEncode
{
    public static class EncryptionService  // Добавьте static
    {
        private static Dictionary<char, int> code;

        // Статический конструктор для инициализации
        static EncryptionService()
        {
            code = new Dictionary<char, int>()
            {
                ['a'] = 0,
                ['b'] = 1,
                ['c'] = 2,
                ['d'] = 3,
                ['e'] = 4,
                ['f'] = 5,
                ['g'] = 6,
                ['h'] = 7,
                ['i'] = 8,
                ['j'] = 9,
                ['k'] = 10,
                ['l'] = 11,
                ['m'] = 12,
                ['n'] = 13,
                ['o'] = 14,
                ['p'] = 15,
                ['q'] = 16,
                ['r'] = 17,
                ['s'] = 18,
                ['t'] = 19,
                ['u'] = 20,
                ['v'] = 21,
                ['w'] = 22,
                ['x'] = 23,
                ['y'] = 24,
                ['z'] = 25
            };
        }

        // Методы тоже должны быть статическими
        public static string Encrypt(string text, string key)
        {
            int len = key.Length;
            string res = "";

            for (int i = len; i < text.Length; i++)
            {
                key += key[i % len];
            }

            for (int i = 0; i < text.Length; i++)
            {
                int x = (code[text[i]] + code[key[i]]) % 26;
                foreach (var j in code)
                {
                    if (x == j.Value)
                    {
                        res += j.Key;
                    }
                }
            }
            return res;
        }

        public static string Decrypt(string text, string key)
        {
            int len = key.Length;
            string res = "";

            for (int i = len; i < text.Length; i++)
            {
                key += key[i % len];
            }

            for (int i = 0; i < text.Length; i++)
            {
                int x = (code[text[i]] - code[key[i]] + 26) % 26;
                foreach (var j in code)
                {
                    if (x == j.Value)
                    {
                        res += j.Key;
                    }
                }
            }
            return res;
        }
    }
}


