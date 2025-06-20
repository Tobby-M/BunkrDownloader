using Spectre.Console;

namespace BunkrDownloader.Classes
{

    class CliOptions
    {
        public string Url { get; private set; }
        public string ListFile { get; private set; }
        public string DownloadPath { get; private set; }
        public int Retries { get; private set; }
        public int ConcurrentDownloads { get; private set; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Url) || !string.IsNullOrWhiteSpace(ListFile);

        public static CliOptions Prompt()
        {
            var options = new CliOptions();

            AnsiConsole.Write(new Markup("[bold green]Bunkr Downloader Configuration[/]\n\n"));

            options.Url = AnsiConsole.Ask<string>(
                "[green]Enter album URL[/] [grey](leave empty to use a file)[/]:", "");

            if (string.IsNullOrWhiteSpace(options.Url))
            {
                options.ListFile = AnsiConsole.Ask<string>(
                    "[green]Enter path to file with album links[/]:");
            }

            options.DownloadPath = AnsiConsole.Ask(
                "[green]Enter download path[/] [grey](default: downloads)[/]:", "downloads");

            options.Retries = AnsiConsole.Ask(
                "[green]Enter retry count[/] [grey](default: 10)[/]:", 10);

            options.ConcurrentDownloads = AnsiConsole.Ask(
                "[green]Number of concurrent downloads[/] [grey](default: 2)[/]:", 2);

            return options;
        }
    }

}
