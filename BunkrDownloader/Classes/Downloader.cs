using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BunkrDownloader.Classes
{
    class Downloader
    {
        private readonly HttpClient client;
        private readonly CliOptions options;

        public Downloader(HttpClient client, CliOptions options)
        {
            this.client = client;
            this.options = options;

            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT; Win64; x64)");
            client.DefaultRequestHeaders.Referrer = new Uri("https://bunkr.sk/");
        }

        public async Task RunAsync()
        {
            var targets = new List<string>();

            if (!string.IsNullOrEmpty(options.ListFile))
                targets.AddRange(File.ReadAllLines(options.ListFile));
            else if (!string.IsNullOrEmpty(options.Url))
                targets.Add(options.Url);

            Directory.CreateDirectory(options.DownloadPath);

            foreach (var url in targets)
            {
                try
                {
                    var processor = new AlbumProcessor(client, options);
                    await processor.ProcessAsync(url , options.ConcurrentDownloads);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}
