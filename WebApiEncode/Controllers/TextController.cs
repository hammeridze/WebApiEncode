using EncodeLibrary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApiEncode.Services;

namespace WebApiEncode.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TextController : ControllerBase
    {
        private readonly EncryptionContext _context;
        private readonly ILogger<TextController> _logger;
        private readonly FileService _fileService;

        public TextController(EncryptionContext context, ILogger<TextController> logger, FileService fileService)
        {
            _context = context;
            _logger = logger;
            _fileService = fileService;
        }

        /// <summary>
        /// Получает пользователя из JWT токена
        /// </summary>
        private User? GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return null;
            }

            return _context.Users.FirstOrDefault(u => u.Id == userId);
        }

        /// <summary>
        /// Находит минимальный свободный ID для нового текста (глобально для всех пользователей)
        /// </summary>
        private int FindNextAvailableId()
        {
            var existingIds = _context.Texts
                .Select(t => t.Id)
                .OrderBy(id => id)
                .ToList();

            // Если нет текстов, начинаем с 1
            if (!existingIds.Any())
            {
                return 1;
            }

            // Ищем первый пропуск в последовательности
            int expectedId = 1;
            foreach (var id in existingIds)
            {
                if (id > expectedId)
                {
                    // Найден пропуск
                    return expectedId;
                }
                expectedId = id + 1;
            }

            // Если пропусков нет, возвращаем следующий после максимального
            return expectedId;
        }

        [HttpPost("add")]
        public async Task<ActionResult<Text>> AddText([FromBody] Text request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Запрос не может быть пустым");
                }

                if (string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest("Содержимое текста не может быть пустым");
                }

                var user = GetCurrentUser();
                if (user == null)
                {
                    return Unauthorized("Пользователь не найден");
                }

                // Находим минимальный свободный ID
                int nextId = FindNextAvailableId();
                
                // Если указан ID и текст с таким ID не существует, используем этот ID
                if (request.Id > 0)
                {
                    var existingText = _context.Texts.Find(request.Id);
                    if (existingText == null)
                    {
                        // Используем указанный ID, если он свободен
                        nextId = request.Id;
                    }
                    // Если ID занят, используем найденный минимальный свободный ID
                }

                // Создаем объект текста с указанным ID
                Text text = new Text
                {
                    Id = nextId,
                    Content = request.Content,
                    User = user
                };

                // Добавляем текст через EF Core
                _context.Texts.Add(text);
                
                // Добавляем запись в историю
                string previewText = request.Content!.Length > 50 
                    ? request.Content.Substring(0, 50) + "..." 
                    : request.Content;
                
                History historyEntry = new History
                {
                    Operation = "add",
                    Date = DateTime.Now,
                    User = user,
                    Details = $"Текст '{previewText}' добавлен с ID {nextId}"
                };
                _context.Histories.Add(historyEntry);
                
                try
                {
                    _context.SaveChanges();
                    
                    // Логирование в файл
                    await _fileService.LogOperationToFile("add", $"Текст '{previewText}' добавлен с ID {nextId}", user.Email);

                    _logger.LogInformation($"Пользователь {user.Email} добавил текст с ID {text.Id}");

                    return Ok(text);
                }
                catch (DbUpdateException ex)
                {
                    // Если произошла ошибка из-за конфликта ID, находим следующий свободный
                    _context.ChangeTracker.Clear();
                    _logger.LogWarning(ex, $"Не удалось вставить текст с ID {nextId}, пробуем найти другой свободный ID");
                    
                    nextId = FindNextAvailableId();
                    
                    // Создаем новый объект текста с новым ID
                    Text text2 = new Text
                    {
                        Id = nextId,
                        Content = request.Content,
                        User = user
                    };
                    
                    try
                    {
                        _context.Texts.Add(text2);
                        
                        History historyEntry2 = new History
                        {
                            Operation = "add",
                            Date = DateTime.Now,
                            User = user,
                            Details = $"Текст '{previewText}' добавлен с ID {nextId}"
                        };
                        _context.Histories.Add(historyEntry2);
                        
                        _context.SaveChanges();

                        await _fileService.LogOperationToFile("add", $"Текст '{previewText}' добавлен с ID {nextId}", user.Email);
                        _logger.LogInformation($"Пользователь {user.Email} добавил текст с ID {text2.Id}");

                        return Ok(text2);
                    }
                    catch (Exception ex2)
                    {
                        _logger.LogError(ex2, "Критическая ошибка при добавлении текста");
                        return StatusCode(500, "Не удалось добавить текст");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении текста");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        [HttpPut("update/{id}")]
        public async Task<ActionResult> UpdateText(int id, [FromBody] Text request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Запрос не может быть пустым");
                }

                if (string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest("Содержимое текста не может быть пустым");
                }

                if (id <= 0)
                {
                    return BadRequest("Некорректный ID");
                }

                Text? text = _context.Texts
                    .Include(t => t.User)
                    .FirstOrDefault(t => t.Id == id);
                if (text == null)
                {
                    return NotFound("Не найден текст с таким id");
                }

                var user = GetCurrentUser();
                if (user == null)
                {
                    return Unauthorized("Пользователь не найден");
                }

                // Проверка прав доступа - пользователь может изменять только свои тексты
                if (text.User.Id != user.Id)
                {
                    return Forbid("Вы можете изменять только свои тексты");
                }

                text.Content = request.Content ?? string.Empty;

                History history = new History
                {
                    Operation = "update",
                    Date = DateTime.Now,
                    User = user,
                    Details = $"Текст ID {id} изменен"
                };
                _context.Histories.Add(history);

                _context.SaveChanges();

                // Логирование в файл
                await _fileService.LogOperationToFile("update", $"Текст ID {id} изменен", user.Email);

                _logger.LogInformation($"Пользователь {user.Email} изменил текст с ID {id}");

                return Ok(new { message = "Текст успешно обновлен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при обновлении текста с ID {id}");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<ActionResult<Text>> DeleteText(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Некорректный ID");
                }

                Text? text = _context.Texts
                    .Include(t => t.User)
                    .FirstOrDefault(t => t.Id == id);
                if (text == null)
                {
                    return NotFound("Не найден текст с таким id");
                }

                var user = GetCurrentUser();
                if (user == null)
                {
                    return Unauthorized("Пользователь не найден");
                }

                // Проверка прав доступа - пользователь может удалять только свои тексты
                if (text.User.Id != user.Id)
                {
                    return Forbid("Вы можете удалять только свои тексты");
                }

                // Сохраняем ID для повторного использования
                int deletedId = text.Id;
                string deletedContent = text.Content ?? string.Empty;

                _context.Texts.Remove(text);

                History history = new History
                {
                    Operation = "delete",
                    Date = DateTime.Now,
                    User = user,
                    Details = $"Текст ID {deletedId} удален"
                };
                _context.Histories.Add(history);

                _context.SaveChanges();
                
                // Обновляем sqlite_sequence, чтобы разрешить повторное использование ID
                // Это необходимо для SQLite, чтобы можно было использовать удаленный ID снова
                try
                {
                    var maxId = _context.Texts.Any() 
                        ? _context.Texts.Max(t => (int?)t.Id) ?? 0 
                        : 0;
                    
                    // Если удаленный ID был максимальным, обновляем sqlite_sequence
                    if (deletedId >= maxId)
                    {
                        // Обновляем sqlite_sequence на максимальный существующий ID
                        _context.Database.ExecuteSqlRaw(
                            "UPDATE sqlite_sequence SET seq = {0} WHERE name = 'Texts'",
                            maxId);
                    }
                }
                catch (Exception ex)
                {
                    // Логируем, но не прерываем выполнение
                    _logger.LogWarning(ex, $"Не удалось обновить sqlite_sequence после удаления текста с ID {deletedId}");
                }

                // Логирование в файл
                await _fileService.LogOperationToFile("delete", $"Текст ID {deletedId} удален", user.Email);

                _logger.LogInformation($"Пользователь {user.Email} удалил текст с ID {deletedId}");

                // Возвращаем информацию об удаленном тексте, включая ID для повторного использования
                return Ok(new { 
                    message = "Текст успешно удален", 
                    deletedId = deletedId,
                    deletedContent = deletedContent
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при удалении текста с ID {id}");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        [HttpGet("texts")]
        public ActionResult<List<Text>> GetAllTexts()
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null)
                {
                    return Unauthorized("Пользователь не найден");
                }

                var texts = _context.Texts
                    .Include(t => t.User)
                    .Where(t => t.User.Id == user.Id)
                    .ToList();
                return Ok(texts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка текстов");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        [HttpGet("history")]
        public ActionResult<List<History>> GetHistory()
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null)
                {
                    return Unauthorized("Пользователь не найден");
                }

                var history = _context.Histories
                    .Include(h => h.User)
                    .Where(h => h.User != null && h.User.Id == user.Id)
                    .OrderByDescending(h => h.Date)
                    .Take(50)
                    .ToList();

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении истории операций");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        [HttpGet("text/{id}")]
        public ActionResult<Text> GetTextById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Некорректный ID");
                }

                var user = GetCurrentUser();
                if (user == null)
                {
                    return Unauthorized("Пользователь не найден");
                }

                var text = _context.Texts
                    .Include(t => t.User)
                    .FirstOrDefault(t => t.Id == id);

                if (text == null)
                {
                    return NotFound("Текст с таким ID не найден");
                }

                // Проверка прав доступа - пользователь может просматривать только свои тексты
                if (text.User.Id != user.Id)
                {
                    return Forbid("Вы можете просматривать только свои тексты");
                }

                return Ok(text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении текста с ID {id}");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        [HttpDelete("history")]
        public async Task<ActionResult> DeleteHistory()
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null)
                {
                    return Unauthorized("Пользователь не найден");
                }

                // Удаляем всю историю пользователя
                var histories = _context.Histories
                    .Include(h => h.User)
                    .Where(h => h.User != null && h.User.Id == user.Id)
                    .ToList();

                int count = histories.Count;
                
                if (count > 0)
                {
                    _context.Histories.RemoveRange(histories);
                    _context.SaveChanges();

                    // Логирование в файл
                    await _fileService.LogOperationToFile("delete", $"Удалена история операций ({count} записей)", user.Email);

                    _logger.LogInformation($"Пользователь {user.Email} удалил {count} записей из истории");

                    return Ok(new { 
                        message = "История операций успешно удалена", 
                        deletedCount = count 
                    });
                }
                else
                {
                    return Ok(new { 
                        message = "История операций пуста", 
                        deletedCount = 0 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении истории операций");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }
    }
}
