using HtmlAgilityPack;
using ShellProgressBar;

namespace BunkrDownloader.Classes
{
    class AlbumProcessor
    {
        readonly HttpClient _client;
        readonly BunkrService _bunkr;
        readonly Config _config;

        public AlbumProcessor(HttpClient client, BunkrService bunkr, Config config)
        {
            _client = client;
            _bunkr = bunkr;
            _config = config;
        }

        public async Task ProcessAlbum(string pageUrl)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(await _client.GetStringAsync(pageUrl));

            bool isBunkr = doc.DocumentNode.SelectSingleNode("//title").InnerText.Contains("| Bunkr");
            bool direct = doc.DocumentNode.SelectNodes(isBunkr
                ? "//span[contains(@class,'ic-videos')] | //div[contains(@class,'lightgallery')]"
                : "") != null;

            var albumName = Sanitizer.CleanName(
                doc.DocumentNode.SelectSingleNode("//h1[contains(@class,'truncate')]")?.InnerText
                ?? doc.DocumentNode.SelectSingleNode("//h1[@id='title']").InnerText);
            var folder = Path.Combine(_config.DownloadPath, albumName);
            Directory.CreateDirectory(folder);

            var alreadyFile = Path.Combine(folder, "already_downloaded.txt");
            var downloaded = File.Exists(alreadyFile)
                ? File.ReadAllLines(alreadyFile).ToHashSet()
                : new HashSet<string>();

            var hrefs = ExtractLinks(doc, isBunkr, direct, pageUrl);
            if (_config.ExportOnly)
            {
                File.AppendAllLines(Path.Combine(folder, "url_list.txt"), hrefs);
                Console.WriteLine($"URL list exported for {albumName}");
                return;
            }

            using var rootBar = new ProgressBar(hrefs.Count, albumName);
            var sem = new SemaphoreSlim(_config.MaxConcurrent);
            var tasks = hrefs.Select(async href =>
            {
                await sem.WaitAsync();
                try
                {
                    var (url, name) = await _bunkr.GetDownloadInfo(href, isBunkr);
                    if (url == null || downloaded.Contains(url)) return;

                    if (!string.IsNullOrEmpty(_config.Extensions)
                        && !_config.Extensions.Split(',').Contains(Path.GetExtension(url).TrimStart('.')))
                        return;

                    using var child = rootBar.Spawn(100, Path.GetFileName(url));
                    var dl = new Downloader(_client, _config.Retries);
                    await dl.DownloadWithProgress(url, name, folder, child);
                    File.AppendAllText(alreadyFile, url + Environment.NewLine);
                }
                finally { sem.Release(); rootBar.Tick(); }
            });

            await Task.WhenAll(tasks);
            Console.WriteLine($"Finished {albumName}");
        }

        static List<string> ExtractLinks(HtmlDocument doc, bool isBunkr, bool direct, string pageUrl)
        {
            if (direct && isBunkr) return new() { pageUrl };
            var xpath = isBunkr ? "//a[contains(@class,'after:absolute')]" : "//a[contains(@class,'image')]";
            return doc.DocumentNode.SelectNodes(xpath)
                       ?.Select(a => UrlHelper.Normalize(a.GetAttributeValue("href", ""), isBunkr))
                       .ToList()
                   ?? new List<string>();
        }
    }

}
