using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PddTrainer.Api.Models;
using PddTrainer.Api.Models.DTO;
using PddTrainer.Api.Services.Interfaces;
using System.Security.Claims;

namespace PddTrainer.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AttemptsController : ControllerBase
    {
        private readonly IAttemptService _attemptService;

        public AttemptsController(IAttemptService attemptService) 
        {
            _attemptService = attemptService;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] SubmitAttemptDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var attempt = await _attemptService.ProcessAttemptAsync(userId, dto);

            return Ok(new
            {
                attempt.Id,
                attempt.CorrectAnswers,
                attempt.Mistakes,
                attempt.Passed
            });
        }
    }
}
