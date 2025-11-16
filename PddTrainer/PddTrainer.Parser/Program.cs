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
        var baseUrl = "https://avto-russia.ru/pdd_abma1b1/";
        var ticketNumber = 1;
        var ticketUrl = $"{baseUrl}vesbilet{ticketNumber}.html";

        var projectRoot = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
        var imagesDir = Path.Combine(projectRoot, "images");
        Directory.CreateDirectory(imagesDir);
        /*Либо так
        var projectRoot = AppContext.BaseDirectory; // директория с исполняемым файлом
        var imagesDir = Path.Combine(projectRoot, "images");
        Directory.CreateDirectory(imagesDir);
        */

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        Console.WriteLine($"Скачиваем страницу билета {ticketNumber}...");
        var html = await httpClient.GetStringAsync(ticketUrl); // Получаем HTML страницы

        var doc = new HtmlDocument();
        doc.LoadHtml(html); // Загружаем в HtmlAgilityPack для парсинга

        var ticket = new TicketDto { Title = $"Билет {ticketNumber}" };

        // Находим все <b> с текстом "Билет" и "Вопрос"
        // Это заголовки вопросов
        var questionHeaders = doc.DocumentNode
            .SelectNodes("//b")
            ?.Where(n => !string.IsNullOrWhiteSpace(n.InnerText) &&
                         n.InnerText.Contains("Билет") && n.InnerText.Contains("Вопрос"))
            .ToList();

        if (questionHeaders == null || questionHeaders.Count == 0)
        {
            Console.WriteLine("Не найдено ни одного вопроса.");
            return;
        }

        Console.WriteLine($"Найдено вопросов: {questionHeaders.Count}");
        var baseUri = new Uri(baseUrl);

        for (int i = 0; i < questionHeaders.Count; i++)
        {
            var question = new QuestionDto();
            var start = questionHeaders[i];
            var end = i + 1 < questionHeaders.Count ? questionHeaders[i + 1] : null;

            // Собираем все узлы между текущим и следующим вопросом
            var nodesBetween = GetNodesBetween(start, end);

            // Находим картинку вопроса (если есть)
            var imgNode = FindQuestionImage(start, nodesBetween, doc);
            if (imgNode != null)
                question.ImageUrl = await DownloadImageAsync(httpClient, imgNode, baseUri, imagesDir, ticketNumber, i + 1);

            // Извлекаем текст вопроса
            question.Text = ExtractQuestionText(nodesBetween, start);

            // Извлекаем варианты ответов
            question.AnswerOptions = ExtractAnswerOptions(nodesBetween);

            // Извлекаем пояснение (hint)
            var hint = nodesBetween.FirstOrDefault(n => !string.IsNullOrEmpty(n.Id) && n.Id.StartsWith("idDivHint", StringComparison.OrdinalIgnoreCase));
            if (hint != null)
                question.Explanation = HtmlEntity.DeEntitize(hint.InnerText.Trim());

            // Пропускаем полностью пустые вопросы
            if (!string.IsNullOrWhiteSpace(question.Text) || question.AnswerOptions.Count > 0 || !string.IsNullOrWhiteSpace(question.Explanation) || !string.IsNullOrWhiteSpace(question.ImageUrl))
                ticket.Questions.Add(question);
        }


        // Выводим JSON в консоль
        Console.WriteLine("Собранный JSON:");
        Console.WriteLine(JsonConvert.SerializeObject(ticket, Formatting.Indented));

        // Отправка JSON на API
        var apiUrl = "https://localhost:7269/api/Tickets";
        var json = JsonConvert.SerializeObject(ticket);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        Console.WriteLine($"Отправляем билет на API: {apiUrl}");
        var response = await httpClient.PostAsync(apiUrl, content);
        Console.WriteLine($"Ответ API: {response.StatusCode}");

        Console.ReadLine();
    }

    /// <summary>
    /// Собирает все узлы между текущим заголовком вопроса и следующим
    /// 1) Берём все потомки родителя после <b>
    /// 2) Добавляем соседние узлы на уровне родителя до следующего заголовка
    /// </summary>
    static List<HtmlNode> GetNodesBetween(HtmlNode start, HtmlNode end)
    {
        var nodes = new List<HtmlNode>();
        bool afterHeader = false;
        var parent = start.ParentNode;

        // Проходим по потомкам родителя
        foreach (var child in parent.ChildNodes)
        {
            if (!afterHeader)
            {
                if (child == start || child.DescendantsAndSelf().Any(d => d == start))
                    afterHeader = true; // Начинаем собирать после <b>
                continue;
            }
            if (child.NodeType == HtmlNodeType.Element)
                nodes.Add(child);
        }

        // Добавляем соседние элементы родителя до конца или до следующего заголовка
        for (var node = parent.NextSibling; node != null && node != end?.ParentNode; node = node.NextSibling)
            if (node.NodeType == HtmlNodeType.Element)
                nodes.Add(node);

        return nodes;
    }

    /// <summary>
    /// Находит наиболее подходящее изображение для вопроса
    /// </summary>
    static HtmlNode FindQuestionImage(HtmlNode headerNode, List<HtmlNode> nodesBetween, HtmlDocument doc)
    {
        HtmlNode ChooseBest(IEnumerable<HtmlNode> imgs)
        {
            // Сначала ищем по alt (содержит 'картин')
            foreach (var img in imgs)
            {
                var alt = img.GetAttributeValue("alt", "") ?? "";
                if (alt.IndexOf("картин", StringComparison.OrdinalIgnoreCase) >= 0) return img;

                var cls = img.GetAttributeValue("class", "") ?? "";
                if (cls.IndexOf("img-responsive", StringComparison.OrdinalIgnoreCase) >= 0) return img;

                var src = img.GetAttributeValue("src", "") ?? "";
                if (src.IndexOf("pdd-", StringComparison.OrdinalIgnoreCase) >= 0) return img;
            }
            return imgs.FirstOrDefault(); // если ничего не найдено, берём первое
        }

        var startParent = headerNode.ParentNode;

        // 1) Проверка в родителе заголовка
        // 2) Проверка среди узлов между
        // 3) Проверка ближайших соседей родителя
        // 4) Поиск по всему документу по alt
        return ChooseBest(startParent?.Descendants("img") ?? Enumerable.Empty<HtmlNode>())
            ?? ChooseBest(nodesBetween.SelectMany(n => n.Descendants("img")))
            ?? Enumerable.Range(0, 8).SelectMany(_ => startParent?.NextSibling?.Descendants("img") ?? Enumerable.Empty<HtmlNode>()).FirstOrDefault()
            ?? doc.DocumentNode.SelectSingleNode("//img[contains(translate(@alt, 'К', 'к'), 'картин')]");
    }

    /// <summary>
    /// Загружает изображение по URL и сохраняет локально
    /// </summary>
    static async Task<string> DownloadImageAsync(HttpClient httpClient, HtmlNode imgNode, Uri baseUri, string imagesDir, int ticketNumber, int questionIndex)
    {
        // Убираем ведущие ./ или /
        var rawSrc = imgNode.GetAttributeValue("src", "").Trim().TrimStart('.', '/');
        Uri imgUri;
        try { imgUri = new Uri(baseUri, rawSrc); }
        catch { imgUri = new Uri(baseUri, Uri.EscapeUriString(rawSrc)); } // на всякий случай эскейпим

        var ext = Path.GetExtension(imgUri.LocalPath);
        if (string.IsNullOrEmpty(ext)) ext = ".jpg"; // дефолтное расширение

        var imgPath = Path.Combine(imagesDir, $"ticket{ticketNumber}_q{questionIndex}{ext}");
        try
        {
            var data = await httpClient.GetByteArrayAsync(imgUri.AbsoluteUri);
            await File.WriteAllBytesAsync(imgPath, data);
            return imgPath.Replace("\\", "/"); // используем слеш для URL
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не удалось скачать изображение: {imgUri.AbsoluteUri} — {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Извлекает текст вопроса
    /// Сначала ищем <div> с font-weight: bold, иначе длинный текст, иначе берём заголовок
    /// </summary>
    static string ExtractQuestionText(List<HtmlNode> nodesBetween, HtmlNode headerNode)
    {
        var textDiv = nodesBetween.FirstOrDefault(n => n.Name == "div" && n.GetAttributeValue("style", "").ToLower().Contains("font-weight: bold"));
        if (textDiv != null) return HtmlEntity.DeEntitize(textDiv.InnerText.Trim());

        var candidate = nodesBetween.Where(n => n.NodeType == HtmlNodeType.Element)
            .Select(n => n.InnerText.Trim())
            .FirstOrDefault(t => !string.IsNullOrEmpty(t) && t.Length > 10 && !Regex.IsMatch(t, @"^\d+\.\s"));
        return !string.IsNullOrEmpty(candidate) ? HtmlEntity.DeEntitize(candidate) : HtmlEntity.DeEntitize(headerNode.InnerText.Trim());
    }

    /// <summary>
    /// Извлекает варианты ответов
    /// Находит div с padding:5px, проверяет правильность по цвету или тегу <b>
    /// </summary>
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
