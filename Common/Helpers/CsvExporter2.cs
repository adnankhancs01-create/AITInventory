using System.Globalization;
using System.Reflection;
using System.Text;

namespace Common.Helpers
{
    /// <summary>Pure .NET CSV export (no MAUI types), so it also compiles in the Common project.</summary>
    public static class CsvExporter2
    {
        /// <summary>Builds CSV text from the public readable scalar properties of T.</summary>
        public static string ToCsv<T>(IEnumerable<T> rows)
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && IsScalar(p.PropertyType))
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", props.Select(p => Escape(p.Name))));

            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(",", props.Select(p => Escape(Format(p.GetValue(row))))));
            }

            return sb.ToString();
        }

        /// <summary>Writes the CSV to Documents\AITInventoryExports and returns the full path.</summary>
        public static string SaveToExportFolder(string fileNamePrefix, string csv)
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "AITInventoryExports");
            Directory.CreateDirectory(folder);

            var path = Path.Combine(folder, $"{fileNamePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            // UTF-8 with BOM so Excel opens non-English text correctly.
            File.WriteAllText(path, csv, new UTF8Encoding(true));
            return path;
        }

        private static bool IsScalar(Type type)
        {
            var t = Nullable.GetUnderlyingType(type) ?? type;
            return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime);
        }

        private static string Format(object? value)
        {
            return value switch
            {
                null => string.Empty,
                DateTime d => d.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                decimal m => m.ToString("0.####", CultureInfo.InvariantCulture),
                double d => d.ToString("0.####", CultureInfo.InvariantCulture),
                float f => f.ToString("0.####", CultureInfo.InvariantCulture),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty
            };
        }

        private static string Escape(string value)
        {
            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }
}
