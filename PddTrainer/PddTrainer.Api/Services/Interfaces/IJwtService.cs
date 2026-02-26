using PddTrainer.Api.Models;

namespace PddTrainer.Api.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
