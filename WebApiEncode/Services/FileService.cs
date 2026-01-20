using System.Text;

namespace WebApiEncode.Services
{
    public class FileService
    {
        private readonly string _resultsDirectory;
        private readonly string _logsDirectory;

        public FileService()
        {
            _resultsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Results");
            _logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");

            // Создаем директории, если их нет
            if (!Directory.Exists(_resultsDirectory))
            {
                Directory.CreateDirectory(_resultsDirectory);
            }

            if (!Directory.Exists(_logsDirectory))
            {
                Directory.CreateDirectory(_logsDirectory);
            }
        }

        public async Task SaveResultToFile(string operation, string originalText, string resultText, string? key = null)
        {
            try
            {
                string fileName = $"result_{operation}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(_resultsDirectory, fileName);

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
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не прерываем выполнение
                Console.WriteLine($"Ошибка при сохранении результата в файл: {ex.Message}");
            }
        }

        public async Task LogOperationToFile(string operation, string details, string? userEmail = null)
        {
            try
            {
                string fileName = $"operations_{DateTime.Now:yyyyMMdd}.log";
                string filePath = Path.Combine(_logsDirectory, fileName);

                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                              $"{(userEmail != null ? $"[{userEmail}] " : "")}" +
                              $"{operation.ToUpper()}: {details}";

                await File.AppendAllTextAsync(filePath, logEntry + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при записи в лог-файл: {ex.Message}");
            }
        }

        public async Task<List<string>> ReadLogFile(DateTime date)
        {
            try
            {
                string fileName = $"operations_{date:yyyyMMdd}.log";
                string filePath = Path.Combine(_logsDirectory, fileName);

                if (File.Exists(filePath))
                {
                    var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
                    return lines.ToList();
                }

                return new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении лог-файла: {ex.Message}");
                return new List<string>();
            }
        }

        public List<string> GetAvailableLogFiles()
        {
            try
            {
                var files = Directory.GetFiles(_logsDirectory, "operations_*.log")
                    .Select(Path.GetFileName)
                    .Where(f => f != null)
                    .Cast<string>()
                    .OrderByDescending(f => f)
                    .ToList();

                return files;
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
