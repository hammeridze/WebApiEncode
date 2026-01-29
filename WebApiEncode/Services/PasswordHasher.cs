using System.Security.Cryptography;
using System.Text;

namespace WebApiEncode.Services
{
    public static class PasswordHasher
    {
        // Метод для хеширования пароля
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Добавляем соль для безопасности
                byte[] salt = Encoding.UTF8.GetBytes("vigenere_salt_2024");
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] saltedPassword = new byte[salt.Length + passwordBytes.Length];
                
                Buffer.BlockCopy(salt, 0, saltedPassword, 0, salt.Length);
                Buffer.BlockCopy(passwordBytes, 0, saltedPassword, salt.Length, passwordBytes.Length);
                
                byte[] hashBytes = sha256.ComputeHash(saltedPassword);
                return Convert.ToBase64String(hashBytes);
            }
        }

        // Проверка пароля
        public static bool VerifyPassword(string inputPassword, string storedHash)
        {
            string newHash = HashPassword(inputPassword);
            return newHash == storedHash;
        }
    }
}
