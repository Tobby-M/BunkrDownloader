
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BunkrDownloader.Classes
{
    class BunkrService
    {
        const string API = "https://bunkr.cr/api/vs";
        const string KEY_BASE = "SECRET_KEY_";
        readonly HttpClient _client;

        public BunkrService(HttpClient client) => _client = client;

        public async Task<(string url, string name)> GetDownloadInfo(string link, bool isBunkr)
        {
            if (isBunkr)
            {
                var slug = Regex.Match(link, "/f/(.*?)$").Groups[1].Value;
                var resp = await _client.PostAsync(API,
                    new StringContent(JsonSerializer.Serialize(new { slug }), System.Text.Encoding.UTF8, "application/json"));
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(await resp.Content.ReadAsStringAsync());
                long ts = data["timestamp"].GetInt64();
                string enc = data["url"].GetString();
                return (Decrypt(enc, ts), null);
            }
            var resp2 = await _client.GetAsync(link);
            var obj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(await resp2.Content.ReadAsStringAsync());
            return (obj["url"].GetString(), obj.TryGetValue("name", out var n) ? n.GetString() : null);
        }

        static string Decrypt(string enc, long ts)
        {
            var key = KEY_BASE + (ts / 3600);
            var data = Convert.FromBase64String(enc);
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            for (int i = 0; i < data.Length; i++) data[i] ^= keyBytes[i % keyBytes.Length];
            return System.Text.Encoding.UTF8.GetString(data);
        }
    }

}
