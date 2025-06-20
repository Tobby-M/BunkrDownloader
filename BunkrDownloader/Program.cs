using BunkrDownloader.Classes;

class Program
{
    static readonly HttpClient client = new HttpClient(new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
    });

    static async Task Main(string[] args)
    {
        var config = CliOptions.Prompt();
        var downloader = new Downloader(client, config);
        await downloader.RunAsync();
        Console.WriteLine("All done.");
        Console.ReadLine();
    }
}



