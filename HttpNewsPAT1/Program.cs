using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace HttpNewsPAT1
{
    public class Program
    {
        private static HttpClient _httpClient;

        static async Task Main(string[] args)
        {
            SetupDebugOutputToFile();
            //Cookie token = await SingIn("admin", "admin");
            //string Content = await GetContent(token);
            //ParsingHtml(Content);
            //Console.Read();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, errors) => true;

            _httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) => true
            });

            string content = await GetContentFromAvito();
            if (!string.IsNullOrEmpty(content))
            {
                ParsingHtmlAvito(content);
            }

            Console.WriteLine("Готово. Нажмите любую клавишу...");
            Console.ReadKey();
            Cookie token = await SingIn("admin", "admin");

            string result = await AddItem(
                token,
                "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSQBAOUJ51mQFQoSE6iwW8YVk-yKjz4u5LAtQ&s",
                "Объявляем сбор гуманитарной помощи для участников СВО",
                "В Авиатехникуме проходит сбор гуманитарной помощи для военнослужащих — участников специальной военной операции. Приглашаем всех студентов, преподавателей и сотрудников присоединиться к важной инициативе."
            );

            Console.WriteLine(result);
        }

        public static void ParsingHtmlAvito(string htmlCode)
        {
            var html = new HtmlDocument();
            html.LoadHtml(htmlCode);

            var items = html.DocumentNode.SelectNodes("//div[@data-marker='item']");

            if (items == null || items.Count == 0)
            {
                Console.WriteLine("Объявления не найдены — возможно, сработала защита или изменилась структура.");
                File.WriteAllText("last_page.html", htmlCode);
                Console.WriteLine("HTML страницы сохранён в last_page.html — открой и посмотри, что пришло.");
                return;
            }

            Console.WriteLine($"Найдено объявлений: {items.Count}\n");

            foreach (var item in items)
            {
                try
                {
                    var titleNode = item.SelectSingleNode(".//a[@data-marker='item-title'] | .//a[@data-marker='title']");
                    var priceNode = item.SelectSingleNode(".//span[@data-marker='item-price']//span | .//meta[@itemprop='price']");
                    var linkNode = item.SelectSingleNode(".//a[@data-marker='item-title']");
                    var locationNode = item.SelectSingleNode(".//div[@data-marker='item-location']//span");

                    string title = WebUtility.HtmlDecode(titleNode?.InnerText.Trim() ?? "Нет названия");
                    string price = priceNode?.GetAttributeValue("content", priceNode?.InnerText.Trim() ?? "Цена не указана");
                    string link = linkNode != null ? "https://www.avito.ru" + linkNode.GetAttributeValue("href", "") : "";
                    string location = locationNode?.InnerText.Trim() ?? "";

                    Console.WriteLine($"Название: {title}");
                    Console.WriteLine($"Цена: {price}");
                    Console.WriteLine($"Ссылка: {link}");
                    Console.WriteLine($"Место: {location}");
                    Console.WriteLine(new string('-', 60));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка парсинга одного объявления: {ex.Message}");
                }
            }
        }

        public static async Task<string> GetContentFromAvito()
        {
            string url = "https://www.avito.ru/moskva/kvartiry/sdam/na_dlitelnyy_srok-ASgBAgICAkSSA8gQ8AeQUg?cd=1";

            Debug.WriteLine($"Выполняем запрос: {url}");

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                request.Headers.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
                request.Headers.Referrer = new Uri("https://www.avito.ru/");

                var response = await _httpClient.SendAsync(request);
                Debug.WriteLine($"Статус: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine($"Ошибка HTTP: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сети: {ex.Message}");
                return null;
            }
        }

        private static void SetupDebugOutputToFile()
        {
            string logFilePath = "debug_log.txt";
            TextWriterTraceListener traceListener = new TextWriterTraceListener(logFilePath);
            Debug.Listeners.Clear();
            Debug.Listeners.Add(traceListener);
            Debug.AutoFlush = true;
            Debug.WriteLine($"=== Начало сеанса: {DateTime.Now} ===");
        }

        public static void ParsingHtml(string htmlCode)
        {
            var html = new HtmlDocument();
            html.LoadHtml(htmlCode);
            var Document = html.DocumentNode;
            IEnumerable DivsNews = Document.Descendants(0).Where(n => n.HasClass("news"));
            foreach (HtmlNode DivNews in DivsNews)
            {
                var src = DivNews.ChildNodes[1].GetAttributeValue("src", "none");
                var name = DivNews.ChildNodes[3].InnerText;
                var description = DivNews.ChildNodes[5].InnerText;
                Console.WriteLine(name + "\n" + "Изображение: " + src + "\n" + "Описание: " + description + "\n");
            }
        }

        public static async Task<Cookie> SingIn(string Login, string Password)
        {
            Cookie token = null;
            string url = "http://localhost/ajax/login.php";
            Debug.WriteLine($"Выполняем запрос: {url}");

            try
            {
                var handler = new HttpClientHandler();
                handler.UseCookies = true;
                var localHttpClient = new HttpClient(handler);

                var postData = $"login={Login}&password={Password}";
                var content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded");

                var response = await localHttpClient.PostAsync(url, content);
                Debug.WriteLine($"Статус выполнения: {response.StatusCode}");

                string responseFromServer = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseFromServer);

                var cookies = handler.CookieContainer.GetCookies(new Uri("http://localhost"));
                token = cookies["token"];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            return token;
        }

        public static async Task<string> AddItem(Cookie token, string src, string name, string description)
        {
            string url = "http://localhost/ajax/add.php";
            Debug.WriteLine($"Выполняем запрос: {url}");

            try
            {
                var localHttpClient = new HttpClient();

                var postData = $"src={Uri.EscapeDataString(src)}" +
                              $"&name={Uri.EscapeDataString(name)}" +
                              $"&description={Uri.EscapeDataString(description)}";

                var content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded");

                if (token != null)
                {
                    localHttpClient.DefaultRequestHeaders.Add("Cookie", $"token={token.Value}");
                }

                var response = await localHttpClient.PostAsync(url, content);
                Debug.WriteLine($"Статус выполнения: {response.StatusCode}");

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return null;
            }
        }

        public static async Task Open()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost/main");
                Console.Write(response.StatusCode);
                string responseFromServer = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseFromServer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        public static async Task<string> GetContent(Cookie Token)
        {
            string Content = null;
            string url = "http://localhost/main";
            Debug.WriteLine($"Выполняем запрос: {url}");

            try
            {
                var localHttpClient = new HttpClient();

                if (Token != null)
                {
                    localHttpClient.DefaultRequestHeaders.Add("Cookie", $"token={Token.Value}");
                }

                var response = await localHttpClient.GetAsync(url);
                Debug.WriteLine($"Статус выполнения: {response.StatusCode}");

                Content = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            return Content;
        }
    }

}
