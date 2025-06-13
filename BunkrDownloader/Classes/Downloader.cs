using ShellProgressBar;
using System.Diagnostics;

namespace BunkrDownloader.Classes
{
    class Downloader
    {
        readonly HttpClient _client;
        readonly int _retries;

        public Downloader(HttpClient client, int retries) { _client = client; _retries = retries; }

        public async Task DownloadWithProgress(string url, string name, string folder, IProgressBar bar)
        {
            var uri = new Uri(url);
            var file = name ?? Path.GetFileName(uri.LocalPath);
            var path = Path.Combine(folder, file);

            for (int attempt = 1; attempt <= _retries; attempt++)
            {
                try
                {
                    using var resp = await _client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                    resp.EnsureSuccessStatusCode();

                    var total = resp.Content.Headers.ContentLength ?? -1L;
                    long downloaded = 0;
                    var sw = Stopwatch.StartNew();
                    var buffer = new byte[8192];

                    using var stream = await resp.Content.ReadAsStreamAsync();
                    using var fs = File.Create(path);

                    int read;
                    while ((read = await stream.ReadAsync(buffer)) > 0)
                    {
                        await fs.WriteAsync(buffer, 0, read);
                        downloaded += read;
                        if (total > 0)
                        {
                            int pct = (int)(downloaded * 100 / total);
                            double mb = downloaded / 1024.0 / 1024.0;
                            double totMb = total / 1024.0 / 1024.0;
                            double spd = mb / sw.Elapsed.TotalSeconds;
                            bar.Tick(pct, $"{file} {mb:F2}/{totMb:F2} MB ({spd:F2} MB/s)");
                        }
                    }
                    sw.Stop();
                    return;
                }
                catch when (attempt < _retries)
                {
                    await Task.Delay(2000);
                }
            }
            throw new Exception("Max retries reached");
        }
    }
}
