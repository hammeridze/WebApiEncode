using EncodeLibrary;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Client
{
    public class JwtClient
    {
        private readonly HttpClient _httpClient;
        private string _token;
        private DateTime _tokenExpiry;
        private User user;
        private string? _cachedEmail;
        private string? _cachedPassword;
        
        public JwtClient(string baseAddress)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseAddress)
            };
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Очищает кэш авторизационных данных
        /// </summary>
        public void ClearCache()
        {
            _token = null;
            _tokenExpiry = DateTime.MinValue;
            user = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
            // Не очищаем _cachedEmail и _cachedPassword, чтобы пользователь мог использовать их снова
        }

        public async Task Registration()
        {
            Console.WriteLine("Введите почту:");
            RequestRegistration requestRegistration = new RequestRegistration();
            requestRegistration.Email = Console.ReadLine();
            Console.WriteLine("Введите пароль:");
            requestRegistration.Password = Console.ReadLine();

            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/Auth/registration/", requestRegistration);
            response.EnsureSuccessStatusCode();
            ResponseRegistration p = await response.Content.ReadAsAsync<ResponseRegistration>();
            if (p.Success)
            {
                Console.WriteLine("Вы успешно зарегистрированы!!!");
            }
            else
            {
                Console.WriteLine("Не удалось зарегистрироваться");
                Console.WriteLine(p.Note);
            }
        }

        public async Task<bool> Autorization()
        {
            RequestRegistration requestRegistration = new RequestRegistration();
            
            // Если есть кэшированный email, предлагаем использовать его
            if (!string.IsNullOrEmpty(_cachedEmail))
            {
                Console.Write($"Введите почту (Enter для использования '{_cachedEmail}'): ");
                string? emailInput = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(emailInput))
                {
                    requestRegistration.Email = _cachedEmail;
                    Console.WriteLine($"Используется почта: {_cachedEmail}");
                }
                else
                {
                    requestRegistration.Email = emailInput;
                    _cachedEmail = emailInput;
                }
            }
            else
            {
                Console.Write("Введите почту: ");
                requestRegistration.Email = Console.ReadLine();
                _cachedEmail = requestRegistration.Email;
            }
            
            // Если есть кэшированный пароль, предлагаем использовать его
            if (!string.IsNullOrEmpty(_cachedPassword))
            {
                Console.Write("Введите пароль (Enter для использования сохраненного): ");
                string? passwordInput = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(passwordInput))
                {
                    requestRegistration.Password = _cachedPassword;
                    Console.WriteLine("Используется сохраненный пароль");
                }
                else
                {
                    requestRegistration.Password = passwordInput;
                    _cachedPassword = passwordInput;
                }
            }
            else
            {
                Console.Write("Введите пароль: ");
                requestRegistration.Password = Console.ReadLine();
                _cachedPassword = requestRegistration.Password;
            }
           
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/Auth/autorization/", requestRegistration);
            response.EnsureSuccessStatusCode();
            ResponseRegistration p = await response.Content.ReadAsAsync<ResponseRegistration>();
            if (p.Success)
            {
                Console.WriteLine("Вы успешно авторизованы!!!");
                _token = p.JwtToken;
                _tokenExpiry = p.Expiration;
                user = p.User;
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _token);

                Console.WriteLine($"Token получен. Истекает: {_tokenExpiry}");
            }
            else
            {
                Console.WriteLine("Не удалось авторизоваться");
                Console.WriteLine(p.Note);
                // Очищаем кэш при неудачной авторизации
                _cachedPassword = null;
            }
            return p.Success;
        }

        public async Task ChangePassword(string newPassword)
        {
            ChangePasswordRequest changePasswordRequest = new ChangePasswordRequest()
            {
                Login = user.Email,
                OldPassword = _cachedPassword ?? user.Password,
                NewPassword = newPassword
            };

            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/Auth/password/", changePasswordRequest);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Пароль успешно изменен");
                // Обновляем кэшированный пароль
                _cachedPassword = newPassword;
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("Не удалось изменить пароль");
            }
        }


        public async Task EncodeText(string text, string key, string operation)
        {
            if (DateTime.Now >= _tokenExpiry)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n❌ Токен истек. Требуется переаутентификация.");
                Console.ResetColor();
                return;
            }

            string url = "/api/Encryption/" + operation;
            Request request = new Request();
            request.Text = text;
            request.Key = key;
            
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, request);
                if (response.IsSuccessStatusCode)
                {
                    Response p = await response.Content.ReadAsAsync<Response>();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    System.Console.WriteLine();
                    Console.WriteLine("┌─────────────────────────────────────────────────────────────────────────────┐");
                    Console.WriteLine($"│  РЕЗУЛЬТАТ {(operation == "encrypt" ? "ШИФРОВАНИЯ" : "РАСШИФРОВАНИЯ"),-40} │");
                    Console.WriteLine("└─────────────────────────────────────────────────────────────────────────────┘");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  {p.ResultText}");
                    Console.ResetColor();
                    
                    // Предложение сохранить результат в файл
                    Console.Write("\nСохранить результат в файл? (Y/N): ");
                    string? saveChoice = Console.ReadLine();
                    if (saveChoice?.ToUpper() == "Y")
                    {
                        await SaveResultToFile(operation, p.OriginalText, p.ResultText, key);
                    }
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n❌ Ошибка: {errorMessage}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Ошибка: {ex.Message}");
                Console.ResetColor();
            }
        }

        public async Task AddText(string text, int? id = null)
        {
            try
            {
                Text req = new Text()
                {
                    Content = text,
                    User = user
                };
                
                if (id.HasValue && id.Value > 0)
                {
                    req.Id = id.Value;
                }
                
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/Text/add/", req);
                
                if (response.IsSuccessStatusCode)
                {
                    Text p = await response.Content.ReadAsAsync<Text>();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n✓ Текст успешно добавлен, ID = {p.Id}");
                    Console.ResetColor();
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    Console.ForegroundColor = ConsoleColor.Red;
                    //Console.WriteLine($"\n❌ Ошибка при добавлении текста: {response.StatusCode}");
                    //Console.WriteLine($"Детали: {errorMessage}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении текста: {ex.Message}");
            }
        }

        public async Task UpdateText(string text, int id)
        {
            Text req = new Text()
            {
                Content = text,
                User = user,
                Id = id
            };

            HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"/api/Text/update/{id}", req);
            if (response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ Текст успешно изменен");
                Console.ResetColor();
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Ошибка: {errorMessage}");
                Console.ResetColor();
            }
        }

        public async Task DeleteText(int id)
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync($"/api/Text/delete/{id}");
            if (response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ Текст успешно удален");
                Console.ResetColor();
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Ошибка: {errorMessage}");
                Console.ResetColor();
            }
        }

        public async Task GetAllTexts()
        {
            HttpResponseMessage response = await _httpClient.GetAsync("/api/Text/texts");

            if (response.IsSuccessStatusCode)
            {
                var res = await response.Content.ReadAsAsync<List<Text>>();
                if (res != null && res.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    System.Console.WriteLine();
                    Console.WriteLine($"┌──────────────────────────────────────────────────────────┐");
                    Console.WriteLine($"│  Найдено текстов: {res.Count,-40}                        │");
                    Console.WriteLine($"└──────────────────────────────────────────────────────────┘");
                    Console.ResetColor();
                    
                    for (int i = 0; i < res.Count; i++)
                    {
                        var item = res[i];
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"\n  [{i + 1}] ID: {item.Id}");
                        Console.ResetColor();
                        Console.WriteLine($"      Текст: {item.Content}");
                        if (i < res.Count - 1)
                        {
                            Console.WriteLine(new string('-', 60));
                        }
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n⚠ У вас пока нет сохраненных текстов");
                    Console.ResetColor();
                }
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Ошибка: {errorMessage}");
                Console.ResetColor();
            }
        }

        public async Task GetHistory()
        {
            HttpResponseMessage response = await _httpClient.GetAsync("/api/Text/history");

            if (response.IsSuccessStatusCode)
            {
                var history = await response.Content.ReadAsAsync<List<History>>();
                if (history != null && history.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine();
                    Console.WriteLine($"┌──────────────────────────────────────────────────────────┐");
                    Console.WriteLine($"│  История операций (последние {history.Count,-30}         │");
                    Console.WriteLine($"└──────────────────────────────────────────────────────────┘");
                    Console.ResetColor();
                    
                    foreach (var item in history)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"[{item.Date:yyyy-MM-dd HH:mm:ss}] ");
                        Console.ResetColor();
                        
                        Console.ForegroundColor = GetOperationColor(item.Operation);
                        Console.Write($"{item.Operation.ToUpper(),-10} ");
                        Console.ResetColor();
                        
                        Console.WriteLine($": {item.Details}");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n⚠ История операций пуста");
                    Console.ResetColor();
                }
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Ошибка: {errorMessage}");
                Console.ResetColor();
            }
        }

        public async Task DeleteHistory()
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync("/api/Text/history");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                try
                {
                    var json = JObject.Parse(result);
                    var deletedCount = json["deletedCount"]?.Value<int>() ?? 0;
                    var message = json["message"]?.Value<string>() ?? "История операций успешно удалена";
                    
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n✓ {message}");
                    if (deletedCount > 0)
                    {
                        Console.WriteLine($"  Удалено записей: {deletedCount}");
                    }
                    Console.ResetColor();
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n✓ История операций успешно удалена");
                    Console.ResetColor();
                }
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Ошибка: {errorMessage}");
                Console.ResetColor();
            }
        }

        private ConsoleColor GetOperationColor(string operation)
        {
            return operation.ToLower() switch
            {
                "encrypt" => ConsoleColor.Green,
                "decrypt" => ConsoleColor.Blue,
                "add" => ConsoleColor.Cyan,
                "update" => ConsoleColor.Yellow,
                "delete" => ConsoleColor.Red,
                _ => ConsoleColor.White
            };
        }

        private async Task SaveResultToFile(string operation, string originalText, string resultText, string? key = null)
        {
            try
            {
                string resultsDir = Path.Combine(Directory.GetCurrentDirectory(), "Results");
                if (!Directory.Exists(resultsDir))
                {
                    Directory.CreateDirectory(resultsDir);
                }

                string fileName = $"result_{operation}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(resultsDir, fileName);

                var content = new StringBuilder();
                content.AppendLine($"Операция: {operation}");
                content.AppendLine($"Дата и время: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                if (!string.IsNullOrEmpty(key))
                {
                    content.AppendLine($"Ключ: {key}");
                }
                content.AppendLine($"Исходный текст: {originalText}");
                content.AppendLine($"Результат: {resultText}");
                content.AppendLine(new string('-', 50));

                await File.WriteAllTextAsync(filePath, content.ToString(), Encoding.UTF8);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n✓ Результат сохранен в файл: {filePath}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Ошибка при сохранении в файл: {ex.Message}");
                Console.ResetColor();
            }
        }

        public async Task GetTextById(int id)
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"/api/Text/text/{id}");

            if (response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsAsync<Text>();
                Console.ForegroundColor = ConsoleColor.Cyan;
                System.Console.WriteLine();
                Console.WriteLine("┌────────────────────────────────────────────────────────┐");
                Console.WriteLine("│  НАЙДЕННЫЙ ТЕКСТ                                       │");
                Console.WriteLine("└────────────────────────────────────────────────────────┘");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ID: {text.Id}");
                Console.ResetColor();
                Console.WriteLine($"  Текст: {text.Content}");
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Ошибка: {errorMessage}");
                Console.ResetColor();
            }
        }

        public async Task EncodeTextById(int textId, string key)
        {
            KeyRequest request = new KeyRequest
            {
                Key = key
            };
            
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"/api/Encryption/encrypt-text/{textId}", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsAsync<Response>();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("┌────────────────────────────────────────────────────────┐");
                Console.WriteLine("│             РЕЗУЛЬТАТ ШИФРОВАНИЯ                       │");
                Console.WriteLine("└────────────────────────────────────────────────────────┘");
                Console.ResetColor();
                Console.WriteLine($"  Исходный текст: {result.OriginalText}");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  Зашифрованный текст: {result.ResultText}");
                Console.ResetColor();
                
                // Предложение сохранить результат в файл
                Console.Write("\nСохранить результат в файл? (Y/N): ");
                string? saveChoice = Console.ReadLine();
                if (saveChoice?.ToUpper() == "Y")
                {
                    await SaveResultToFile("encrypt", result.OriginalText, result.ResultText, key);
                }
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Ошибка: {errorMessage}");
                Console.ResetColor();
            }
        }

        public async Task DecryptTextById(int textId, string key)
        {
            KeyRequest request = new KeyRequest
            {
                Key = key
            };
            
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"/api/Encryption/decrypt-text/{textId}", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsAsync<Response>();
                Console.ForegroundColor = ConsoleColor.Cyan;
                System.Console.WriteLine();
                Console.WriteLine("┌────────────────────────────────────────────────────────┐");
                Console.WriteLine("│            РЕЗУЛЬТАТ РАСШИФРОВАНИЯ                     │");
                Console.WriteLine("└────────────────────────────────────────────────────────┘");
                Console.ResetColor();
                Console.WriteLine($"  Зашифрованный текст: {result.OriginalText}");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  Расшифрованный текст: {result.ResultText}");
                Console.ResetColor();
                
                // Предложение сохранить результат в файл
                Console.Write("\nСохранить результат в файл? (Y/N): ");
                string? saveChoice = Console.ReadLine();
                if (saveChoice?.ToUpper() == "Y")
                {
                    await SaveResultToFile("decrypt", result.OriginalText, result.ResultText, key);
                }
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Ошибка: {errorMessage}");
                Console.ResetColor();
            }
        }
    }
}
