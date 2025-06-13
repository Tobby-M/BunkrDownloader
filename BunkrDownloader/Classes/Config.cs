namespace BunkrDownloader.Classes
{
    class Config
    {
        public List<string> Targets { get; } = new();
        public string DownloadPath { get; private set; } = "downloads";
        public string Extensions { get; private set; }
        public int Retries { get; private set; } = 10;
        public bool ExportOnly { get; private set; }
        public int MaxConcurrent { get; private set; } = 2;

        public static Config Interactive()
        {
            var cfg = new Config();
            Console.Write("Enter a URL or path to a file of URLs: ");
            var input = Console.ReadLine().Trim();
            if (File.Exists(input))
                cfg.Targets.AddRange(File.ReadAllLines(input).Where(l => !string.IsNullOrWhiteSpace(l)));
            else
                cfg.Targets.Add(input);

            Console.Write($"Download path [{cfg.DownloadPath}]: ");
            var dp = Console.ReadLine().Trim();
            if (!string.IsNullOrEmpty(dp)) cfg.DownloadPath = dp;

            //Console.Write("Extensions to download (comma-separated, leave blank for all): ");
            //var ext = Console.ReadLine().Trim();
            //if (!string.IsNullOrEmpty(ext)) cfg.Extensions = ext;

            Console.Write($"Retries [{cfg.Retries}]: ");
            var r = Console.ReadLine().Trim();
            if (int.TryParse(r, out var ri)) cfg.Retries = ri;

            Console.Write($"Max concurrent downloads [{cfg.MaxConcurrent}]: ");
            var m = Console.ReadLine().Trim();
            if (int.TryParse(m, out var mc)) cfg.MaxConcurrent = mc;

            //Console.Write("Export URL list only? (y/N): ");
            //var y = Console.ReadLine().Trim().ToLower();
            //cfg.ExportOnly = y == "y" || y == "yes";

            return cfg;
        }
    }
}
