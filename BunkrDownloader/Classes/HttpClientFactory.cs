namespace BunkrDownloader.Classes
{
    static class HttpClientFactory
    {
        public static HttpClient Create()
        {
            var client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            });
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT; Win64; x64)");
            client.DefaultRequestHeaders.Referrer = new Uri("https://bunkr.sk/");
            return client;
        }
    }
}
