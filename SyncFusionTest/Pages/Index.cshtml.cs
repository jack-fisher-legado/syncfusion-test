using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Interactive;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf;
using Syncfusion.Drawing;
using System.Reflection.Metadata;
using Syncfusion.Pdf.Redaction;
using Microsoft.CodeAnalysis.Text;
using FontStyle = Syncfusion.Drawing.FontStyle;
using RectangleF = Syncfusion.Drawing.RectangleF;
using System;

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
            var templateFileName = "sample-template.pdf";

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs", templateFileName);

            var pdfData = System.IO.File.ReadAllBytes(filePath);
            PdfLoadedDocument? loadedDocument = LoadDocument(filePath);

            var x = 0;
            var y = 0;
            var width = 50;
            var height = 50;
            var pageNumber = 0;

            string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "example.jpg");

            FillTemplatesWithData(loadedDocument, "{{addressLine1}}", "43 Argyle House");
            FillTemplatesWithData(loadedDocument, "{{addressLine2}}", "Codebase");
            FillTemplatesWithData(loadedDocument, "{{addressCity}}", "Edinburgh");
            FillTemplatesWithData(loadedDocument, "{{addressPostCode}}", "EH1 1AA");
            FillTemplatesWithData(loadedDocument, "{{firstName}}", "Jackthisisasuperlongnameandwhoknowsjusthowlongitwillbereally");
            
           // PaintGraphicOnDocument(loadedDocument, imagePath, x, y, width, height);

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

            PdfLoadedDocument FillTemplatesWithData(PdfLoadedDocument loadedDocument, string replace, string with)
            {
                Dictionary<int, List<RectangleF>> matchedTextbounds = new Dictionary<int, List<RectangleF>>();

                loadedDocument.FindText(replace, out matchedTextbounds);

                // Loop through each instance of the matched text we are replacing
                // we are removing any sentences that are empty strings with this Where clause
                foreach (KeyValuePair<int, List<RectangleF>> matchedText in matchedTextbounds.Where(x => x.Value.Count > 0))
                {
                    int pageIndex = matchedText.Key; 

                    List<RectangleF> listOfBoxesContainingMatchedText = matchedText.Value;
                   

                    PdfLoadedPage pageReplaceTextIsOn = loadedDocument.Pages[pageIndex] as PdfLoadedPage;
                    float pageWidth = pageReplaceTextIsOn.Size.Width;
                    float pageHeight = pageReplaceTextIsOn.Size.Height;

                    pageReplaceTextIsOn.ExtractText(out Syncfusion.Pdf.TextLineCollection textLineCollection);

                    var bounds = textLineCollection.TextLine[0].Bounds;

                    // if we want to support custom fonts, we need to have a path to them available...
                    //PdfFont pdfFont = new PdfTrueTypeFont(@"../../ARIALUNI.ttf", 24);

                    // Now, you can work with the page as needed.
                    for (int j = 0; j < listOfBoxesContainingMatchedText.Count; j++)
                    {
                        var thisTextsBox = listOfBoxesContainingMatchedText[j];
                        var matchedLine = textLineCollection.TextLine.FirstOrDefault(t => t.Text.Contains(replace));

                        if (matchedLine != null)
                        {
                            var fontSize = matchedLine.FontSize;
                            var fontName = matchedLine.FontName;
                            var fontStyle = matchedLine.FontStyle;

                            var font = GetPdfFont(fontName, fontSize, fontStyle);


                            // Detect text alignment based on bounding box
                            PdfStringFormat format = new PdfStringFormat();
                            if (Math.Abs(thisTextsBox.X) < 5)
                                format.Alignment = PdfTextAlignment.Left;  // Left-aligned
                            else if (Math.Abs(thisTextsBox.X + thisTextsBox.Width - pageWidth) < 5)
                                format.Alignment = PdfTextAlignment.Right; // Right-aligned
                            else if (Math.Abs(thisTextsBox.X - (pageWidth - thisTextsBox.Width) / 2) < 5)
                                format.Alignment = PdfTextAlignment.Center; // Centre-aligned

                            //format.Alignment = PdfTextAlignment.Right;

                            loadedDocument.FindText(matchedLine.Text, out var thisLineOfTextsBounds);

                            var newLineText = matchedLine.Text.Replace(replace, with);

                            // Apply redaction with correct font
                            PdfRedaction redaction = new PdfRedaction(thisLineOfTextsBounds.First().Value[0], Color.Transparent);

                            // TODO - accurately line up the Y axis for the replacement string with the Y axis of the rest of the sentence (regardless of font, size etc.)
                            redaction.Appearance.Graphics.DrawString(newLineText, font, PdfBrushes.Black, new PointF() { X = 0, Y = 0 }, format);

                            //redaction.Appearance.Graphics.DrawString(with, font, PdfBrushes.Black, new RectangleF(0, 0, pageWidth, pageHeight), format);

                            pageReplaceTextIsOn.AddRedaction(redaction);
                        }
                    }
                }

                //Apply redaction.
                loadedDocument.Redact();
                return loadedDocument;
            }

            PdfFont GetPdfFont(string fontName, float fontSize, FontStyle style)
            {
                PdfFontFamily pdfFontFamily = PdfFontFamily.Helvetica; // Default

                // Map common font names to Syncfusion's standard PDF fonts
                switch (fontName.ToLower())
                {
                    case "arial":
                    case "helvetica":
                        pdfFontFamily = PdfFontFamily.Helvetica;
                        break;
                    case "times new roman":
                    case "times":
                        pdfFontFamily = PdfFontFamily.TimesRoman;
                        break;
                    case "courier":
                    case "courier new":
                        pdfFontFamily = PdfFontFamily.Courier;
                        break;
                    default:
                        break;
                }

                // Convert FontStyle to Syncfusion's style
                PdfFontStyle pdfStyle = PdfFontStyle.Regular;
                if (style.HasFlag(FontStyle.Bold)) pdfStyle |= PdfFontStyle.Bold;
                if (style.HasFlag(FontStyle.Italic)) pdfStyle |= PdfFontStyle.Italic;
                if (style.HasFlag(FontStyle.Underline)) pdfStyle |= PdfFontStyle.Underline;
                if (style.HasFlag(FontStyle.Strikeout)) pdfStyle |= PdfFontStyle.Strikeout;

                return new PdfStandardFont(pdfFontFamily, fontSize, pdfStyle);
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
