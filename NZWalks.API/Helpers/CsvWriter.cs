using System.Text;

namespace NZWalks.API.Helpers
{
    public static class CsvWriter
    {
        public static byte[] Write(IEnumerable<string> headers, IEnumerable<IEnumerable<string?>> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers.Select(Escape)));

            foreach (var row in rows)
                sb.AppendLine(string.Join(",", row.Select(Escape)));

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string Escape(string? value)
        {
            value ??= string.Empty;
            return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }
    }
}
