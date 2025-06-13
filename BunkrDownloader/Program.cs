//string url = "https://bunkr.cr/a/tYhcuHNh"
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using HtmlAgilityPack;
using ShellProgressBar;

class Program
{
    const string BUNKR_API_URL = "https://bunkr.cr/api/vs";
    const string SECRET_KEY_BASE = "SECRET_KEY_";

    static readonly HttpClient client = new HttpClient(new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
    });

    static async Task Main(string[] args)
    {
        // Install via: dotnet add package ShellProgressBar

        string url = "https://bunkr.cr/a/tYhcuHNh", listFile = null, downloadPath = "downloads", extensions = null;
        int retries = 10;
        bool exportOnly = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-u": url = args[++i]; break;
                case "-f": listFile = args[++i]; break;
                case "-p": downloadPath = args[++i]; break;
                case "-r": retries = int.Parse(args[++i]); break;
                case "-e": extensions = args[++i]; break;
                case "-w": exportOnly = true; break;
            }
        }

        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT; Win64; x64)");
        client.DefaultRequestHeaders.Referrer = new Uri("https://bunkr.sk/");

        var targets = new List<string>();
        if (!string.IsNullOrEmpty(listFile)) targets.AddRange(File.ReadAllLines(listFile));
        else if (!string.IsNullOrEmpty(url)) targets.Add(url);
        else { Console.WriteLine("Provide -u or -f"); return; }

        Directory.CreateDirectory(downloadPath);

        foreach (var target in targets)
        {
            try { await ProcessAlbum(target, downloadPath, retries, extensions, exportOnly); }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
        }

        Console.WriteLine("All done.");
    }

    static async Task ProcessAlbum(string pageUrl, string basePath, int retries, string extensions, bool onlyExport)
    {
        var html = await client.GetStringAsync(pageUrl);
        var doc = new HtmlDocument(); doc.LoadHtml(html);

        bool isBunkr = doc.DocumentNode.SelectSingleNode("//title").InnerText.Contains("| Bunkr");
        bool direct = doc.DocumentNode.SelectNodes(isBunkr
            ? "//span[contains(@class,'ic-videos')] | //div[contains(@class,'lightgallery')]"
            : "") != null;

        var albumName = doc.DocumentNode.SelectSingleNode("//h1[contains(@class,'truncate')]")?.InnerText
                        ?? doc.DocumentNode.SelectSingleNode("//h1[@id='title']").InnerText;
        albumName = CleanName(albumName);

        var downloadFolder = Path.Combine(basePath, albumName);
        Directory.CreateDirectory(downloadFolder);

        var alreadyPath = Path.Combine(downloadFolder, "already_downloaded.txt");
        var already = File.Exists(alreadyPath)
            ? File.ReadAllLines(alreadyPath).ToHashSet()
            : new HashSet<string>();

        var nodes = doc.DocumentNode.SelectNodes(isBunkr
            ? "//a[contains(@class,'after:absolute')]"
            : "//a[contains(@class,'image')]") ?? new HtmlNodeCollection(null);

        var items = new List<string>();
        if (direct && isBunkr)
            items.Add(pageUrl);
        else
            items.AddRange(nodes.Select(a => NormalizeUrl(a.GetAttributeValue("href", ""), isBunkr)));

        if (onlyExport)
        {
            foreach (var itemUrl in items)
                File.AppendAllText(Path.Combine(downloadFolder, "url_list.txt"), itemUrl + Environment.NewLine);
            Console.WriteLine($"URL list exported for {albumName}");
            return;
        }

        using var rootBar = new ProgressBar(items.Count, albumName);
        var dlSemaphore = new SemaphoreSlim(2);
        var dlTasks = new List<Task>();

        foreach (var itemUrl in items)
        {
            await dlSemaphore.WaitAsync();
            dlTasks.Add(Task.Run(async () =>
            {
                try
                {
                    var record = await GetRealUrl(itemUrl, isBunkr);
                    if (record.url == null) return;

                    var ext = Path.GetExtension(new Uri(record.url).AbsolutePath).TrimStart('.');
                    if (!string.IsNullOrEmpty(extensions) && !extensions.Split(',').Contains(ext)) return;
                    if (already.Contains(record.url)) return;

                    using IProgressBar child = rootBar.Spawn(100, Path.GetFileName(record.url));

                    await DownloadWithProgress(record.url, record.name, downloadFolder, child, retries);
                    File.AppendAllText(alreadyPath, record.url + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Download error: {ex.Message}");
                }
                finally { dlSemaphore.Release(); rootBar.Tick(); }
            }));
        }

        await Task.WhenAll(dlTasks);
        Console.WriteLine($"Finished {albumName}");
    }

    static async Task DownloadWithProgress(string url, string name, string folder, IProgressBar bar, int retries)
    {
        var uri = new Uri(url);
        var filename = name ?? Path.GetFileName(uri.LocalPath);
        var path = Path.Combine(folder, filename);

        for (int attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                using var resp = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                resp.EnsureSuccessStatusCode();

                var total = resp.Content.Headers.ContentLength ?? -1L;
                var buffer = new byte[8192];
                long downloaded = 0;
                var sw = Stopwatch.StartNew();

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
                        double totalMb = total / 1024.0 / 1024.0;
                        double speed = mb / sw.Elapsed.TotalSeconds;
                        bar.Tick(pct, $"{filename} {mb:F2}/{totalMb:F2} MB ({speed:F2} MB/s)");
                    }
                }
                sw.Stop();
                return;
            }
            catch when (attempt < retries)
            {
                await Task.Delay(2000);
            }
        }
        throw new Exception("Max retries reached");
    }

    static string NormalizeUrl(string href, bool isBunkr)
        => href.StartsWith("http") ? href : isBunkr ? $"https://bunkr.sk{href}" : href.Replace("/f/", "/api/f/");

    static async Task<(string url, string name)> GetRealUrl(string url, bool isBunkr)
    {
        if (isBunkr)
        {
            var m = Regex.Match(url, "/f/(.*?)$");
            if (!m.Success) return (null, null);
            var slug = m.Groups[1].Value;

            using var resp = await client.PostAsync(BUNKR_API_URL,
                new StringContent(JsonSerializer.Serialize(new { slug }), System.Text.Encoding.UTF8, "application/json"));
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(await resp.Content.ReadAsStringAsync());

            var ts = data["timestamp"].GetInt64();
            var enc = data["url"].GetString();
            return (Decrypt(enc, ts), null);
        }
        using var resp2 = await client.GetAsync(url);
        var obj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(await resp2.Content.ReadAsStringAsync());
        return (obj["url"].GetString(), obj.ContainsKey("name") ? obj["name"].GetString() : null);
    }

    static string Decrypt(string encUrl, long timestamp)
    {
        var key = SECRET_KEY_BASE + (timestamp / 3600);
        var encBytes = Convert.FromBase64String(encUrl);
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
        for (var i = 0; i < encBytes.Length; i++) encBytes[i] ^= keyBytes[i % keyBytes.Length];
        return System.Text.Encoding.UTF8.GetString(encBytes);
    }

    static string CleanName(string s) => Regex.Replace(s, @"[<>:""/\\|?*]", "-").Trim();
}



