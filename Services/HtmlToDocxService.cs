using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CTOM.Services
{
    public class HtmlToDocxService
    {
        public void ConvertAndSave(string htmlContent, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
            {
                throw new ArgumentException("htmlContent must not be empty", nameof(htmlContent));
            }

            // Create a new Word document
            using var document = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);

            // Add the main part with an empty body first
            var mainPart = document.AddMainDocumentPart();
            var body = new Body();
            mainPart.Document = new Document(body);

            // -----------------------------
            // Insert the HTML via AltChunk – Word will convert it when opening the file.
            // This preserves most formatting (tables, paragraphs, bold, etc.)
            // -----------------------------
            const string altChunkId = "HtmlChunk";
            var altPart = mainPart.AddAlternativeFormatImportPart(AlternativeFormatImportPartType.Xhtml, altChunkId);
                        // Wrap user HTML into minimal valid XHTML so Word can parse it
            var xhtmlContent = $"""<html xmlns="http://www.w3.org/1999/xhtml"><head><meta charset="utf-8" /></head><body>{htmlContent}</body></html>""";
            using (var writer = new StreamWriter(altPart.GetStream()))
            {
                writer.Write(xhtmlContent);
            }

            // Reference the imported HTML in the body
            body.Append(new AltChunk { Id = altChunkId });

            // Ensure there is at least one SectionProperties so Word can open the file
            body.Append(new Paragraph(new Run())); // dummy paragraph to avoid empty body problems
            body.Append(new SectionProperties(new PageSize(), new PageMargin()));

            mainPart.Document.Save();
        }

        private string CleanHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // Remove HTML tags but keep the content
            var text = Regex.Replace(html, "<[^>]*>", "");
            
            // Decode HTML entities
            text = System.Net.WebUtility.HtmlDecode(text);
            
            // Normalize line breaks
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            
            return text.Trim();
        }

        private string ProcessPlaceholders(string text)
        {
            // This regex finds placeholders like <<fieldName>>
            return Regex.Replace(text, "<<([^>]+)>>", match =>
            {
                var fieldName = match.Groups[1].Value.Trim();
                return $"«{fieldName}»"; // Using guillemets as placeholders
            });
        }

        private void AddTextWithFormatting(Run run, string text)
        {
            // Split text into normal text and placeholders
            var parts = Regex.Split(text, "(«[^»]+»)");
            
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                
                if (part.StartsWith("«") && part.EndsWith("»"))
                {
                    // This is a placeholder - add with special formatting
                    var fieldName = part.Trim('«', '»');
                    var fieldRun = new Run();
                    
                    // Add formatting for the placeholder
                    var runProperties = new RunProperties();
                    runProperties.Append(new Color { Val = "2E75B5" }); // Blue color
                    runProperties.Append(new Highlight { Val = HighlightColorValues.Yellow });
                    runProperties.Append(new RunFonts { Ascii = "Times New Roman" });
                    runProperties.Append(new FontSize { Val = "22" }); // 11pt
                    
                    fieldRun.Append(runProperties);
                    fieldRun.Append(new Text($"{fieldName}"));
                    
                    run.AppendChild(fieldRun);
                }
                else
                {
                    // Normal text
                    run.AppendChild(new Text(part));
                }
            }
        }
    }
}
