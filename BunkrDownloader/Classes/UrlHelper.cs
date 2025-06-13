

namespace BunkrDownloader.Classes
{
    static class UrlHelper
    {
        public static string Normalize(string href, bool isBunkr)
            => href.StartsWith("http") ? href
               : isBunkr ? $"https://bunkr.sk{href}" : href.Replace("/f/", "/api/f/");
    }
}
