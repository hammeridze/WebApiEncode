using EncodeLibrary;

namespace WebApiEncode.Services
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}
