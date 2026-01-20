using EncodeLibrary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApiEncode.Services;

namespace WebApiEncode.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EncryptionController : ControllerBase
{
    private readonly EncryptionContext _context;
    private readonly ILogger<EncryptionController> _logger;
    private readonly FileService _fileService;

    public EncryptionController(EncryptionContext context, ILogger<EncryptionController> logger, FileService fileService)
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

    [HttpPost("encrypt")]
    public async Task<ActionResult<Response>> EncryptText([FromBody] Request request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest("Запрос не может быть пустым");
            }

            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest("Текст не может быть пустым");
            }

            if (string.IsNullOrWhiteSpace(request.Key))
            {
                return BadRequest("Ключ не может быть пустым");
            }

            var user = GetCurrentUser();
            if (user == null)
            {
                return Unauthorized("Пользователь не найден");
            }

            string resultText = EncryptionService.Encrypt(request.Text, request.Key);
            
            Response response = new Response
            {
                OriginalText = request.Text,
                ResultText = resultText
            };

            // Логирование операции
            History history = new History
            {
                Operation = "encrypt",
                Date = DateTime.Now,
                User = user,
                Details = $"Шифрование текста. Длина исходного текста: {request.Text.Length} символов"
            };
            _context.Histories.Add(history);
            _context.SaveChanges();

            // Сохранение результата в файл и логирование
            await _fileService.SaveResultToFile("encrypt", request.Text, resultText, request.Key);
            await _fileService.LogOperationToFile("encrypt", $"Шифрование текста длиной {request.Text.Length} символов", user.Email);

            _logger.LogInformation($"Пользователь {user.Email} выполнил шифрование текста длиной {request.Text.Length} символов");

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning($"Ошибка валидации при шифровании: {ex.Message}");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при выполнении шифрования");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpPost("decrypt")]
    public async Task<ActionResult<Response>> DecryptText([FromBody] Request request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest("Запрос не может быть пустым");
            }

            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest("Текст не может быть пустым");
            }

            if (string.IsNullOrWhiteSpace(request.Key))
            {
                return BadRequest("Ключ не может быть пустым");
            }

            var user = GetCurrentUser();
            if (user == null)
            {
                return Unauthorized("Пользователь не найден");
            }

            string resultText = EncryptionService.Decrypt(request.Text, request.Key);
            
            Response response = new Response
            {
                OriginalText = request.Text,
                ResultText = resultText
            };

            // Логирование операции
            History history = new History
            {
                Operation = "decrypt",
                Date = DateTime.Now,
                User = user,
                Details = $"Расшифрование текста. Длина зашифрованного текста: {request.Text.Length} символов"
            };
            _context.Histories.Add(history);
            _context.SaveChanges();

            // Сохранение результата в файл и логирование
            await _fileService.SaveResultToFile("decrypt", request.Text, resultText, request.Key);
            await _fileService.LogOperationToFile("decrypt", $"Расшифрование текста длиной {request.Text.Length} символов", user.Email);

            _logger.LogInformation($"Пользователь {user.Email} выполнил расшифрование текста длиной {request.Text.Length} символов");

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning($"Ошибка валидации при расшифровании: {ex.Message}");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при выполнении расшифрования");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpPost("encrypt-text/{textId}")]
    public async Task<ActionResult<Response>> EncryptTextById(int textId, [FromBody] KeyRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Key))
            {
                return BadRequest("Ключ не может быть пустым");
            }

            var user = GetCurrentUser();
            if (user == null)
            {
                return Unauthorized("Пользователь не найден");
            }

            var text = _context.Texts
                .Include(t => t.User)
                .FirstOrDefault(t => t.Id == textId);

            if (text == null)
            {
                return NotFound("Текст с таким ID не найден");
            }

            if (text.User.Id != user.Id)
            {
                return Forbid("Вы можете шифровать только свои тексты");
            }

            string resultText = EncryptionService.Encrypt(text.Content, request.Key);
            
            Response response = new Response
            {
                OriginalText = text.Content,
                ResultText = resultText
            };

            History history = new History
            {
                Operation = "encrypt",
                Date = DateTime.Now,
                User = user,
                Details = $"Шифрование текста ID {textId}. Длина: {text.Content.Length} символов"
            };
            _context.Histories.Add(history);
            _context.SaveChanges();

            // Сохранение результата в файл и логирование
            await _fileService.SaveResultToFile("encrypt", text.Content, resultText, request.Key);
            await _fileService.LogOperationToFile("encrypt", $"Шифрование текста ID {textId} длиной {text.Content.Length} символов", user.Email);

            _logger.LogInformation($"Пользователь {user.Email} зашифровал текст с ID {textId}");

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning($"Ошибка валидации при шифровании: {ex.Message}");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при шифровании текста с ID {textId}");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpPost("decrypt-text/{textId}")]
    public async Task<ActionResult<Response>> DecryptTextById(int textId, [FromBody] KeyRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Key))
            {
                return BadRequest("Ключ не может быть пустым");
            }

            var user = GetCurrentUser();
            if (user == null)
            {
                return Unauthorized("Пользователь не найден");
            }

            var text = _context.Texts
                .Include(t => t.User)
                .FirstOrDefault(t => t.Id == textId);

            if (text == null)
            {
                return NotFound("Текст с таким ID не найден");
            }

            if (text.User.Id != user.Id)
            {
                return Forbid("Вы можете расшифровывать только свои тексты");
            }

            string resultText = EncryptionService.Decrypt(text.Content, request.Key);
            
            Response response = new Response
            {
                OriginalText = text.Content,
                ResultText = resultText
            };

            History history = new History
            {
                Operation = "decrypt",
                Date = DateTime.Now,
                User = user,
                Details = $"Расшифрование текста ID {textId}. Длина: {text.Content.Length} символов"
            };
            _context.Histories.Add(history);
            _context.SaveChanges();

            // Сохранение результата в файл и логирование
            await _fileService.SaveResultToFile("decrypt", text.Content, resultText, request.Key);
            await _fileService.LogOperationToFile("decrypt", $"Расшифрование текста ID {textId} длиной {text.Content.Length} символов", user.Email);

            _logger.LogInformation($"Пользователь {user.Email} расшифровал текст с ID {textId}");

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning($"Ошибка валидации при расшифровании: {ex.Message}");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при расшифровании текста с ID {textId}");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }
}