using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BunkrDownloader.Classes
{
    static class Sanitizer
    {
        public static string CleanName(string s)
            => Regex.Replace(s, @"[<>:""/\\|?*]", "-").Trim();
    }
}
