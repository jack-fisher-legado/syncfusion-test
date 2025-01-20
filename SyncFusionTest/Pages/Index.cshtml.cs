using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Interactive;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf;
using Syncfusion.Drawing;
using System.Reflection.Metadata;

namespace SyncFusionTest.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            // swap out file names passed to filePath generation to test different scenarios
            var basicFileName = "sample.pdf";
            var protectedFileName = "sample-protected.pdf";
            var readOnlyFileName = "sample-read-only.pdf";
            var multiPageFileName = "sample-multi-page.pdf";

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs", multiPageFileName);

            var pdfData = System.IO.File.ReadAllBytes(filePath);
            PdfLoadedDocument? loadedDocument = LoadDocument(filePath);

            var x = 100;
            var y = 1000;
            var width = 50;
            var height = 50;
            var pageNumber = 0;

            string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "example.jpg");
            
            PaintGraphicOnDocument(loadedDocument, imagePath, x, y, width, height);

            AddSummaryPageToEndOfDocument(loadedDocument, imagePath);

            return CreateFileToReturn();

            #region local functions 

            PdfLoadedDocument? LoadDocument(string filePath)
            {
                try
                {
                    var pdfData = System.IO.File.ReadAllBytes(filePath);

                    //Load the existing PDF document
                    PdfLoadedDocument loadedDocument = new(pdfData);

                    // UserPassword should always be empty as loading above without password should throw PdfInvalidPasswordException
                    // However, suggestion here is that is not always the case - https://www.syncfusion.com/forums/103951/how-to-determine-if-pdf-is-password-protected?reply=l70EgH
                    return string.IsNullOrWhiteSpace(loadedDocument.Security.UserPassword) ? loadedDocument : null;

                }
                catch(PdfInvalidPasswordException ex)
                {
                    // this is a password protected document
                    return null;
                }
            }

            void PaintGraphicOnDocumentPage(PdfLoadedDocument document, int pageNumber, string imagePath, float x, float y, float width, float height)
            {
                PdfLoadedPage? loadedPage = loadedDocument.Pages[pageNumber] as PdfLoadedPage;

                using (FileStream imageStream = new(imagePath, FileMode.Open, FileAccess.Read))
                {
                    PdfBitmap image = new(imageStream);
                    // set the height and width of the image to 50 and place at coordiantes 100, 50
                    loadedPage.Graphics.DrawImage(image, x, y, width, height);

                }
            }

            void PaintGraphicOnDocument(PdfLoadedDocument document, string imagePath, float x, float y, float width, float height)
            {
                // First determine what page the graphic is to be painted on
                float cumulativeHeight = 0;
                int targetPageIndex = -1;
                float pageSpecificY = y;

                for (int i = 0; i < loadedDocument.Pages.Count; i++)
                {
                    PdfPageBase page = loadedDocument.Pages[i];
                    float pageHeight = page.Size.Height;

                    if (pageSpecificY <= pageHeight + cumulativeHeight)
                    {
                        targetPageIndex = i;
                        pageSpecificY = pageSpecificY - cumulativeHeight;
                        break;
                    }

                    cumulativeHeight += pageHeight;
                }

                // if we have successfully determined a page based on global coordinates, we can append the graphic here
                if (targetPageIndex != -1)
                {
                    PdfLoadedPage targetPage = loadedDocument.Pages[targetPageIndex] as PdfLoadedPage;
                    PdfGraphics graphics = targetPage.Graphics;

                    using (FileStream imageStream = new(imagePath, FileMode.Open, FileAccess.Read))
                    {
                        PdfBitmap image = new(imageStream);
                        targetPage.Graphics.DrawImage(image, x, pageSpecificY, width, height);
                    }
                }
            }

            void AddSummaryPageToEndOfDocument(PdfLoadedDocument document, string imagePath)
            {
                PdfPageBase page = document.Pages.Add();
                PdfGraphics graphics = page.Graphics;
                
                PdfPen pen = new PdfPen(PdfBrushes.Black, 1);
                graphics.DrawRectangle(pen, new RectangleF(2, 2, page.Size.Width, page.Size.Height));

                PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 16);
                graphics.DrawString("Summary Page", font, PdfBrushes.Black, new PointF(10, 10));

                graphics.DrawString("IP Address: 89.107.58.127", font, PdfBrushes.Black, new PointF(10, 40));

                graphics.DrawString("Signed By: Jack Fisher", font, PdfBrushes.Black, new PointF(10, 80));

                graphics.DrawString("Signature:", font, PdfBrushes.Black, new PointF(10, 120));

                using (FileStream imageStream = new(imagePath, FileMode.Open, FileAccess.Read))
                {
                    PdfBitmap image = new(imageStream);
                    graphics.DrawImage(image, 10, 140, width, height);
                }
            }

            void DrawTextOnDocumentPage(PdfLoadedDocument document, int pageNumber, string text, float x, float y)
            {
                PdfLoadedPage? loadedPage = loadedDocument.Pages[pageNumber] as PdfLoadedPage;
                PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 16);
                loadedPage.Graphics.DrawString(text, font, PdfBrushes.Black, new PointF(x, y));
            }

            FileStreamResult CreateFileToReturn()
            {
                //Creating the stream object.
                MemoryStream stream = new();
                //Save the document as stream.
                loadedDocument.Save(stream);
                stream.Position = 0;

                // Return the document as a downloadable file
                return File(stream, "application/pdf", "Completed Document.pdf");
            }

            #endregion

        }
    }
}
