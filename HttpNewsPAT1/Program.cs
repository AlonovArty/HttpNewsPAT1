using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace HttpNewsPAT1
{
    public class Program
    {
        static void Main(string[] args)
        {
            SetupDebugOutputToFile();
            //Cookie token = SingIn("admin", "admin");
            //string Content = GetContent(token);
            //ParsingHtml(Content);
            //Console.Read();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, errors) => true; 

            string content = GetContentFromAvito();
            if (!string.IsNullOrEmpty(content))
            {
                ParsingHtmlAvito(content);
            }

            Console.WriteLine("Готово. Нажмите любую клавишу...");
            Console.ReadKey();
            Cookie token = SingIn("admin", "admin");

            string result = AddItem(
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
                    var titleNode = item.SelectSingleNode(".//a[@data-marker='item-title']//h3 | .//a[@data-marker='item-title']//div");
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

        public static string GetContentFromAvito()
        {
            string url = "https://www.avito.ru/moskva/kvartiry/sdam/na_dlitelnyy_srok-ASgBAgICAkSSA8gQ8AeQUg?cd=1";

            Debug.WriteLine($"Выполняем запрос: {url}");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";

            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.Headers.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
            request.Referer = "https://www.avito.ru/";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            request.CookieContainer = new CookieContainer();

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Debug.WriteLine($"Статус: {response.StatusCode}");
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine($"Ошибка сети: {ex.Message}");
                if (ex.Response != null)
                {
                    using (var errResp = (HttpWebResponse)ex.Response)
                    using (var stream = errResp.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        string errorHtml = reader.ReadToEnd();
                        File.WriteAllText("error_page.html", errorHtml);
                        Console.WriteLine("Страница с ошибкой/защитой сохранена в error_page.html");
                    }
                }
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
            foreach(HtmlNode DivNews in DivsNews)
            {
                var src = DivNews.ChildNodes[1].GetAttributeValue("src", "none");
                var name = DivNews.ChildNodes[3].InnerText;
                var description = DivNews.ChildNodes[5].InnerText;
                Console.WriteLine(name + "\n" + "Изображение: " + src + "\n" + "Описание: " + description + "\n");
            }
        }

        public static Cookie SingIn(string Login,string Password)
        {

            Cookie token = null;
            string url = "http://localhost/ajax/login.php";
            Debug.WriteLine($"Выполняем запрос: {url}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = new CookieContainer();
            string postData = $"login={Login}&password={Password}";
            byte[] Data = Encoding.ASCII.GetBytes(postData);
            request.ContentLength = Data.Length;
            using(var stream = request.GetRequestStream())
            {
                stream.Write(Data, 0, Data.Length);
                
            }
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                 Debug.WriteLine($"Статус выполнения: {response.StatusCode}");
            string responseFromServer = new StreamReader(response.GetResponseStream()).ReadToEnd();
            Console.WriteLine(responseFromServer);
                token = response.Cookies["token"];
            }
           
            return token;
        }


        public static string AddItem(Cookie token, string src, string name, string description)
        {
            string url = "http://localhost/ajax/add.php";
            Debug.WriteLine($"Выполняем запрос: {url}");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(token);

            string postData = $"src={Uri.EscapeDataString(src)}" +
                              $"&name={Uri.EscapeDataString(name)}" +
                              $"&description={Uri.EscapeDataString(description)}";

            byte[] data = Encoding.UTF8.GetBytes(postData);
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
                stream.Write(data, 0, data.Length);

            string responseText;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                Debug.WriteLine($"Статус выполнения: {response.StatusCode}");
                responseText = reader.ReadToEnd();
            }

            return responseText;
        }

        public static void Open()
        {
            WebRequest request = WebRequest.Create("http://localhost/main");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Console.Write(response.StatusDescription);
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            Console.WriteLine(responseFromServer);
            reader.Close();
            dataStream.Close();
            response.Close();
            Console.Read();
        }

        public static string GetContent(Cookie Token)
        {
            string Content = null ;
            string url = "http://localhost/main";
            Debug.WriteLine($"Выполняем запрос: {url}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(Token);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Debug.WriteLine($"Статус выполнения: {response.StatusCode}");
                Content = new StreamReader(response.GetResponseStream()).ReadToEnd();
                
            }
            return Content;
           
        }

     
    }
}
