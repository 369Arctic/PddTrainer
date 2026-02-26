using PddTrainer.Api.Models;
using PddTrainer.Api.Models.DTO;

namespace PddTrainer.Api.Services.Interfaces
{
    public interface IAttemptService
    {
        Task<Attempt> ProcessAttemptAsync(int userId, SubmitAttemptDto dto);
    }
}
