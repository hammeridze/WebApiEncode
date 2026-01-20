using EncodeLibrary;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WebApiEncode.Services;

namespace WebApiEncode.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ITokenService _tokenService;
        private EncryptionContext _context;

        public AuthController(IConfiguration configuration,
                         ITokenService tokenService,
                         EncryptionContext context)
        {
            _configuration = configuration;
            _tokenService = tokenService;
            _context = context;
        }

        [HttpPost("registration")]
        public ActionResult<ResponseRegistration> Registration([FromBody] RequestRegistration requestRegistration)
        {
            ResponseRegistration responseRegistration = new ResponseRegistration();

            if (requestRegistration == null)
            {
                return BadRequest(new ResponseRegistration 
                { 
                    Success = false, 
                    Note = "Запрос не может быть пустым" 
                });
            }

            if (string.IsNullOrWhiteSpace(requestRegistration.Email))
            {
                return BadRequest(new ResponseRegistration 
                { 
                    Success = false, 
                    Note = "Email не может быть пустым" 
                });
            }

            if (string.IsNullOrWhiteSpace(requestRegistration.Password))
            {
                return BadRequest(new ResponseRegistration 
                { 
                    Success = false, 
                    Note = "Пароль не может быть пустым" 
                });
            }

            if (!requestRegistration.Email.Contains("@") || !requestRegistration.Email.Contains("."))
            {
                return BadRequest(new ResponseRegistration 
                { 
                    Success = false, 
                    Note = "Некорректный формат email" 
                });
            }

            if (requestRegistration.Password.Length < 3)
            {
                return BadRequest(new ResponseRegistration 
                { 
                    Success = false, 
                    Note = "Пароль должен содержать минимум 3 символа" 
                });
            }

            if (_context.Users.Any(t => t.Email == requestRegistration.Email))
            {
                responseRegistration.Success = false;
                responseRegistration.Note = "Уже есть пользователь с такой почтой";
            }
            else
            {
                User user = new User
                {
                    Email = requestRegistration.Email,
                    Password = requestRegistration.Password
                };
                _context.Users.Add(user);
                _context.SaveChanges();

                responseRegistration.Success = true;
                responseRegistration.Id = user.Id;
            }

            return Ok(responseRegistration);
        }


        [HttpPost("autorization")]
        public ActionResult<ResponseRegistration> Autorization([FromBody] RequestRegistration request)
        {
            if (request == null)
            {
                return BadRequest(new ResponseRegistration 
                { 
                    Success = false, 
                    Note = "Запрос не может быть пустым" 
                });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new ResponseRegistration 
                { 
                    Success = false, 
                    Note = "Email не может быть пустым" 
                });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new ResponseRegistration 
                { 
                    Success = false, 
                    Note = "Пароль не может быть пустым" 
                });
            }

            User? user = _context.Users.FirstOrDefault(t => t.Email == request.Email && t.Password == request.Password);
            ResponseRegistration response = new ResponseRegistration();

            if (user != null)
            {
                var token = _tokenService.GenerateToken(user);

                response.Success = true;
                response.Id = user.Id;
                response.JwtToken = token;
                response.User = user;
                response.Expiration = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:ExpiryMinutes"]));
            }
            else
            {
                response.Success = false;
                response.Note = "Не найден пользователь с такими данными";
            }

            return Ok(response);
        }

        [HttpPost("password")]
        public IActionResult Password([FromBody] ChangePasswordRequest request)
        {
            if (request == null)
            {
                return BadRequest("Запрос не может быть пустым");
            }

            if (string.IsNullOrWhiteSpace(request.Login))
            {
                return BadRequest("Логин не может быть пустым");
            }

            if (string.IsNullOrWhiteSpace(request.OldPassword))
            {
                return BadRequest("Старый пароль не может быть пустым");
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("Новый пароль не может быть пустым");
            }

            if (request.NewPassword.Length < 3)
            {
                return BadRequest("Новый пароль должен содержать минимум 3 символа");
            }

            User? user = _context.Users.FirstOrDefault(t => t.Email == request.Login && t.Password == request.OldPassword);
            if (user == null)
                return BadRequest("Неверный логин или пароль");

            user.Password = request.NewPassword;
            _context.SaveChanges();

            return Ok(new { message = "Пароль успешно изменен" });
        }
    }
}
