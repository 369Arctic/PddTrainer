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
    public class TicketsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;

        public TicketsController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _logger = Log.ForContext<TicketsController>();
            _mapper = mapper;
        }

        /// <summary>
        /// Получить список всех билетов
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketDto>>> GetAllTickets()
        {
            _logger.Information("Получен запрос на получение всех билетов");

            var tickets = await _context.Tickets
                .AsNoTracking()
                .Include(q => q.Questions)
                .ThenInclude(a => a.AnswerOptions)
                .ToListAsync();

            var result = _mapper.Map<List<TicketDto>>(tickets);

            return Ok(result);
        }

        /// <summary>
        /// Получить билет по конкретному ID
        /// </summary>
        /// <param name="id">ID билета</param>
        [HttpGet("{id}")]
        public async Task<ActionResult<TicketDto>> GetTicketById(int id)
        {
            _logger.Information("Получен запрос на получение билета с Id = {Id}", id);

            var ticket = await _context.Tickets
                .Include(q => q.Questions)
                .ThenInclude(a => a.AnswerOptions)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (ticket == null)
            {
                _logger.Warning("Билет с Id = {Id} не найден", id);
                return NotFound(new { message = $"Билет с Id = {id} не найден" });
            }

            return Ok(_mapper.Map<TicketDto>(ticket));
        }

        /// <summary>
        /// Получить виртуальный (не хранящийся в БД) билет по ID темы.
        /// </summary>
        /// <param name="themeId">ID темы.</param>
        /// Объект <see cref="TicketDto"/>, содержащий название темы и список всех
        /// вопросов, привязанных к данной теме. В случае отсутствия темы возвращает ошибку.
        [HttpGet("theme/{themeId}")]
        public async Task<ActionResult<TicketDto>> GetTicketByThemeId(int themeId)
        {
            _logger.Information("Получен запрос на получение билета по теме с Id = {Id}", themeId);

            var theme = await _context.Themes.FirstOrDefaultAsync(u => u.Id == themeId);
            if (theme == null)
            {
                _logger.Warning("Не удалось найти тему с Id = {Id}", themeId);
                return NotFound(new { message = "Не удалось найти тему." });
            }

            var questions = await _context.Questions
                .Where(u => u.ThemeId == themeId)
                .Include(a => a.AnswerOptions)
                .ToListAsync();

            var ticketDto = new TicketDto()
            {
                Id = themeId,
                Title = theme.Title,
                Questions = _mapper.Map<List<QuestionDto>>(questions)
            };

            return Ok(ticketDto);
        }

        /// <summary>
        /// Создать новый билет (вместе с вопросами)
        /// </summary>
        /// <param name="ticket">Билет</param>
        [HttpPost]
        public async Task<ActionResult<TicketDto>> CreateTicket([FromBody] TicketDto dto)
        {
            if (dto == null)
            {
                _logger.Warning("Попытка создать пустой билет");
                return BadRequest(new { message = "Невозможно создать пустой билет" });
            }

            if (!ModelState.IsValid)
            {
                _logger.Warning("Ошибка валидации при создании билета");
                return BadRequest(new { ModelState });
            }

            try
            {
                var ticket = _mapper.Map<Ticket>(dto);

                _context.Tickets.Add(ticket);
                await _context.SaveChangesAsync();

                var resultDto = _mapper.Map<TicketDto>(ticket);

                _logger.Information("Создан новый билет Id = {Id}, Title = {Title}", ticket.Id, ticket.Title);

                return CreatedAtAction(nameof(GetTicketById), new { id = ticket.Id }, resultDto);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при создании билета");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Обновить билет по его ID
        /// </summary>
        /// <param name="id">ID билета</param>
        /// <param name="ticket">Билет</param>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTicket(int id, [FromBody] TicketDto dto)
        {
            if (id != dto.Id)
                return BadRequest(new { message = "ID билета не совпадает" });

            var ticket = await _context.Tickets
                .Include(t => t.Questions)
                .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound(new { message = "Билет не найден" });

            _mapper.Map(dto, ticket);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Удалить билет по его ID
        /// </summary>
        /// <param name="id">ID билета</param>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(u => u.Id == id);

            if (ticket == null)
            {
                _logger.Warning("Попытка удалить несуществующий билет Id = {Id}", id);
                return NotFound(new { message = "Билет не найден" });
            }

            try
            {
                _context.Tickets.Remove(ticket);
                await _context.SaveChangesAsync();

                _logger.Information("Билет Id = {Id} удалён", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при удалении билета Id = {Id}", id);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }
    }
}
