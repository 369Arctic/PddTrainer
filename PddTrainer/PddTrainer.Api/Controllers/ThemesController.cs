using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PddTrainer.Api.Data;
using PddTrainer.Api.Models;
using PddTrainer.Api.Models.DTO;
using Serilog;
using ILogger = Serilog.ILogger;

namespace PddTrainer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ThemesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;

        public ThemesController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _logger = Log.ForContext<ThemesController>();
            _mapper = mapper;
        }

        /// <summary>
        /// Создать новую тему билета.
        /// </summary>
        /// <param name="dto">Тема.</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> CreateTheme([FromBody] ThemeDto dto)
        {
            if (dto == null)
            {
                _logger.Warning("Попытка создать пустую тему");
                return BadRequest(new { message = "Невозможно создать пустую тему" });
            }

            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                _logger.Warning("Попытка создать тему с пустым наименованием");
                return BadRequest(new { message = "Невозможно создать тему с пустым наименованием" });
            }

            if (await _context.Themes.AnyAsync(u => string.Equals(u.Title, dto.Title)))
            {
                _logger.Warning("Попытка создать уже существующую тему");
                return Conflict(new { message = "Тема уже существует" });
            }
            try
            {
                var theme = _mapper.Map<Theme>(dto);
                _context.Themes.Add(theme);
                await _context.SaveChangesAsync();

                var resultDto = _mapper.Map<ThemeDto>(theme);
                _logger.Information("Создана новая тема Id = {Id}, Title = {Title}", theme.Id, theme.Title);

                return Ok(resultDto);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при создании темы билета");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Получить все темы билетов.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllThemes()
        {
            _logger.Information("Получен запрос на получение всех тем билетов");

            var themes = await _context.Themes.ToListAsync();

            var result = _mapper.Map<List<ThemeDto>>(themes);
            return Ok(result);
        }
    }
}
