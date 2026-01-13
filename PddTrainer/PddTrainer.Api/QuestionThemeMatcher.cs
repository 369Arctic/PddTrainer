using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PddTrainer.Api.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
                
                await Task.Delay(200);
            }
            _logger.LogInformation("Привязка вопросов к темам завершена.");
            _logger.LogInformation($"Привязано вопросов: {linked}");
            _logger.LogInformation($"Не найдено в БД: {notFound}");
        }

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
                catch
                {
                    // Намеренное игнорирование
                }
            }

            return Encoding.UTF8.GetString(bytes);
        }

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
