using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;

namespace Data
{
    /// <summary>
    /// Used only by the EF tools (Add-Migration / Update-Database). The MAUI app builds its DbContext at runtime
    /// through dependency injection, which the tools cannot start, so this factory creates it directly.
    /// Connection string: environment variable AIT_CONNECTION if set, otherwise DefaultConnection from
    /// AIT_Inventory_App/appsettings.json (found by searching the parent folders).
    /// </summary>
    public class InventoryDbContextFactory2 : IDesignTimeDbContextFactory<InventoryDbContext>
    {
        public InventoryDbContext CreateDbContext(string[] args)
        {
            string? connection = Environment.GetEnvironmentVariable("AIT_CONNECTION");

            if (string.IsNullOrWhiteSpace(connection))
                connection = ReadFromAppSettings();

            if (string.IsNullOrWhiteSpace(connection))
                throw new InvalidOperationException(
                    "No connection string found. Set the AIT_CONNECTION environment variable or check AIT_Inventory_App/appsettings.json.");

            var options = new DbContextOptionsBuilder<InventoryDbContext>()
                .UseSqlServer(connection)
                .Options;

            return new InventoryDbContext(options);
        }

        private static string? ReadFromAppSettings()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                string path = Path.Combine(dir.FullName, "AIT_Inventory_App", "appsettings.json");
                if (File.Exists(path))
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                    if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs)
                        && cs.TryGetProperty("DefaultConnection", out var value))
                        return value.GetString();
                }

                dir = dir.Parent;
            }

            return null;
        }
    }
}
