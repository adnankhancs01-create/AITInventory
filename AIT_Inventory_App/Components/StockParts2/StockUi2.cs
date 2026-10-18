using Common;
using Common.Helpers;
using Radzen;

namespace AIT_Inventory_App.Components.StockParts2
{
    /// <summary>Shared UI helpers for the Stock2 pages (messages and CSV export). Uses only DialogService, which the app already has.</summary>
    public static class StockUi2
    {
        public static Task AlertAsync(DialogService dialog, string title, string message)
        {
            return dialog.Alert(message, title, new AlertOptions { OkButtonText = "OK" });
        }

        /// <summary>Shows the service errors (or the success message). Returns true when the call succeeded.</summary>
        public static async Task<bool> ShowResultAsync<T>(DialogService dialog, BaseResponse<T> result, bool showSuccess = true)
        {
            if (result.Success)
            {
                if (showSuccess && !string.IsNullOrWhiteSpace(result.Message))
                    await AlertAsync(dialog, "Done", result.Message);
                return true;
            }

            string text = result.Errors != null && result.Errors.Count > 0
                ? string.Join("  •  ", result.Errors)
                : (string.IsNullOrWhiteSpace(result.Message) ? "The request failed." : result.Message);

            await AlertAsync(dialog, "Cannot complete", text);
            return false;
        }

        public static async Task ExportAsync<T>(DialogService dialog, string filePrefix, IEnumerable<T> rows)
        {
            try
            {
                var list = rows.ToList();
                if (list.Count == 0)
                {
                    await AlertAsync(dialog, "Export", "There are no rows to export.");
                    return;
                }

                string path = CsvExporter2.SaveToExportFolder(filePrefix, CsvExporter2.ToCsv(list));
                await AlertAsync(dialog, "Export complete", $"{list.Count} rows saved to {path}");
            }
            catch (Exception ex)
            {
                await AlertAsync(dialog, "Export failed", ex.Message);
            }
        }
    }
}
