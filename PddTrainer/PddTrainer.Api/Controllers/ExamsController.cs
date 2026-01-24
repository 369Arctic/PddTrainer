using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PddTrainer.Api.Data;
using PddTrainer.Api.Models;
using PddTrainer.Api.Models.DTO;
using Serilog;
using ILogger = Serilog.ILogger;

namespace PddTrainer.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExamsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly ExamSettings _examSettings;

        public ExamsController(ApplicationDbContext context, IMapper mapper, IOptions<ExamSettings> examSettings)
        {
            _context = context;
            _logger = Log.ForContext<ExamsController>();
            _mapper = mapper;
            _examSettings = examSettings.Value;
        }

        [HttpGet("{examId}")]
        public async Task<IActionResult> GetExam(string examId)
        {
            var mode = _examSettings.Exams.FirstOrDefault(u => u.Id == examId);
            if (mode == null)
            {
                _logger.Error("Не удалось найти вид экзамена с Id {examId}", examId);
                return NotFound("Экзамен не найден");
            }

            var questions = await _context.Questions
                .Include(u => u.AnswerOptions)
                .ToListAsync();

            var random = new Random();
            var selectedQuestions = questions
                .OrderBy(u => random.Next())
                .Take(mode.TotalQuestions)
                .ToList();

            var examDto = _mapper.Map<ExamDto>(mode);
            examDto.Questions = _mapper.Map<List<QuestionDto>>(selectedQuestions);

            _logger.Information("Вопросы для экзамена с Id {examId} успешно сформированы.", examId);
            return Ok(examDto);
        }
    }
}
