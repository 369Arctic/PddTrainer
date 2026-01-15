using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using PddTrainer.Api.Data;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace PddTrainer.Api
{
    internal class QuestionThemeMatcher
    {
        private readonly ApplicationDbContext _db;
        private readonly HttpClient _http;
        private readonly ILogger<QuestionThemeMatcher> _logger;

        public QuestionThemeMatcher(ApplicationDbContext db, HttpClient http, ILogger<QuestionThemeMatcher> logger)
        {
            _db = db;
            _http = http;
            _logger = logger;
        }

        /// <summary>
        /// Привязать вопросы к темам.
        /// </summary>
        public async Task MatchAsync()
        {
            _logger.LogInformation("Начало привязки вопросв к темам.");

            var themes = await _db.Themes.Where(u => !string.IsNullOrEmpty(u.SourceUrl))
                .ToListAsync();
            _logger.LogInformation($"Найдено тем {themes.Count}");

            var questions = await _db.Questions.Where(u => u.ThemeId == null)
                .Select(q => new
                {
                    q.Id,
                    q.Text
                })
                .ToListAsync();
            _logger.LogInformation($"Найдено вопросов без темы: {questions.Count}");

            var questionMap = new Dictionary<string, List<int>>(StringComparer.Ordinal);

            foreach (var question in questions)
            {
                var key = Normalize(question.Text);

                if (!questionMap.ContainsKey(key))
                {
                    questionMap[key] = new List<int>();
                }

                questionMap[key].Add(question.Id);
            }

            var linked = 0;
            var notFound = 0;

            foreach (var theme in themes)
            {
                _logger.LogInformation($"Начата обработка темы: {theme.Title}");

                string html;
                try
                {
                    html = await LoadHtmlAsync(theme.SourceUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Не удалось загрузить HTML-страницу с URL: {theme.SourceUrl}");
                    continue;
                }

                var parsedTitles = ParseQuestionTitles(html);

                foreach (var title in parsedTitles)
                {
                    var key = Normalize(title);
                    if (!questionMap.TryGetValue(key, out var questionIds))
                    {
                        notFound++;
                        _logger.LogWarning($"Не удалось найти в БД вопрос: {title}");
                        continue;
                    }

                    // Присвоение одной темы для всех вопросов с одинаковым текстом.
                    await _db.Questions
                        .Where(q => questionIds.Contains(q.Id) && q.ThemeId == null)
                        .ExecuteUpdateAsync(s => s.SetProperty(q => q.ThemeId, theme.Id));

                    linked ++;
                }
                // Небольшая задержка перед переходом к следующей теме.
                await Task.Delay(200);
            }
            _logger.LogInformation("Привязка вопросов к темам завершена.");
            _logger.LogInformation($"Привязано вопросов: {linked}");
            _logger.LogInformation($"Не найдено в БД: {notFound}");
        }

        /// <summary>
        /// Загрузить HTML по URL, определить корректную кодировку.
        /// </summary>
        /// <param name="url">URL страницы для загрузки.</param>
        /// <returns>HTML текст страницы.</returns>
        /// <remarks>
        /// Метод пробует кодировки: windows-1251, utf-8, windows-1252.
        /// Если ни одна не подходит, возвращает строку в UTF-8.
        /// Если возникает ошибка при декодировании - кодировка логируется.
        /// </remarks>
        private async Task<string> LoadHtmlAsync(string url)
        {
            var bytes = await _http.GetByteArrayAsync(url);

            foreach (var encName in new[] { "windows-1251", "utf-8", "windows-1252" })
            {
                try
                {
                    var enc = Encoding.GetEncoding(encName);
                    var text = enc.GetString(bytes);

                    if (text.Contains("<html", StringComparison.OrdinalIgnoreCase))
                        return text;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Не удалось декодировать HTML с кодировкой {encName}: {ex.Message}");
                }
            }

            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Распарсить HTML страницу и извлечь заголовки вопросов.
        /// </summary>
        /// <param name="html">HTML текст страницы.</param>
        /// <returns>Список заголовков вопросов в виде строк.</returns>
        private static List<string> ParseQuestionTitles(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'b-title_type_h4')]");

            if (nodes == null)
                return new List<string>();

            return nodes.Select(u => WebUtility.HtmlDecode(u.InnerText))
                .Select(u => u.Trim())
                .Where(u => !string.IsNullOrEmpty(u))
                .ToList();
        }

        /// <summary>
        /// Нормализация текст вопроса для корректного сравнения.
        /// </summary>
        /// <param name="text">Исходный текст вопроса.</param>
        /// <returns>Нормализованный текст в нижнем регистре, без лишних пробелов и специальных символов.</returns>
        /// <remarks>
        /// Заменяет 'ё' на 'е'.
        /// Убирает лишние пробелы.
        /// Убирает все символы, кроме букв, цифр и пробелов.
        /// Декодирует HTML сущности.
        /// </remarks>
        private static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            text = WebUtility.HtmlDecode(text);
            text = text.Replace('ё', 'е')
                .Replace('Ё', 'Е');

            text = Regex.Replace(text, @"\s+", " ");
            text = Regex.Replace(text, @"[^\p{L}\p{Nd}\s]", "");

            return text.Trim().ToLowerInvariant();
        }
    }
}
