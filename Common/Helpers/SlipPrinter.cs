
using System;
using System.Drawing;
using System.Drawing.Printing;

namespace Common.Helpers
{
    public class SlipPrinter : ISlipPrinter
    {
        private string _slipContent;

        public void PrintSlip(string slipContent)
        {
            _slipContent = slipContent;

            // uncomment the following code to enable actual printing
            PrintDocument printDoc = new PrintDocument();
            printDoc.PrintPage += PrintPageHandler;

            // Optional: set printer name
            // printDoc.PrinterSettings.PrinterName = "YourPrinterName";

            // uncomment the following code to enable actual printing
            printDoc.Print();
        }

        private void PrintPageHandler(object sender, PrintPageEventArgs e)
        {
            Font font = new Font("Consolas", 10);
            float lineHeight = font.GetHeight(e.Graphics);
            float x = e.MarginBounds.Left;
            float y = e.MarginBounds.Top;

            foreach (string line in _slipContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                e.Graphics.DrawString(line, font, Brushes.Black, x, y);
                y += lineHeight;
            }
        }
    }

}
