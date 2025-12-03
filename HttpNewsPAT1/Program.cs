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
            Cookie token = SingIn("user", "user");
            string Content = GetContent(token);
            ParsingHtml(Content);
            Console.Read();

         
        }

        public static void ParsingAvitoHtml(string htmlCode)
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

        private static void SetupDebugOutputToFile()
        {
            string logFilePath = "debug_log.txt";
            TextWriterTraceListener traceListener = new TextWriterTraceListener(logFilePath);
            Debug.Listeners.Clear();
            Debug.Listeners.Add(traceListener);
            Debug.AutoFlush = true;
            Debug.WriteLine($"=== Начало сеанса: {DateTime.Now} ===");
        }
    }
}
