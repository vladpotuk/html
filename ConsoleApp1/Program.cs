using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        await DownloadAndDisplayHamlet();
    }

    static async Task DownloadAndDisplayHamlet()
    {
        string url = "https://www.gutenberg.org/cache/epub/2265/pg2265.txt"; 

        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode(); 

                string content = await response.Content.ReadAsStringAsync(); 

               
                Console.WriteLine("Гамлет Вільяма Шекспіра:");
                Console.WriteLine(content);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Помилка під час запиту: {e.Message}");
            }
        }
    }
}
