using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using PddTrainer.ThemeImport.PddModels;

class Program
{
    private static readonly string themesUrl = "https://www.drom.ru/pdd/themes/";
    private static readonly string apiUrl = "https://localhost:7269/api/themes";

    static async Task Main()
    {
        // Регистрация провайдера кодировок для поддержки "windows-1251" и других кодировок, не включённых по умолчанию в .NET Core.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var http = new HttpClient();

        Console.WriteLine("Загрузка HTML с сайта drom.ru...");

        // Получаем байты и декодируем их в строку с учётом кодировки в заголовке (если есть).
        byte[] bytes;
        try
        {
            bytes = await http.GetByteArrayAsync(themesUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при загрузке страницы: {ex.Message}");
            return;
        }

        // Попытка прочитать кодировку из заголовка Content-Type
        string charset = null;
        try
        {
            // Делаем отдельный HEAD-запрос или получаем Content-Type из предыдущего запроса.
            // Т.к байты уже есть - получаем заголовки
            using var headReq = new HttpRequestMessage(HttpMethod.Head, themesUrl);
            using var headResp = await http.SendAsync(headReq);
            charset = headResp.Content.Headers.ContentType?.CharSet;
        }
        catch
        {
            charset = null;
        }

        string html;

        if (!string.IsNullOrWhiteSpace(charset))
        {
            try
            {
                var enc = Encoding.GetEncoding(charset);
                html = enc.GetString(bytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось использовать кодировку '{charset}': {ex.Message}. Попытка автоопределения...");
                html = TryDecodeWithFallbacks(bytes);
            }
        }
        else
        {
            html = TryDecodeWithFallbacks(bytes);
        }

        Console.WriteLine("HTML получен. Парсинг...");

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var nodes = doc.DocumentNode.SelectNodes("//div[@class='b-withPddLegacyIcon']/a");

        var themes = new List<ThemeDto>();

        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                string text = node.InnerText.Trim();

                // Пример: "Дорожные знаки (127 вопросов)" будет как "Дорожные знаки"
                int idx = text.IndexOf("(");
                if (idx > 0)
                    text = text[..idx].Trim();
                string url = node.GetAttributeValue("href", "").Trim();

                themes.Add(new ThemeDto
                {
                    Title = text,
                    SourceUrl = url
                });
            }
        }

        Console.WriteLine($"Найдено тем: {themes.Count}");

        foreach (var theme in themes)
        {
            Console.WriteLine($"Импорт темы: {theme.Title} ({theme.SourceUrl})");

            var response = await http.PostAsJsonAsync(apiUrl, theme);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Успешно: {theme.Title}");
            }
            else
            {
                string content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ошибка импорта {theme.Title}: {content}");
            }

            await Task.Delay(150);
        }

        Console.WriteLine("Импорт тем завершен");
        Console.ReadLine();
    }

    private static string TryDecodeWithFallbacks(byte[] bytes)
    {
        // Попытки декодирования: windows-1251 -> utf-8 -> utf-8 с BOM-safe -> последний резорт: default Encoding.UTF8
        var encodingsToTry = new[] { "windows-1251", "utf-8", "windows-1252" };

        foreach (var name in encodingsToTry)
        {
            try
            {
                var enc = Encoding.GetEncoding(name);
                var s = enc.GetString(bytes);
                // Проверка на содержание кириллицы или теги <html>
                if (!string.IsNullOrWhiteSpace(s) && (s.Contains("<html", StringComparison.OrdinalIgnoreCase) || s.Any(c => c >= 0x0400 && c <= 0x04FF)))
                    return s;
            }
            catch
            {
                // игнорируем неудачные кодировки
            }
        }

        // Попытка UTF8 с проверкой ошибок
        try
        {
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            // Крайняя попытка использовать кодировку по умолчанию.
            return Encoding.Default.GetString(bytes);
        }
    }
}