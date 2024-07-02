using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace GutenbergAuthorDownloader
{
    class Program
    {
        private const string GutenbergUrl = "https://www.gutenberg.org";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Введіть прізвище та ім'я автора:");
            string authorName = Console.ReadLine();

            try
            {
                List<BookInfo> books = await GetBooksByAuthor(authorName);
                Console.WriteLine($"Знайдено книг автора {authorName}: {books.Count}");

                foreach (var book in books)
                {
                    await DownloadBook(book);
                }

                Console.WriteLine("Завантаження завершено.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка: {ex.Message}");
            }
        }

        static async Task<List<BookInfo>> GetBooksByAuthor(string authorName)
        {
            List<BookInfo> books = new List<BookInfo>();

            try
            {
                string url = $"{GutenbergUrl}/ebooks/search/?query={Uri.EscapeUriString(authorName)}";
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string html = await response.Content.ReadAsStringAsync();
                    books = ParseBooksByAuthor(html);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при виконанні запиту до Gutenberg.org: {ex.Message}");
            }

            return books;
        }

        static List<BookInfo> ParseBooksByAuthor(string html)
        {
            List<BookInfo> books = new List<BookInfo>();

            try
            {
                int startIndex = html.IndexOf("<ol class=\"results\">");
                int endIndex = html.IndexOf("</ol>", startIndex);

                if (startIndex != -1 && endIndex != -1)
                {
                    string resultsHtml = html.Substring(startIndex, endIndex - startIndex);

                    int index = 0;
                    while ((startIndex = resultsHtml.IndexOf("<li class=\"booklink\">", index)) != -1)
                    {
                        startIndex = resultsHtml.IndexOf("<a href=\"/ebooks/", startIndex);
                        if (startIndex == -1) break;
                        startIndex = resultsHtml.IndexOf(">", startIndex) + 1;
                        endIndex = resultsHtml.IndexOf("</a>", startIndex);

                        if (endIndex != -1)
                        {
                            string link = resultsHtml.Substring(startIndex, endIndex - startIndex).Trim();
                            string title = StripHtmlTags(link);
                            string id = GetBookId(link);

                            books.Add(new BookInfo { Id = id, Title = title });
                            index = endIndex + "</a>".Length;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при парсингу результатів пошуку: {ex.Message}");
            }

            return books;
        }

        static async Task DownloadBook(BookInfo book)
        {
            try
            {
                string url = $"{GutenbergUrl}/ebooks/{book.Id}.txt.utf-8";
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string bookText = await response.Content.ReadAsStringAsync();

                    string filePath = $"{book.Title}.txt";
                    File.WriteAllText(filePath, bookText);

                    Console.WriteLine($"Книга '{book.Title}' завантажена: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при завантаженні книги '{book.Title}': {ex.Message}");
            }
        }

        static string StripHtmlTags(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty);
        }

        static string GetBookId(string link)
        {
            int start = link.LastIndexOf('/') + 1;
            int end = link.LastIndexOf('"');
            return link.Substring(start, end - start);
        }
    }

    public class BookInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }
}
