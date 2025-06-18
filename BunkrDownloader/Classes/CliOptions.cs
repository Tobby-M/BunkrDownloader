namespace BunkrDownloader.Classes
{
    class CliOptions
    {
        public string Url { get; private set; }
        public string ListFile { get; private set; }
        public string DownloadPath { get; private set; } = "downloads";
        public int Retries { get; private set; } = 10;
        public int ConcurrentDownloads { get; private set; } = 2;

        public bool IsValid => !string.IsNullOrEmpty(Url) || !string.IsNullOrEmpty(ListFile);

        public static CliOptions Prompt()
        {
            var options = new CliOptions();

            Console.Write("Enter album URL (or leave blank to provide file path): ");
            options.Url = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(options.Url))
            {
                Console.Write("Enter path to file with album links: ");
                options.ListFile = Console.ReadLine()?.Trim();
            }

            Console.Write("Enter download path [default: downloads]: ");
            var path = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(path))
                options.DownloadPath = path;

            Console.Write("Enter retry count [default: 10]: ");
            if (int.TryParse(Console.ReadLine(), out int retries))
                options.Retries = retries;

            Console.Write("Number of concurrent downloads [default: 2]: ");
            if (int.TryParse(Console.ReadLine(), out int concurrent))
                options.ConcurrentDownloads = concurrent;

            return options;
        }
    }
}
