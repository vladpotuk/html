using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GutenbergSearchConsoleApp
{
    class Program
    {
        private const string GutenbergUrl = "https://www.gutenberg.org";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Введіть текст для пошуку (назву книги або автора):");
            string searchText = Console.ReadLine();

            try
            {
                List<BookInfo> searchResults = await SearchBooks(searchText);

                if (searchResults.Count > 0)
                {
                    Console.WriteLine($"Знайдено книг: {searchResults.Count}\n");
                    foreach (var book in searchResults)
                    {
                        Console.WriteLine($"Назва: {book.Title}");
                        Console.WriteLine($"Автор(и): {string.Join(", ", book.Authors)}");
                        Console.WriteLine($"ID книги: {book.Id}");
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("Нічого не знайдено.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при виконанні пошуку: {ex.Message}");
            }
        }

        static async Task<List<BookInfo>> SearchBooks(string searchText)
        {
            List<BookInfo> searchResults = new List<BookInfo>();

            try
            {
                string url = $"{GutenbergUrl}/ebooks/search/?query={Uri.EscapeUriString(searchText)}";
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string html = await response.Content.ReadAsStringAsync();
                    searchResults = ParseSearchResults(html);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при виконанні запиту до Gutenberg.org: {ex.Message}");
            }

            return searchResults;
        }

        static List<BookInfo> ParseSearchResults(string html)
        {
            List<BookInfo> searchResults = new List<BookInfo>();

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
                            List<string> authors = GetAuthors(resultsHtml, endIndex);

                            searchResults.Add(new BookInfo { Id = id, Title = title, Authors = authors });
                            index = endIndex + "</a>".Length;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при парсингу результатів пошуку: {ex.Message}");
            }

            return searchResults;
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

        static List<string> GetAuthors(string html, int startIndex)
        {
            List<string> authors = new List<string>();
            int index = html.IndexOf("<span class=\"author\">", startIndex);
            if (index != -1)
            {
                index = html.IndexOf(">", index) + 1;
                int endIndex = html.IndexOf("</span>", index);
                string authorsStr = html.Substring(index, endIndex - index);
                authors.AddRange(authorsStr.Split(','));
            }
            return authors;
        }
    }

    public class BookInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public List<string> Authors { get; set; }
    }
}
