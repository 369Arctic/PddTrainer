using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PddTrainer.Api.Data;
using PddTrainer.Api.Models;
using PddTrainer.Api.Models.DTO;
using PddTrainer.Api.Services.Interfaces;

namespace PddTrainer.Api.Services
{
    public class AttemptService : IAttemptService
    {
        private readonly ApplicationDbContext _context;
        private readonly ExamSettings _examSettings;

        public AttemptService(ApplicationDbContext context, IOptions<ExamSettings> examOptions)
        {
            _context = context;
            _examSettings = examOptions.Value;
        }

        /// <summary>
        /// Обработать попытку пользователя: провалидировать данные,
        /// проверить ответы, вычислить результат и сохранить его в БД.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="dto">DTO с данными попытки и ответами пользователя.</param>
        /// <returns>Сохранённая сущность попытки.</returns>
        public async Task<Attempt> ProcessAttemptAsync(int userId, SubmitAttemptDto dto)
        {
            ValidateDto(dto);

            var questions = await LoadQuestionsAsync(dto);

            var attemptAnswers = new List<AttemptAnswer>();

            var correct = CheckAnswers(questions, dto, attemptAnswers);
            var total = dto.Answers.Count;
            var mistakes = total - correct;

            var passed = CalculatePass(dto, mistakes);

            var attempt = new Attempt
            {
                UserId = userId,
                Type = dto.Type,
                ThemeId = dto.ThemeId,
                TicketId = dto.TicketId,
                ExamModeId = dto.ExamModeId,
                TotalQuestions = total,
                CorrectAnswers = correct,
                Mistakes = mistakes,
                Passed = passed,
                StartedAt = dto.StartedAt,
                CompletedAt = DateTime.UtcNow,
                Answers = attemptAnswers
            };

            _context.Attempts.Add(attempt);
            await _context.SaveChangesAsync();

            return attempt;
        }

        /// <summary>
        /// Загрузить вопросы из БД по ИД, переданным в DTO, включая варианты ответов.
        /// </summary>
        /// <param name="dto">DTO с ответами пользователя.</param>
        /// <returns>Список найденных вопросов.</returns>
        /// <exception cref="Exception">Выбрасывается, если список вопросов некорректен.</exception>
        private async Task <List<Question>> LoadQuestionsAsync(SubmitAttemptDto dto)
        {
            var questionIds = dto.Answers.Select(u => u.QuestionId).ToList();

            var questions = await _context.Questions
                .Include(u => u.AnswerOptions)
                .Where(u => questionIds.Contains(u.Id))
                .ToListAsync();

            if (questions.Count != questionIds.Count)
                throw new Exception("Invalid question list.");

            return questions;
        }

        /// <summary>
        /// Определить, считается ли попытка успешно пройденной.
        /// Для режима экзамена учитывается допустимое количество ошибок.
        /// </summary>
        /// <param name="dto">DTO попытки.</param>
        /// <param name="mistakes">Количество допущенных ошибок.</param>
        /// <returns>True, если попытка считается успешной, иначе false.</returns>

        private bool CalculatePass(SubmitAttemptDto dto, int mistakes)
        {
            if (dto.Type != AttemptType.Exam)
                return true;

            var mode = _examSettings.Exams.FirstOrDefault(u => u.Id == dto.ExamModeId);

            if (mode == null)
                throw new Exception("Invalid exam mode.");

            if (mode.MaxMistakes.HasValue)
                return mistakes <= mode.MaxMistakes.Value;

            return true;
        }

        /// <summary>
        /// Проверить корректность DTO.
        /// </summary>
        /// <param name="dto">DTO попытки.</param>
        /// <exception cref="Exception">Выбрасывается при некорректных данных.</exception>
        private void ValidateDto(SubmitAttemptDto dto)
        {
            if (dto == null)
                throw new Exception("Dto is null");

            if (dto.Type == AttemptType.Theme && dto.ThemeId == null)
                throw new Exception("Theme Id is required.");

            if (dto.Type == AttemptType.Ticket && dto.TicketId == null)
                throw new Exception("Ticket Id is required.");

            if (dto.Type == AttemptType.Exam && string.IsNullOrEmpty(dto.ExamModeId))
                throw new Exception("ExamMode Id is required.");
        }

        /// <summary>
        /// Проверить ответы пользователя и сформировать список результатов по каждому вопросу.
        /// </summary>
        /// <param name="questions">Список вопросов из БД.</param>
        /// <param name="dto">DTO с ответами пользователя.</param>
        /// <param name="attemptAnswers">Коллекция, в которую добавляются результаты проверки.</param>
        /// <returns>Количество правильных ответов.</returns>
        private int CheckAnswers(List<Question> questions, SubmitAttemptDto dto, List<AttemptAnswer> attemptAnswers)
        {
            int correct = 0;
            var questionDictionary = questions.ToDictionary(u => u.Id);

            foreach (var answer in dto.Answers)
            {
                var question = questionDictionary[answer.QuestionId];
                var correctOptions = question.AnswerOptions.FirstOrDefault(u => u.IsCorrect);
                if (correctOptions == null)
                {
                    throw new Exception($"Question {question.Id} has no correct answer.");
                }

                var isCorrect = answer.SelectedAnswerId == correctOptions.Id;

                if (isCorrect)
                    correct++;

                attemptAnswers.Add(new AttemptAnswer
                {
                    QuestionId = question.Id,
                    SelectedAnswerId = answer.SelectedAnswerId,
                    IsCorrect = isCorrect
                });
            }

            return correct;
        }
    }
}
