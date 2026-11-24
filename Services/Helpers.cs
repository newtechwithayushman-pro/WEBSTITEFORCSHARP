using System.Security.Cryptography;
using System.Text;

namespace CollegeEventManagement.Services
{
    // Creates the secret carried by each student's QR pass.
    public static class TokenService
    {
        public const string Prefix = "CAMPUSLY:";
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";   // Base32: no confusing 0/1/8/9

        // 12 random bytes -> 20 Base32 characters (about 96 bits of randomness)
        public static string NewToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(12);
            var sb = new StringBuilder();
            int buffer = 0, bits = 0;
            foreach (var b in bytes)
            {
                buffer = (buffer << 8) | b; bits += 8;
                while (bits >= 5) { sb.Append(Alphabet[(buffer >> (bits - 5)) & 31]); bits -= 5; }
            }
            if (bits > 0) sb.Append(Alphabet[(buffer << (5 - bits)) & 31]);
            return sb.ToString();
        }

        // Accepts a scanned QR payload or a hand-typed code ("CAMPUSLY:ABCDE...", "abcde-fghij-...")
        public static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var s = input.Trim().ToUpperInvariant();
            if (s.StartsWith(Prefix)) s = s.Substring(Prefix.Length);
            return new string(s.Where(char.IsLetterOrDigit).ToArray());
        }

        // "ABCDEFGHIJKLMNOPQRST" -> "ABCDE-FGHIJ-KLMNO-PQRST"
        public static string Pretty(string token)
        {
            if (string.IsNullOrEmpty(token)) return "";
            return string.Join("-", Enumerable.Range(0, (token.Length + 4) / 5).Select(i => token.Substring(i * 5, Math.Min(5, token.Length - i * 5))));
        }
    }

    public static class Csv
    {
        // Quote every cell; a leading = + - @ is neutralised so Excel never runs it as a formula
        public static string Cell(string s)
        {
            s ??= "";
            if (s.Length > 0 && "=+-@".Contains(s[0])) s = "'" + s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        public static byte[] ToBytes(IEnumerable<IEnumerable<string>> rows)
        {
            var sb = new StringBuilder();
            foreach (var row in rows) sb.AppendLine(string.Join(",", row.Select(Cell)));
            return new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();   // BOM so Excel opens UTF-8
        }
    }
}
