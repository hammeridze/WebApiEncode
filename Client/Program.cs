using EncodeLibrary;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
            
            JwtClient jwtClient = new JwtClient("http://localhost:5203");

            // Основной цикл программы
            while (true)
            {
                PrintHeader();
                
                // Авторизация/Регистрация
                bool isAuthorized = false;
                while (!isAuthorized)
                {
                    PrintSection("АВТОРИЗАЦИЯ");
                    Console.WriteLine("┌─────────────────────────────────────┐");
                    Console.WriteLine("│  1. Авторизоваться                  │");
                    Console.WriteLine("│  2. Зарегистрироваться              │");
                    Console.WriteLine("│  3. Выход из программы              │");
                    Console.WriteLine("└─────────────────────────────────────┘");
                    Console.Write("\nВыберите действие: ");
                    
                    string? input = Console.ReadLine();
                    if (!int.TryParse(input, out int choice))
                    {
                        PrintError("Неверный ввод. Попробуйте снова.");
                        continue;
                    }

                    if (choice == 2)
                    {
                        await jwtClient.Registration();
                    }
                    else if (choice == 1)
                    {
                        if (await jwtClient.Autorization())
                        {
                            isAuthorized = true;
                        }
                    }
                    else if (choice == 3)
                    {
                        PrintSuccess("До свидания!");
                        return;
                    }
                    else
                    {
                        PrintError("Неверный выбор. Попробуйте снова.");
                    }
                }

                PrintSuccess("Авторизация успешна!");

                // Смена пароля
                Console.Write("\nЖелаете сменить пароль? (Y/N): ");
                string? changePass = Console.ReadLine();
                if (changePass?.ToUpper() == "Y")
                {
                    Console.Write("Введите новый пароль: ");
                    string? newPassword = Console.ReadLine();
                    if (!string.IsNullOrEmpty(newPassword))
                    {
                        await jwtClient.ChangePassword(newPassword);
                    }
                }

                // Основное меню
                while (true)
                {
                    PrintMainMenu();
                    
                    string? menuInput = Console.ReadLine();
                    if (!int.TryParse(menuInput, out int menuChoice))
                    {
                        PrintError("Неверный ввод. Попробуйте снова.");
                        Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                        Console.ReadKey();
                        continue;
                    }

                    Console.WriteLine();

                    switch (menuChoice)
                    {
                        case 1:
                            await HandleEncrypt(jwtClient);
                            break;
                        case 2:
                            await HandleDecrypt(jwtClient);
                            break;
                        case 3:
                            await HandleAddText(jwtClient);
                            break;
                        case 4:
                            await HandleUpdateText(jwtClient);
                            break;
                        case 5:
                            await HandleDeleteText(jwtClient);
                            break;
                        case 6:
                            await jwtClient.GetAllTexts();
                            break;
                        case 7:
                            await jwtClient.GetHistory();
                            break;
                        case 8:
                            await HandleGetTextById(jwtClient);
                            break;
                        case 9:
                            await HandleEncryptTextById(jwtClient);
                            break;
                        case 10:
                            await HandleDecryptTextById(jwtClient);
                            break;
                        case 11:
                            await HandleDeleteHistory(jwtClient);
                            break;
                        case 0:
                            // Выход из аккаунта - возвращаемся к меню авторизации
                            PrintSuccess("Вы вышли из аккаунта.");
                            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                            Console.ReadKey();
                            Console.Clear();
                            // Выходим из цикла основного меню, возвращаемся к авторизации
                            break;
                        case 99:
                            // Выход из программы
                            PrintSuccess("До свидания!");
                            return;
                        default:
                            PrintError("Неверный выбор. Попробуйте снова.");
                            break;
                    }

                    // Если выбран выход из аккаунта (case 0), выходим из цикла основного меню
                    if (menuChoice == 0)
                    {
                        break;
                    }

                    Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                    Console.ReadKey();
                    Console.Clear();
                }
            }
        }

        static void PrintHeader()
        {
             Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                                                              ║");
            Console.WriteLine("║               СИСТЕМА ШИФРОВАНИЯ VIGENERE                    ║");
            Console.WriteLine("║                                                              ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        static void PrintSection(string title)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  {title}");
            Console.WriteLine($"{new string('═', 60)}");
            Console.ResetColor();
        }

        static void PrintMainMenu()
        {
            Console.Clear();
            PrintHeader();
            PrintSection("ГЛАВНОЕ МЕНЮ");
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("┌────────────────────────────────────────────────────────┐");
            Console.WriteLine("│  ШИФРОВАНИЕ И РАСШИФРОВАНИЕ                            │");
            Console.WriteLine("├────────────────────────────────────────────────────────┤");
            Console.WriteLine("│  1. Зашифровать текст                                  │");
            Console.WriteLine("│  2. Расшифровать текст                                 │");
            Console.WriteLine("│  9. Зашифровать текст из списка                        │");
            Console.WriteLine("│  10. Расшифровать текст из списка                      │");
            Console.WriteLine("├────────────────────────────────────────────────────────┤");
            Console.WriteLine("│  УПРАВЛЕНИЕ ТЕКСТАМИ                                   │");
            Console.WriteLine("├────────────────────────────────────────────────────────┤");
            Console.WriteLine("│  3. Добавить текст                                     │");
            Console.WriteLine("│  4. Изменить текст                                     │");
            Console.WriteLine("│  5. Удалить текст                                      │");
            Console.WriteLine("│  6. Получить все тексты                                │");
            Console.WriteLine("│  8. Найти текст по ID                                  │");
            Console.WriteLine("├────────────────────────────────────────────────────────┤");
            Console.WriteLine("│  ИНФОРМАЦИЯ                                            │");
            Console.WriteLine("├────────────────────────────────────────────────────────┤");
            Console.WriteLine("│  7. Просмотреть историю операций                       │");
            Console.WriteLine("│  11. Удалить историю операций                          │");
            Console.WriteLine("├────────────────────────────────────────────────────────┤");
            Console.WriteLine("│  0. Выйти из аккаунта                                  │");
            Console.WriteLine("│  99. Выход из программы                                │");
            Console.WriteLine("└────────────────────────────────────────────────────────┘");
            Console.ResetColor();
            Console.Write("\nВыберите действие: ");
        }

        static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ ОШИБКА: {message}");
            Console.ResetColor();
        }

        static void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ {message}");
            Console.ResetColor();
        }

        static void PrintInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\nℹ {message}");
            Console.ResetColor();
        }

        static async Task HandleEncrypt(JwtClient jwtClient)
        {
            PrintSection("ШИФРОВАНИЕ ТЕКСТА");
            Console.Write("Введите текст для шифрования: ");
            string? text = Console.ReadLine();
            Console.Write("Введите ключ шифрования: ");
            string? key = Console.ReadLine();

            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(key))
            {
                PrintError("Текст и ключ не могут быть пустыми.");
                return;
            }

            await jwtClient.EncodeText(text, key, "encrypt");
        }

        static async Task HandleDecrypt(JwtClient jwtClient)
        {
            PrintSection("РАСШИФРОВАНИЕ ТЕКСТА");
            Console.Write("Введите зашифрованный текст: ");
            string? text = Console.ReadLine();
            Console.Write("Введите ключ расшифрования: ");
            string? key = Console.ReadLine();

            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(key))
            {
                PrintError("Текст и ключ не могут быть пустыми.");
                return;
            }

            await jwtClient.EncodeText(text, key, "decrypt");
        }

        static async Task HandleAddText(JwtClient jwtClient)
        {
            PrintSection("ДОБАВЛЕНИЕ ТЕКСТА");
            Console.Write("Введите текст: ");
            string? text = Console.ReadLine();

            if (string.IsNullOrEmpty(text))
            {
                PrintError("Текст не может быть пустым.");
                return;
            }

            await jwtClient.AddText(text);
        }

        static async Task HandleUpdateText(JwtClient jwtClient)
        {
            PrintSection("ИЗМЕНЕНИЕ ТЕКСТА");
            Console.Write("Введите ID текста: ");
            string? idInput = Console.ReadLine();
            
            if (!int.TryParse(idInput, out int id) || id <= 0)
            {
                PrintError("Неверный ID.");
                return;
            }

            Console.Write("Введите новый текст: ");
            string? text = Console.ReadLine();

            if (string.IsNullOrEmpty(text))
            {
                PrintError("Текст не может быть пустым.");
                return;
            }

            await jwtClient.UpdateText(text, id);
        }

        static async Task HandleDeleteText(JwtClient jwtClient)
        {
            PrintSection("УДАЛЕНИЕ ТЕКСТА");
            Console.Write("Введите ID текста для удаления: ");
            string? idInput = Console.ReadLine();
            
            if (!int.TryParse(idInput, out int id) || id <= 0)
            {
                PrintError("Неверный ID.");
                return;
            }

            await jwtClient.DeleteText(id);
        }

        static async Task HandleGetTextById(JwtClient jwtClient)
        {
            PrintSection("ПОИСК ТЕКСТА ПО ID");
            Console.Write("Введите ID текста: ");
            string? idInput = Console.ReadLine();
            
            if (!int.TryParse(idInput, out int id) || id <= 0)
            {
                PrintError("Неверный ID.");
                return;
            }

            await jwtClient.GetTextById(id);
        }

        static async Task HandleEncryptTextById(JwtClient jwtClient)
        {
            PrintSection("ШИФРОВАНИЕ ТЕКСТА ИЗ СПИСКА");
            Console.Write("Введите ID текста из списка: ");
            string? idInput = Console.ReadLine();
            
            if (!int.TryParse(idInput, out int textId) || textId <= 0)
            {
                PrintError("Неверный ID.");
                return;
            }

            Console.Write("Введите ключ шифрования: ");
            string? key = Console.ReadLine();

            if (string.IsNullOrEmpty(key))
            {
                PrintError("Ключ не может быть пустым.");
                return;
            }

            await jwtClient.EncodeTextById(textId, key);
        }

        static async Task HandleDecryptTextById(JwtClient jwtClient)
        {
            PrintSection("РАСШИФРОВАНИЕ ТЕКСТА ИЗ СПИСКА");
            Console.Write("Введите ID текста из списка: ");
            string? idInput = Console.ReadLine();
            
            if (!int.TryParse(idInput, out int textId) || textId <= 0)
            {
                PrintError("Неверный ID.");
                return;
            }

            Console.Write("Введите ключ расшифрования: ");
            string? key = Console.ReadLine();

            if (string.IsNullOrEmpty(key))
            {
                PrintError("Ключ не может быть пустым.");
                return;
            }

            await jwtClient.DecryptTextById(textId, key);
        }

        static async Task HandleDeleteHistory(JwtClient jwtClient)
        {
            PrintSection("УДАЛЕНИЕ ИСТОРИИ ОПЕРАЦИЙ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠ ВНИМАНИЕ: Это действие удалит ВСЮ историю операций!");
            Console.ResetColor();
            Console.Write("\nВы уверены, что хотите удалить всю историю? (Y/N): ");
            string? confirm = Console.ReadLine();
            
            if (confirm?.ToUpper() != "Y")
            {
                PrintInfo("Удаление истории отменено.");
                return;
            }

            await jwtClient.DeleteHistory();
        }
    }
}
