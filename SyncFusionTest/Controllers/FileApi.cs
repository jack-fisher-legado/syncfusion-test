using Microsoft.AspNetCore.Mvc;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Parsing;

namespace SyncFusionTest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileApi : ControllerBase
    {
        [HttpGet("Get")]
        public IActionResult Get()
        {
            return Ok(new { message = "Success" });
        }

        [HttpPost("Post")]
        public async Task<IActionResult> Post([FromBody] FileData model)
        {

            // HAPPY PATH?:
            // 1. We verify document is not password protected
            // 2. We add graphic(s) to the document for each signature provided
            // 3. We add a summary page to the end of the document containing signing information
            // 4. We hash the document using SHA256 and store in DB
            // 5. We store the document in Azure
            // 6. We schedule a notification event "document signed"
            // 7. We return a success message to the client

            // Unknowns:
            // 1. How does the certificate work? What do we have to do with it to validate?
            // 2. What is the maximum file size we accept?
            // 3. What format will signature graphics be sent in? Base64 encoded? Or as file upload/form-data?

            // Note: swap out file names passed to filePath generation to test different scenarios
            var basicFileName = "sample.pdf";
            var protectedFileName = "sample-protected.pdf";
            var readOnlyFileName = "sample-read-only.pdf";
            var multiPageFileName = "sample-multi-page.pdf";

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs", multiPageFileName);

            var pdfData = System.IO.File.ReadAllBytes(filePath);
            PdfLoadedDocument loadedDocument = LoadDocument(filePath);

            foreach(var signature in model.Signatures)
            {
                var x = signature.Coordinates.X;
                var y = signature.Coordinates.Y;

                var base64image = signature.Base64Graphic;
                PaintGraphicOnDocument(loadedDocument, base64image, x, y);
            }

            AddSummaryPageToEndOfDocument(loadedDocument, model.Signatures);

            #region local functions 

            PdfLoadedDocument LoadDocument(string filePath)
            {
                try
                {
                    var pdfData = System.IO.File.ReadAllBytes(filePath);

                    //Load the existing PDF document
                    PdfLoadedDocument loadedDocument = new(pdfData);

                    // UserPassword should always be empty as loading above without password should throw PdfInvalidPasswordException
                    if (string.IsNullOrWhiteSpace(loadedDocument.Security.UserPassword))
                    {
                        return loadedDocument;
                    }

                    // handle this exception however we see fit...
                    throw new Exception("Password was not empty on document");

                }
                catch (PdfInvalidPasswordException ex)
                {
                    // this is a password protected document
                    // handle this exception however we see fit...
                    throw;
                }
            }

            void PaintGraphicOnDocumentPage(PdfLoadedDocument document, int pageNumber, string imagePath, float x, float y, float width, float height)
            {
                var loadedPage = loadedDocument.Pages[pageNumber] as PdfLoadedPage;

                using (FileStream imageStream = new(imagePath, FileMode.Open, FileAccess.Read))
                {
                    PdfBitmap image = new(imageStream);
                    // set the height and width of the image to 50 and place at coordiantes 100, 50
                    loadedPage.Graphics.DrawImage(image, x, y, width, height);

                }
            }

            void PaintGraphicOnDocument(PdfLoadedDocument document, string base64image, float x, float y)
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

                    byte[] imageBytes = Convert.FromBase64String(base64image);
                    using (MemoryStream imageStream = new MemoryStream(imageBytes))
                    {
                        PdfBitmap image = new (imageStream);
                        targetPage.Graphics.DrawImage(image, x, pageSpecificY);
                    }
                }
            }

            void AddSummaryPageToEndOfDocument(PdfLoadedDocument document, Signature[] signatures)
            {
                PdfPageBase page = document.Pages.Add();
                PdfGraphics graphics = page.Graphics;

                PdfPen pen = new PdfPen(PdfBrushes.Black, 1);
                graphics.DrawRectangle(pen, new RectangleF(2, 2, page.Size.Width, page.Size.Height));

                PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 16);
                graphics.DrawString("Summary Page", font, PdfBrushes.Black, new PointF(10, 10));

                int yStartingPoint = 0;

                foreach(var signature in signatures)
                {
                    graphics.DrawString($"IP Address: {signature.IpAddress}", font, PdfBrushes.Black, new PointF(10, yStartingPoint + 40));

                    graphics.DrawString($"Signed By: {signature.SignerId}", font, PdfBrushes.Black, new PointF(10, yStartingPoint + 80));

                    graphics.DrawString("Signature:", font, PdfBrushes.Black, new PointF(10, yStartingPoint + 120));

                    byte[] imageBytes = Convert.FromBase64String(signature.Base64Graphic);
                    using (MemoryStream imageStream = new MemoryStream(imageBytes))
                    {
                        PdfBitmap image = new(imageStream);
                        graphics.DrawImage(image, 10, yStartingPoint + 140);
                    }

                    yStartingPoint = yStartingPoint + 200;
                }
            }

            void DrawTextOnDocumentPage(PdfLoadedDocument document, int pageNumber, string text, float x, float y)
            {
                PdfLoadedPage? loadedPage = loadedDocument.Pages[pageNumber] as PdfLoadedPage;
                PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 16);
                loadedPage.Graphics.DrawString(text, font, PdfBrushes.Black, new PointF(x, y));
            }

            #endregion

            return Ok(new { message = "File processed successfully" });
        }
    }

    public class FileData
    {
        public int FileId { get; set; }
        public required Signature[] Signatures { get; set; }
        public required string Certificate { get; set; }
    }

    public class Signature
    {
        public required string Base64Graphic { get; set; }
        // Potentially a page number in here too, to be decided...
        public required Coordinate Coordinates { get; set; }
        public int SignerId { get; set; }
        public required string IpAddress { get; set; }
    }

    public class Coordinate
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
