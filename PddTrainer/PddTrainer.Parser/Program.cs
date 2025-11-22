using AngleSharp;
using HtmlAgilityPack;
using Newtonsoft.Json;
using PddTrainer.Parser.PddModels;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

class Program
{
    static async Task Main(string[] args)
    {
        // ----------------------------
        // Настройки
        // ----------------------------
        var baseUrl = "https://avto-russia.ru/pdd_abma1b1/";
        var totalTickets = 40; // Количество билетов
        var apiUrl = "https://localhost:7269/api/Tickets";

        // Папка для картинок
        var projectRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..");
        var solutionRoot = Directory.GetParent(projectRoot)!.FullName;  
        var apiProjectRoot = Path.Combine(solutionRoot, "PddTrainer.Api");
        var imagesDir = Path.Combine(apiProjectRoot, "imagesDir");

        Directory.CreateDirectory(imagesDir);

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        var allTickets = new List<TicketDto>();

        // ----------------------------
        // Основной цикл по всем билетам
        // ----------------------------
        for (int ticketNumber = 1; ticketNumber <= totalTickets; ticketNumber++)
        {
            var ticketUrl = $"{baseUrl}vesbilet{ticketNumber}.html";
            Console.WriteLine($"Скачиваем страницу билета {ticketNumber}...");

            var html = await httpClient.GetStringAsync(ticketUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var ticket = new TicketDto { Title = $"Билет {ticketNumber}" };

            // Находим все заголовки вопросов
            var questionHeaders = doc.DocumentNode
                .SelectNodes("//b")
                ?.Where(n => !string.IsNullOrWhiteSpace(n.InnerText) &&
                             n.InnerText.Contains("Билет") && n.InnerText.Contains("Вопрос"))
                .ToList();

            if (questionHeaders == null || questionHeaders.Count == 0)
            {
                Console.WriteLine($"В билете {ticketNumber} не найдено вопросов.");
                continue;
            }

            Console.WriteLine($"Найдено вопросов: {questionHeaders.Count}");
            var baseUri = new Uri(baseUrl);

            for (int i = 0; i < questionHeaders.Count; i++)
            {
                var start = questionHeaders[i];
                var end = i + 1 < questionHeaders.Count ? questionHeaders[i + 1] : null;

                var nodesBetween = GetNodesBetween(start, end);

                var question = new QuestionDto
                {
                    Text = ExtractQuestionText(nodesBetween, start),
                    AnswerOptions = ExtractAnswerOptions(nodesBetween),
                    Explanation = nodesBetween.FirstOrDefault(n => !string.IsNullOrEmpty(n.Id) && n.Id.StartsWith("idDivHint", StringComparison.OrdinalIgnoreCase))?.InnerText
                };

                var imgNode = FindQuestionImage(start, nodesBetween, doc);
                if (imgNode != null)
                    question.ImageUrl = await DownloadImageAsync(httpClient, imgNode, baseUri, imagesDir, ticketNumber, i + 1);

                // Пропускаем полностью пустые вопросы
                if (!string.IsNullOrWhiteSpace(question.Text) || question.AnswerOptions.Count > 0 || !string.IsNullOrWhiteSpace(question.Explanation) || !string.IsNullOrWhiteSpace(question.ImageUrl))
                    ticket.Questions.Add(question);
            }

            allTickets.Add(ticket);

            // Отправляем билет на API
            var json = JsonConvert.SerializeObject(ticket);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(apiUrl, content);
            Console.WriteLine($"Билет {ticketNumber} отправлен. Ответ API: {response.StatusCode}");
        }

        // ----------------------------
        // По желанию: вывести все билеты в один JSON
        // ----------------------------
        Console.WriteLine("Все билеты:");
        Console.WriteLine(JsonConvert.SerializeObject(allTickets, Formatting.Indented));

        Console.ReadLine();
    }

    // ----------------------------
    // Собирает узлы между текущим и следующим заголовком вопроса
    // ----------------------------
    static List<HtmlNode> GetNodesBetween(HtmlNode start, HtmlNode end)
    {
        var nodes = new List<HtmlNode>();
        bool afterHeader = false;
        var parent = start.ParentNode;

        foreach (var child in parent.ChildNodes)
        {
            if (!afterHeader)
            {
                if (child == start || child.DescendantsAndSelf().Any(d => d == start))
                    afterHeader = true;
                continue;
            }
            if (child.NodeType == HtmlNodeType.Element)
                nodes.Add(child);
        }

        for (var node = parent.NextSibling; node != null && node != end?.ParentNode; node = node.NextSibling)
            if (node.NodeType == HtmlNodeType.Element)
                nodes.Add(node);

        return nodes;
    }

    // ----------------------------
    // Находит картинку вопроса
    // ----------------------------
    static HtmlNode FindQuestionImage(HtmlNode headerNode, List<HtmlNode> nodesBetween, HtmlDocument doc)
    {
        HtmlNode ChooseBest(IEnumerable<HtmlNode> imgs)
        {
            foreach (var img in imgs)
            {
                var alt = img.GetAttributeValue("alt", "") ?? "";
                if (alt.IndexOf("картин", StringComparison.OrdinalIgnoreCase) >= 0) return img;

                var cls = img.GetAttributeValue("class", "") ?? "";
                if (cls.IndexOf("img-responsive", StringComparison.OrdinalIgnoreCase) >= 0) return img;

                var src = img.GetAttributeValue("src", "") ?? "";
                if (src.IndexOf("pdd-", StringComparison.OrdinalIgnoreCase) >= 0) return img;
            }
            return imgs.FirstOrDefault();
        }

        var startParent = headerNode.ParentNode;

        return ChooseBest(startParent?.Descendants("img") ?? Enumerable.Empty<HtmlNode>())
            ?? ChooseBest(nodesBetween.SelectMany(n => n.Descendants("img")))
            ?? Enumerable.Range(0, 8).SelectMany(_ => startParent?.NextSibling?.Descendants("img") ?? Enumerable.Empty<HtmlNode>()).FirstOrDefault()
            ?? doc.DocumentNode.SelectSingleNode("//img[contains(translate(@alt, 'К', 'к'), 'картин')]");
    }

    // ----------------------------
    // Загружает изображение и сохраняет локально
    // ----------------------------
    static async Task<string> DownloadImageAsync(HttpClient httpClient, HtmlNode imgNode, Uri baseUri, string imagesDir, int ticketNumber, int questionIndex)
    {
        var rawSrc = imgNode.GetAttributeValue("src", "").Trim().TrimStart('.', '/');
        Uri imgUri;
        try { imgUri = new Uri(baseUri, rawSrc); }
        catch { imgUri = new Uri(baseUri, Uri.EscapeUriString(rawSrc)); }

        var ext = Path.GetExtension(imgUri.LocalPath);
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";

        var imgPath = Path.Combine(imagesDir, $"ticket{ticketNumber}_q{questionIndex}{ext}");
        try
        {
            var data = await httpClient.GetByteArrayAsync(imgUri.AbsoluteUri);
            await File.WriteAllBytesAsync(imgPath, data);
            return imgPath.Replace("\\", "/");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не удалось скачать изображение: {imgUri.AbsoluteUri} — {ex.Message}");
            return null;
        }
    }

    // ----------------------------
    // Извлекает текст вопроса
    // ----------------------------
    static string ExtractQuestionText(List<HtmlNode> nodesBetween, HtmlNode headerNode)
    {
        var textDiv = nodesBetween.FirstOrDefault(n => n.Name == "div" && n.GetAttributeValue("style", "").ToLower().Contains("font-weight: bold"));
        if (textDiv != null) return HtmlEntity.DeEntitize(textDiv.InnerText.Trim());

        var candidate = nodesBetween.Where(n => n.NodeType == HtmlNodeType.Element)
            .Select(n => n.InnerText.Trim())
            .FirstOrDefault(t => !string.IsNullOrEmpty(t) && t.Length > 10 && !Regex.IsMatch(t, @"^\d+\.\s"));
        return !string.IsNullOrEmpty(candidate) ? HtmlEntity.DeEntitize(candidate) : HtmlEntity.DeEntitize(headerNode.InnerText.Trim());
    }

    // ----------------------------
    // Извлекает варианты ответов
    // ----------------------------
    static List<AnswerOptionDto> ExtractAnswerOptions(List<HtmlNode> nodesBetween)
    {
        var list = new List<AnswerOptionDto>();
        var answerDivs = nodesBetween.Where(n => n.Name == "div" && n.GetAttributeValue("style", "").ToLower().Contains("padding:5px"));
        foreach (var a in answerDivs)
        {
            var raw = a.InnerText.Trim();
            if (!Regex.IsMatch(raw, @"^\s*\d+\.\s*")) continue;

            var text = Regex.Replace(raw, @"^\s*\d+\.\s*", "");
            var isCorrect = a.GetAttributeValue("style", "").ToLower().Contains("forestgreen") || a.SelectSingleNode(".//b") != null;
            list.Add(new AnswerOptionDto { Text = HtmlEntity.DeEntitize(text), IsCorrect = isCorrect });
        }
        return list;
    }
}
