//string url = "https://bunkr.cr/a/tYhcuHNh"
using BunkrDownloader.Classes;

class Program
{
    static async Task Main(string[] args)
    {
        var config = Config.Interactive();
        var httpClient = HttpClientFactory.Create();
        var bunkr = new BunkrService(httpClient);
        var processor = new AlbumProcessor(httpClient, bunkr, config);

        Directory.CreateDirectory(config.DownloadPath);
        foreach (var target in config.Targets)
        {
            try { await processor.ProcessAlbum(target); }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
        }

        Console.WriteLine("All done.");
    }
}



