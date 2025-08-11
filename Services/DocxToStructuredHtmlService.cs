#nullable enable

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions; // Thêm thư viện Regex để sử dụng
using System.Security.Cryptography; // [SỬA ĐỔI] Thêm using cho việc hash

namespace CTOM.Services;

public class DocxToStructuredHtmlService
{
    private int _paragraphIndex = 0;
    private int _tableIndex = 0;
    private int _checkboxIndex = 0;
    private int _imageIndex = 0;
    private int _nestedTableDepth = 0;
    private int _globalElementId = 0;
    private Dictionary<string, string> _elementIdMap = new();

    // SỬA ĐỔI: Thêm tham số maxTableNestingLevel.
    public string ConvertToHtml(byte[] docxBytes, int maxTableNestingLevel, bool isViewMode = false)
    {
        using var stream = new MemoryStream(docxBytes);
        using var wordDoc = WordprocessingDocument.Open(stream, false);

        var mainPart = wordDoc.MainDocumentPart;
        if (mainPart?.Document?.Body == null) return string.Empty;

        var body = mainPart.Document.Body;
        var htmlBuilder = new StringBuilder();

        // Reset counters for each conversion
        _paragraphIndex = 0;
        _tableIndex = 0;
        _checkboxIndex = 0;
        _imageIndex = 0;
        _nestedTableDepth = 0;
        _globalElementId = 0;
        _elementIdMap.Clear();

        htmlBuilder.Append("<div data-container=\"document\" data-id=\"doc-1\">");
        //htmlBuilder.Append("<div data-container=\"document\" data-id=\"doc-1\" style=\"margin: 0.5rem;\">");

        // SỬA ĐỔI: Truyền maxTableNestingLevel vào hàm xử lý.
        ProcessBodyElements(body.Elements(), mainPart, htmlBuilder, "body", maxTableNestingLevel, isViewMode);

        htmlBuilder.Append("</div>");
        return htmlBuilder.ToString();
    }
    private string GenerateUniqueElementId(string elementType)
    {
        return $"{elementType}-{++_globalElementId}";
    }

    /// <summary>
    /// Identify and normalize common bullet/symbol characters for list items
    /// Based on SpecialBullets.txt reference for Word bullets to HTML entities
    /// </summary>
    /// <param name="text">Text to check</param>
    /// <param name="isViewMode">True if in view mode</param>
    /// <returns>Tuple (isBullet, bulletSymbol)</returns>
    private (bool isBullet, string bulletSymbol) IdentifyBulletSymbol(string text, bool isViewMode = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (false, "");

        var trimmedText = text.Trim();

        // Special bullets mapping based on SpecialBullets.txt
        // Format: Word Character -> HTML Safe Character (view mode uses simple dash for consistency)
        var bulletSymbols = new Dictionary<string, string>
        {
            // Common Word bullets with HTML entities
            { "•", isViewMode ? "-" : "&#8226;" },    // Bullet (dot) - U+2022
            { "◦", isViewMode ? "-" : "&#9702;" },    // White Bullet - U+25E6
            { "–", isViewMode ? "-" : "&#8211;" },    // En Dash - U+2013
            { "—", isViewMode ? "-" : "&#8212;" },    // Em Dash - U+2014
            { "▪", isViewMode ? "-" : "&#9642;" },    // Black Small Square - U+25AA
            { "■", isViewMode ? "-" : "&#9632;" },    // Black Square - U+25A0
            { "●", isViewMode ? "-" : "&#9679;" },    // Black Circle - U+25CF
            { "○", isViewMode ? "-" : "&#9675;" },    // White Circle - U+25CB
            { "♦", isViewMode ? "-" : "&#9830;" },    // Diamond - U+2666
            { "✓", isViewMode ? "-" : "&#10003;" },   // Check Mark - U+2713
            { "→", isViewMode ? "-" : "&#8594;" },    // Right Arrow - U+2192

            // Simple text bullets (always keep as-is)
            { "-", "-" },                           // Hyphen - U+002D
            { "+", "+" },                           // Plus - U+002B
            { "*", "*" },                           // Asterisk - U+002A

            // Additional common bullets
            { "□", isViewMode ? "-" : "□" },         // White Square
            { "▫", isViewMode ? "-" : "▫" },         // White Small Square
            { "◆", isViewMode ? "-" : "◆" },         // Black Diamond
            { "◇", isViewMode ? "-" : "◇" },         // White Diamond
            { "~", "~" },                           // Tilde
            { ">", ">" },                           // Greater than
            { "»", isViewMode ? "-" : "»" },         // Right-pointing double angle quotation mark
            { "►", isViewMode ? "-" : "►" },         // Black right-pointing pointer
            { "▶", isViewMode ? "-" : "▶" },         // Black right-pointing triangle
        };

        // Check for exact match
        if (bulletSymbols.ContainsKey(trimmedText))
        {
            return (true, bulletSymbols[trimmedText]);
        }

        // Check if text starts with bullet symbol + space/tab
        foreach (var kvp in bulletSymbols)
        {
            if (trimmedText.StartsWith(kvp.Key + " ") || trimmedText.StartsWith(kvp.Key + "\t"))
            {
                return (true, kvp.Value);
            }
        }

        return (false, "");
    }

    /// <summary>
    /// Get appropriate bullet character based on numbering ID and level
    /// Reads from numbering.xml to get exact bullet character
    /// </summary>
    private string GetNumberingBullet(int numId, int ilvl, bool isViewMode, MainDocumentPart mainPart)
    {
        try
        {
            var numberingPart = mainPart.NumberingDefinitionsPart;
            if (numberingPart?.Numbering == null)
            {
                return isViewMode ? "-" : "&#8226;"; // Fallback if no numbering part
            }

            // Find the num element with matching numId
            var numElement = numberingPart.Numbering.Elements<NumberingInstance>()
                .FirstOrDefault(n => n.NumberID?.Value == numId);
            if (numElement == null)
            {
                return isViewMode ? "-" : "&#8226;"; // Fallback if numId not found
            }

            // Get the abstractNumId
            var abstractNumId = numElement.AbstractNumId?.Val?.Value;
            if (abstractNumId == null)
            {
                return isViewMode ? "-" : "&#8226;"; // Fallback if no abstractNumId
            }

            // Find the abstractNum element
            var abstractNum = numberingPart.Numbering.Elements<AbstractNum>()
                .FirstOrDefault(a => a.AbstractNumberId?.Value == abstractNumId);
            if (abstractNum == null)
            {
                return isViewMode ? "-" : "&#8226;"; // Fallback if abstractNum not found
            }

            // Find the level with matching ilvl
            var level = abstractNum.Elements<Level>()
                .FirstOrDefault(l => l.LevelIndex?.Value == ilvl);
            if (level == null)
            {
                return isViewMode ? "-" : "&#8226;"; // Fallback if level not found
            }

            // Handle special fonts (Wingdings, Symbol, etc.) with character codes
            var runProperties = level.Elements<RunProperties>().FirstOrDefault();
            var fontName = runProperties?.RunFonts?.Ascii?.Value;

            // Get the bullet character from lvlText
            var lvlText = level.LevelText?.Val?.Value;

            // First, try to decode special font characters if we have font info
            if (!string.IsNullOrEmpty(fontName) && !string.IsNullOrEmpty(lvlText))
            {
                // Check if this is a special font that needs character decoding
                if (fontName.Contains("Wingdings") || fontName.Contains("Symbol"))
                {
                    var decodedChar = DecodeSpecialFontCharacter(lvlText, fontName, isViewMode);
                    if (!string.IsNullOrEmpty(decodedChar))
                    {
                        return decodedChar;
                    }
                    // If decoding failed, try to handle as Unicode character
                    if (lvlText.Length == 1)
                    {
                        var unicodeValue = (int)lvlText[0];
                        // Common Wingdings/Symbol bullet ranges
                        if (unicodeValue >= 0xF020 && unicodeValue <= 0xF0FF)
                        {
                            return isViewMode ? "■" : "&#9632;"; // Black square for view mode
                        }
                    }
                }
            }

            // Handle direct text bullets or Unicode characters
            if (!string.IsNullOrEmpty(lvlText))
            {
                // Check if it's a single Unicode character that might be a bullet
                if (lvlText.Length == 1)
                {
                    var unicodeValue = (int)lvlText[0];
                    // Handle common bullet Unicode ranges
                    if (unicodeValue >= 0x2022 && unicodeValue <= 0x25FF) // Bullet and geometric shapes
                    {
                        return isViewMode ? "■" : $"&#{unicodeValue};"; // Use HTML entity
                    }
                    // Handle private use area (often used by symbol fonts)
                    if (unicodeValue >= 0xE000 && unicodeValue <= 0xF8FF)
                    {
                        return isViewMode ? "■" : "&#9632;"; // Default to black square
                    }
                }

                // Try normal bullet conversion for text-based bullets
                var convertedBullet = ConvertBulletToHtml(lvlText, isViewMode);
                if (!string.IsNullOrEmpty(convertedBullet) && convertedBullet != lvlText)
                {
                    return convertedBullet;
                }

                // If all else fails but we have lvlText, use it as-is or convert to safe bullet
                //return isViewMode ? "■" : (lvlText.Length == 1 ? $"&#{(int)lvlText[0]};" : "&#9632;");
                return isViewMode ? "-" : "&#8226;";
            }

            // Fallback to default bullet if no specific character found
            return isViewMode ? "-" : "&#8226;";
        }
        catch (Exception)
        {
            // Error reading numbering definition, use safe fallback
            return isViewMode ? "-" : "&#8226;"; // Safe fallback
        }
    }

    // SỬA ĐỔI: Thêm tham số maxTableNestingLevel và truyền xuống các hàm con.
    private void ProcessBodyElements(IEnumerable<OpenXmlElement> elements, MainDocumentPart mainPart, StringBuilder htmlBuilder, string parentPath, int maxTableNestingLevel, bool isViewMode)
    {
        foreach (var element in elements)
        {
            switch (element)
            {
                case Paragraph p:
                    ProcessParagraphWithPath(p, mainPart, htmlBuilder, $"{parentPath}.p[{_paragraphIndex}]", maxTableNestingLevel, isViewMode);
                    _paragraphIndex++;
                    break;

                case Table t:
                    ProcessTableWithPath(t, mainPart, htmlBuilder, $"{parentPath}.tbl[{_tableIndex}]", maxTableNestingLevel, isViewMode);
                    _tableIndex++;
                    break;
            }
        }
    }

    // SỬA ĐỔI: Sử dụng maxTableNestingLevel để xác định data-mappable, thêm data-paragraph-hash
        private void ProcessParagraphWithPath(Paragraph p, MainDocumentPart mainPart, StringBuilder htmlBuilder, string docxPath, int maxTableNestingLevel, bool isViewMode)
    {
        var paragraphId = GenerateUniqueElementId("p");
        _elementIdMap[docxPath] = paragraphId;

        string paragraphStyle = ParseParagraphProperties(p.ParagraphProperties, mainPart);
        // [SỬA ĐỔI] Tính hash nội dung của paragraph để làm định danh bền vững
        var paragraphTextContent = p.InnerText;
        var paragraphHash = ComputeSha256Hash(paragraphTextContent);

        bool hasNumberingBullet = false;
        string numberingBullet = "";

        var pPr = p.ParagraphProperties;
        if (pPr?.NumberingProperties != null)
        {
            var numPr = pPr.NumberingProperties;
            var numId = numPr.NumberingId?.Val?.Value;
            var ilvl = numPr.NumberingLevelReference?.Val?.Value ?? 0;
            var pStyle = pPr.ParagraphStyleId?.Val?.Value;

            if (pStyle == "ListParagraph" || numId.HasValue)
            {
                hasNumberingBullet = true;
                numberingBullet = GetNumberingBullet(numId ?? 0, ilvl, isViewMode, mainPart);
            }
        }

        if (isViewMode)
        {
            htmlBuilder.Append($"<p style='{paragraphStyle}'>");
        }
        else
        {
            var maxAllowedDepth = maxTableNestingLevel + 1;
            string mappableAttr = (_nestedTableDepth <= maxAllowedDepth) ? "true" : "false";

            // [SỬA ĐỔI] Thêm data-paragraph-hash vào thẻ p
            htmlBuilder.Append($"<p data-mappable=\"{mappableAttr}\" data-element-id=\"{paragraphId}\" " +
                              $"data-paragraph-hash=\"{paragraphHash}\" " + // Thêm thuộc tính hash
                              $"data-type=\"paragraph\" data-docx-path=\"{docxPath}\" " +
                              $"data-nested-depth=\"{_nestedTableDepth}\" style='{paragraphStyle}'>");
        }

        if (hasNumberingBullet)
        {
            if (isViewMode)
            {
                htmlBuilder.Append($"<span style=\"display: inline-block; margin-right: 3px; font-size: 12px; color: #333;\">{numberingBullet}</span>");
            }
            else
            {
                htmlBuilder.Append($"<span data-mappable=\"false\" data-bulletid=\"{_paragraphIndex}-numbering-bullet\" " +
                                  $"data-type=\"numbering-bullet\" data-docx-path=\"{docxPath}.numbering-bullet\" " +
                                  $"style=\"display: inline-block; margin-right: 3px; font-size: 12px; color: #333;\">{numberingBullet}</span>");
            }
        }

        ProcessRunElements(p.Elements(), mainPart, htmlBuilder, docxPath, paragraphId, isViewMode);

        htmlBuilder.Append("</p>");
    }

    private Dictionary<int, string> PreprocessTextSequences(List<OpenXmlElement> elements)
    {
        var textMap = new Dictionary<int, string>();

        int runIndex = 0;
        foreach (var element in elements)
        {
            if (element is Run run)
            {
                var originalText = string.Join("", run.Elements<Text>().Select(t => t.Text ?? ""));
                var processedText = originalText;

                // Thay thế các chuỗi từ 3 ký tự trở lên thuộc họ "dấu chấm" bằng 4 dấu cách
                processedText = Regex.Replace(
                    processedText,
                    @"([\.…⋯︙⋮·•‧∙．｡。])\1{2,}",  // lặp >= 3 lần các ký tự chấm
                    "    "
                );

                if (processedText != originalText)
                {
                    textMap[runIndex] = processedText;
                }
            }
            runIndex++;
        }

        return textMap;
    }

    private void ProcessRunElements(IEnumerable<OpenXmlElement> elements, MainDocumentPart mainPart, StringBuilder htmlBuilder, string parentPath, string parentElementId = "", bool isViewMode = false)
    {
        // BƯỚC TIỀN XỬ LÝ: Thu thập tất cả text từ các run để xử lý chuỗi dấu chấm/gạch dưới liên tiếp
        var elementsList = elements.ToList();
        var preprocessedTexts = PreprocessTextSequences(elementsList);

        int runIndex = 0;

        foreach (var element in elementsList)
        {
            if (element is Run run)
            {
                string runStyle = ParseRunProperties(run.RunProperties, mainPart);

                // Enhanced checkbox and bullet detection logic
                bool isCheckbox = false;
                bool isBullet = false;
                bool isChecked = false;
                string bulletIcon = "";

                // Method 1: Check for symbols (SymbolChar)
                var symbolChar = run.Elements<SymbolChar>().FirstOrDefault();
                if (symbolChar != null)
                {
                    var charValue = symbolChar.Char?.Value;
                    var fontName = symbolChar.Font?.Value?.ToLower();

                    // Checkbox symbols in Wingdings font
                    if (charValue == "F0FE" || charValue == "F0FC" || // Checked boxes
                        charValue == "F0A8" || charValue == "F0A7" || // Unchecked boxes
                        charValue == "F06F" || charValue == "F070")   // Alternative checkbox chars
                    {
                        isCheckbox = true;
                        isChecked = charValue == "F0FE" || charValue == "F0FC" || charValue == "F06F";
                    }
                    // Bullet symbols - enhanced detection for Wingdings 2 and other fonts
                    else if (charValue == "F0B7" || charValue == "F0B8" || // Square bullets
                             charValue == "F06C" || charValue == "F06D" || // Circle bullets
                             charValue == "F0D8" || charValue == "F0FC" || // Diamond bullets
                             charValue == "F0A7" || charValue == "F0A8" || // Other bullet styles
                             charValue == "F0B2" || charValue == "F0B3" || // Additional square bullets
                             charValue == "F0A1" || charValue == "F0A2" || // Small bullets
                             charValue == "F0B9" || charValue == "F0BA" || // More bullet variants
                             // Wingdings 2 specific characters for square bullets
                             (fontName != null && fontName.Contains("wingdings") &&
                              (charValue == "F020" || charValue == "F021" || charValue == "F022" ||
                               charValue == "F023" || charValue == "F024" || charValue == "F025")))
                    {
                        isBullet = true;
                        // Map to appropriate bullet icons - phân biệt view mode và mapping mode
                        if (isViewMode)
                        {
                            // VIEW MODE: Convert tất cả symbol thành bullet thông thường
                            bulletIcon = charValue switch
                            {
                                "F0B7" or "F0B8" or "F0B2" or "F0B3" => "-", // Square bullet -> dash
                                "F06C" or "F06D" => "•", // Circle bullet
                                "F0D8" => "♦",           // Diamond bullet
                                "F0A1" or "F0A2" or "F0B9" or "F0BA" => "-", // White small square -> dash
                                "F020" or "F021" or "F022" or "F023" or "F024" or "F025" => "-", // Wingdings 2 -> dash
                                _ => "-"                  // Default to dash
                            };
                        }
                        else
                        {
                            // MAPPING MODE: Giữ nguyên logic cũ
                            bulletIcon = charValue switch
                            {
                                "F0B7" or "F0B8" or "F0B2" or "F0B3" => "☐", // Square bullet like checkbox
                                "F06C" or "F06D" => "•", // Circle bullet
                                "F0D8" => "♦",           // Diamond bullet
                                "F0A1" or "F0A2" or "F0B9" or "F0BA" => "▫", // White small square
                                "F020" or "F021" or "F022" or "F023" or "F024" or "F025" => "☐", // Wingdings 2
                                _ => "☐"                  // Default square like checkbox
                            };
                        }
                    }
                }

                // Method 2: Check for text content
                // We check for text-based bullets before checking for checkboxes to avoid misidentification.
                if (!isCheckbox && !isBullet)
                {
                    var textElements = run.Elements<Text>();
                    foreach (var text in textElements)
                    {
                        var textContent = text.Text?.Trim();
                        if (!string.IsNullOrEmpty(textContent))
                        {
                            // Use general function to identify bullet symbols
                            var (isBulletSymbol, bulletSymbol) = IdentifyBulletSymbol(textContent, isViewMode);
                            if (isBulletSymbol)
                            {
                                isBullet = true;
                                bulletIcon = bulletSymbol;
                                break;
                            }

                            // Special case: Check if this run is the first run in paragraph and contains only "-"
                            // This handles cases where bullet "-" is in a separate run from the text
                            if (textContent == "-" && runIndex == 0)
                            {
                                // Check if paragraph has indent or is likely a list item
                                var paragraph = run.Ancestors<Paragraph>().FirstOrDefault();
                                if (paragraph != null)
                                {
                                    // Check for paragraph properties indicating list or indent
                                    var pPr = paragraph.ParagraphProperties;
                                    var hasIndent = (pPr?.Indentation?.Left?.HasValue == true && int.TryParse(pPr.Indentation.Left.Value, out var leftIndent) && leftIndent > 0) ||
                                                   (pPr?.Indentation?.Hanging?.HasValue == true && int.TryParse(pPr.Indentation.Hanging.Value, out var hangingIndent) && hangingIndent > 0) ||
                                                   (pPr?.Indentation?.FirstLine?.HasValue == true && int.TryParse(pPr.Indentation.FirstLine.Value, out var firstLineIndent) && firstLineIndent > 0);

                                    // Check for numbering properties (list)
                                    var hasNumbering = pPr?.NumberingProperties != null;

                                    // If has indent or numbering, treat "-" as bullet
                                    if (hasIndent || hasNumbering)
                                    {
                                        isBullet = true;
                                        bulletIcon = "-";
                                        break;
                                    }
                                }
                            }

                            // CHỈ NHẬN DIỆN CHECKBOX THẬT từ Word - không phải text thường
                            // Chỉ kiểm tra exact match với các ký hiệu checkbox chuẩn
                            if (textContent == "☐" || textContent == "☑" || textContent == "☒")
                            {
                                isCheckbox = true;
                                isChecked = textContent == "☑" || textContent == "☒";
                                break; // Đã tìm thấy checkbox, không cần kiểm tra thêm trong run này.
                            }
                        }
                    }
                }

                // Method 3: Check for legacy form field checkbox - ACCURATE DETECTION
                if (!isCheckbox)
                {
                    var fieldChar = run.Elements<FieldChar>().FirstOrDefault();
                    if (fieldChar != null && fieldChar.FieldCharType?.Value == FieldCharValues.Begin)
                    {
                        // Check if this is a real checkbox form field
                        // Option 1: Check field code contains "FORMCHECKBOX"
                        var nextRun = run.NextSibling<Run>();
                        if (nextRun != null)
                        {
                            var fieldCode = nextRun.Elements<FieldCode>().FirstOrDefault();
                            if (fieldCode != null && fieldCode.Text != null &&
                                fieldCode.Text.Contains("FORMCHECKBOX", StringComparison.OrdinalIgnoreCase))
                            {
                                isCheckbox = true;
                                // Check checked state from field code
                                isChecked = fieldCode.Text.Contains("\\default 1") || fieldCode.Text.Contains("\\checked");
                            }
                        }

                        // Option 2: If no field code found, check context
                        if (!isCheckbox)
                        {
                            // Check if run contains checkbox symbols
                            var runText = run.InnerText?.Trim();
                            var parentText = run.Parent?.InnerText?.Trim();

                            // If run or parent contains checkbox symbols, it might be a checkbox
                            if (!string.IsNullOrEmpty(runText) && (runText.Contains("☐") || runText.Contains("☑") || runText.Contains("☒")) ||
                                !string.IsNullOrEmpty(parentText) && (parentText.Contains("☐") || parentText.Contains("☑") || parentText.Contains("☒")))
                            {
                                isCheckbox = true;
                                isChecked = runText?.Contains("☑") == true || runText?.Contains("☒") == true ||
                                           parentText?.Contains("☑") == true || parentText?.Contains("☒") == true;
                            }
                        }
                    }
                }

                if (isCheckbox)
                {
                    // REAL CHECKBOX from Word: Display in both view mode and mapping mode
                    if (isViewMode)
                    {
                        // VIEW MODE: Display real checkbox from Word, no data attributes
                        htmlBuilder.Append($"<span style=\"display: inline-block; margin-right: 3px; font-size: 14px; color: #333;\">");
                        htmlBuilder.Append(isChecked ? "☑" : "☐");
                        htmlBuilder.Append("</span>");
                    }
                    else
                    {
                        // MAPPING MODE: Display checkbox with full data attributes
                        htmlBuilder.Append($"<span data-mappable=\"false\" data-chkid=\"{_paragraphIndex}-chk-{_checkboxIndex}\" " +
                                          $"data-type=\"checkbox\" data-docx-path=\"{parentPath}.r[{runIndex}].checkbox\" " +
                                          $"style=\"display: inline-block; margin-right: 3px; font-size: 14px; color: #333;\">");
                        htmlBuilder.Append(isChecked ? "☑" : "☐");
                        htmlBuilder.Append("</span>");
                    }

                    _checkboxIndex++;
                    runIndex++;
                    continue;
                }

                if (isBullet)
                {
                    // Render bullet icon
                    htmlBuilder.Append($"<span data-mappable=\"false\" data-bulletid=\"{_paragraphIndex}-bullet-{_checkboxIndex}\" " +
                                      $"data-type=\"bullet\" data-docx-path=\"{parentPath}.r[{runIndex}].bullet\" " +
                                      $"style=\"display: inline-block; margin-right: 3px; font-size: 12px; color: #333;\">");
                    htmlBuilder.Append(bulletIcon);
                    htmlBuilder.Append("</span>");
                    _checkboxIndex++;

                    // Xử lý text còn lại sau bullet
                    string remainingText = "";
                    foreach (var text in run.Elements<Text>())
                    {
                        var textContent = text.Text ?? "";
                        // Loại bỏ bullet character khỏi đầu text
                        if (textContent.StartsWith("-"))
                        {
                            remainingText = textContent.Substring(1).TrimStart();
                        }
                        else if (textContent.StartsWith("+"))
                        {
                            remainingText = textContent.Substring(1).TrimStart();
                        }
                        else if (textContent.StartsWith("*"))
                        {
                            remainingText = textContent.Substring(1).TrimStart();
                        }
                        else if (textContent != bulletIcon)
                        {
                            remainingText = textContent;
                        }
                        break;
                    }

                    // Render remaining text nếu có
                    if (!string.IsNullOrWhiteSpace(remainingText))
                    {
                        if (preprocessedTexts.ContainsKey(runIndex))
                        {
                            var processedText = preprocessedTexts[runIndex];
                            // Remove bullet from processed text
                            if (processedText.StartsWith(bulletIcon))
                            {
                                processedText = processedText.Substring(bulletIcon.Length).TrimStart();
                            }
                            else if (processedText.StartsWith("-") || processedText.StartsWith("+") || processedText.StartsWith("*"))
                            {
                                processedText = processedText.Substring(1).TrimStart();
                            }

                            if (!string.IsNullOrWhiteSpace(processedText))
                            {
                                htmlBuilder.Append($"<span data-mappable=\"true\" data-rid=\"{_paragraphIndex}-{runIndex}\" " +
                                                  $"data-type=\"run\" data-docx-path=\"{parentPath}.r[{runIndex}]\" style='{runStyle}'>");
                                var highlightedContent = HighlightPlaceholders(processedText);
                                htmlBuilder.Append(highlightedContent);
                                htmlBuilder.Append("</span>");
                            }
                        }
                        else
                        {
                            htmlBuilder.Append($"<span data-mappable=\"true\" data-rid=\"{_paragraphIndex}-{runIndex}\" " +
                                              $"data-type=\"run\" data-docx-path=\"{parentPath}.r[{runIndex}]\" style='{runStyle}'>");
                            var highlightedContent = HighlightPlaceholders(remainingText);
                            htmlBuilder.Append(highlightedContent);
                            htmlBuilder.Append("</span>");
                        }
                    }

                    runIndex++;
                    continue;
                }

                // Check for image in this run
                var drawing = run.Elements<Drawing>().FirstOrDefault();
                if (drawing != null)
                {
                    var imagePart = GetImagePartFromDrawing(drawing, mainPart);
                    if (imagePart != null)
                    {
                        // Set responsive image style to prevent table overflow
                        string imageStyle = "max-width: 150px; max-height: 150px; object-fit: contain; margin: 2px;";

                        // Try to get original dimensions (simplified approach)
                        var extentElements = drawing.Descendants().Where(d => d.LocalName == "extent");
                        if (extentElements.Any())
                        {
                            // Use smaller fixed size for images in tables to prevent overflow
                            imageStyle = "width: 100px; height: auto; max-height: 100px; object-fit: contain; margin: 2px;";
                        }

                        using var stream = imagePart.GetStream();
                        using var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        var base64Image = Convert.ToBase64String(ms.ToArray());
                        var contentType = imagePart.ContentType;
                        htmlBuilder.Append($"<img data-mappable=\"false\" data-imgid=\"{_paragraphIndex}-img-{_imageIndex}\" " +
                                          $"data-type=\"image\" data-docx-path=\"{parentPath}.r[{runIndex}].drawing\" " +
                                          $"src=\"data:{contentType};base64,{base64Image}\" " +
                                          $"style=\"{imageStyle}\" />");
                        _imageIndex++;
                        runIndex++;
                        continue;
                    }
                }

                // Check for text content
                if (run.Elements<Text>().Any())
                {
                    if (isViewMode)
                    {
                        // View mode: span đơn giản, không cần data attributes
                        htmlBuilder.Append($"<span style='{runStyle}'>");
                    }
                    else
                    {
                        // Mapping mode: đầy đủ data attributes
                        htmlBuilder.Append($"<span data-mappable=\"true\" data-rid=\"{_paragraphIndex}-{runIndex}\" " +
                                          $"data-type=\"run\" data-docx-path=\"{parentPath}.r[{runIndex}]\" style='{runStyle}'>");
                    }

                    // Sử dụng text đã được tiền xử lý nếu có, nếu không thì xử lý như cũ
                    if (preprocessedTexts.ContainsKey(runIndex))
                    {
                        // Text đã được xử lý chuỗi dấu chấm/gạch dưới ở bước tiền xử lý
                        var processedText = preprocessedTexts[runIndex];

                        // Vẫn cần xử lý spaces
                        foreach (var text in run.Elements<Text>())
                        {
                            string finalContent = processedText;

                            // Xử lý spaces dựa trên Space attribute
                            if (text.Space?.Value == SpaceProcessingModeValues.Preserve)
                            {
                                if (finalContent.StartsWith(" ") || finalContent.EndsWith(" ") || finalContent.Contains("  "))
                                {
                                    finalContent = Regex.Replace(finalContent, @"^ +", m => new string('\u00A0', m.Length));
                                    finalContent = Regex.Replace(finalContent, @" +$", m => new string('\u00A0', m.Length));
                                    finalContent = Regex.Replace(finalContent, @"  +", m => new string('\u00A0', m.Length));
                                }
                            }

                            // Highlight placeholder và append text
                            var highlightedContent = HighlightPlaceholders(finalContent, isViewMode);
                            htmlBuilder.Append(highlightedContent);
                            break; // Chỉ xử lý text đầu tiên vì đã được merge
                        }
                    }
                    else
                    {
                        // Xử lý text như cũ nếu không có trong preprocessed
                        foreach (var text in run.Elements<Text>())
                        {
                            var textContent = text.Text ?? string.Empty;

                            // Xử lý spaces dựa trên Space attribute
                            string finalContent;
                            if (text.Space?.Value == SpaceProcessingModeValues.Preserve)
                            {
                                var spaceProcessedContent = textContent;
                                if (spaceProcessedContent.StartsWith(" ") || spaceProcessedContent.EndsWith(" ") || spaceProcessedContent.Contains("  "))
                                {
                                    spaceProcessedContent = Regex.Replace(spaceProcessedContent, @"^ +", m => new string('\u00A0', m.Length));
                                    spaceProcessedContent = Regex.Replace(spaceProcessedContent, @" +$", m => new string('\u00A0', m.Length));
                                    spaceProcessedContent = Regex.Replace(spaceProcessedContent, @"  +", m => new string('\u00A0', m.Length));
                                }
                                finalContent = spaceProcessedContent;
                            }
                            else
                            {
                                finalContent = textContent;
                            }

                            // Highlight placeholder và append text
                            var highlightedContent = HighlightPlaceholders(finalContent, isViewMode);
                            htmlBuilder.Append(highlightedContent);
                        }
                    }

                    htmlBuilder.Append("</span>");
                    runIndex++;
                }
                else if (run.Elements<TabChar>().Any())
                {
                    // Xử lý TabChar - sử dụng CSS tab-size để bảo toàn khoảng cách như Word
                    // Word default tab stops are typically every 0.5 inches (36 points)
                    int tabCount = run.Elements<TabChar>().Count();

                    if (isViewMode)
                    {
                        // View mode: sử dụng CSS tab character để giống Word hơn
                        for (int i = 0; i < tabCount; i++)
                        {
                            htmlBuilder.Append("<span style='display: inline-block; width: 2em; white-space: pre;'>\t</span>");
                        }
                    }
                    else
                    {
                        // Mapping mode: giữ nguyên logic cũ để tránh ảnh hưởng mapping
                        for (int i = 0; i < tabCount; i++)
                        {
                            htmlBuilder.Append("&nbsp;&nbsp;&nbsp;&nbsp;");
                        }
                    }
                    runIndex++;
                }
            }
            else if (element is SdtRun sdtRun)
            {
                // Process structured document tag (modern checkbox)
                ProcessStructuredDocumentTag(sdtRun, mainPart, htmlBuilder, parentPath, runIndex, isViewMode);
                runIndex++;
            }
            else if (element is FieldChar fieldChar)
            {
                // Process legacy checkbox (FieldChar type)
                if (HasCheckboxFormField(fieldChar))
                {
                    if (!isViewMode)
                    {
                        // MAPPING MODE: Render legacy checkbox
                        htmlBuilder.Append($"<span data-mappable=\"false\" data-chkid=\"{_paragraphIndex}-chk-{_checkboxIndex}\" " +
                                          $"data-type=\"checkbox\" data-docx-path=\"{parentPath}.field[{runIndex}]\" " +
                                          $"style=\"display: inline-block; margin-right: 3px; font-size: 14px; color: #333;\">");
                        htmlBuilder.Append("☐"); // Default unchecked for legacy
                        htmlBuilder.Append("</span>");
                        _checkboxIndex++;
                    }
                    // VIEW MODE: HOÀN TOÀN BỎ QUA - không render checkbox
                }
                runIndex++;
            }
            else if (element is Drawing drawing)
            {
                // Process drawing/image at paragraph level
                var imagePart = GetImagePartFromDrawing(drawing, mainPart);
                if (imagePart != null)
                {
                    using var stream = imagePart.GetStream();
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    var base64Image = Convert.ToBase64String(ms.ToArray());
                    var contentType = imagePart.ContentType;
                    htmlBuilder.Append($"<img data-mappable=\"false\" data-imgid=\"{_paragraphIndex}-img-{_imageIndex}\" " +
                                      $"data-type=\"image\" data-docx-path=\"{parentPath}.drawing[{runIndex}]\" " +
                                      $"src=\"data:{contentType};base64,{base64Image}\" " +
                                      $"style=\"max-width: 150px; max-height: 150px; object-fit: contain; margin: 2px;\" />");
                    _imageIndex++;
                }
                runIndex++;
            }
        }
    }

    // SỬA ĐỔI: Sử dụng maxTableNestingLevel để xác định data-mappable và truyền cho các hàm đệ quy.
   private void ProcessTableWithPath(Table table, MainDocumentPart mainPart, StringBuilder htmlBuilder, string docxPath, int maxTableNestingLevel, bool isViewMode)
    {
        _nestedTableDepth++;
        bool isNestedTable = _nestedTableDepth > 1;

        var tableId = GenerateUniqueElementId("tbl");
        _elementIdMap[docxPath] = tableId;

        string colGroupHtml = BuildColGroup(table);

        string tableStyle = ParseTableProperties(table.GetFirstChild<TableProperties>());
        string tableClass = isNestedTable ? "nested-table" : "parent-table";

        var maxAllowedDepth = maxTableNestingLevel + 1;
        string mappableAttr = (_nestedTableDepth <= maxAllowedDepth) ? "true" : "false";

        htmlBuilder.Append($"<table class=\"{tableClass}\" data-element-id=\"{tableId}\" " +
                          $"data-type=\"table\" data-docx-path=\"{docxPath}\" " +
                          $"data-nested-depth=\"{_nestedTableDepth}\" data-mappable=\"{mappableAttr}\" " +
                          $"style='border-collapse: collapse; {tableStyle}'>");

        if (!string.IsNullOrEmpty(colGroupHtml))
        {
            htmlBuilder.Append(colGroupHtml);
        }

        int rowIndex = 0;
        foreach (var row in table.Elements<TableRow>())
        {
            htmlBuilder.Append("<tr>");
            int cellIndex = 0;
            foreach (var cell in row.Elements<TableCell>())
            {
                string cellStyle = ParseTableCellProperties(cell.GetFirstChild<TableCellProperties>());
                string cellPath = $"{docxPath}.tr[{rowIndex}].tc[{cellIndex}]";

                var cellId = GenerateUniqueElementId("tc");
                _elementIdMap[cellPath] = cellId;

                var gridSpan = cell.GetFirstChild<TableCellProperties>()?.GridSpan?.Val?.Value ?? 1;
                string colspanAttr = gridSpan > 1 ? $" colspan=\"{gridSpan}\"" : "";

                string cellMappableAttr = (_nestedTableDepth <= maxAllowedDepth) ? "true" : "false";

                htmlBuilder.Append($"<td data-mappable=\"{cellMappableAttr}\" data-element-id=\"{cellId}\" " +
                                  $"data-type=\"cell\" data-docx-path=\"{cellPath}\" " +
                                  $"data-nested-depth=\"{_nestedTableDepth}\" " +
                                  $"style='{cellStyle}'{colspanAttr}>");

                if (!cell.Elements<Paragraph>().Any())
                {
                    var emptyParagraphPath = $"{cellPath}.p[0]";
                    var emptyParagraphId = GenerateUniqueElementId("p");
                    _elementIdMap[emptyParagraphPath] = emptyParagraphId;

                    string defaultRunStyle = GetDefaultRunStyle(mainPart);
                    string emptyParagraphStyle = $"margin: 0; padding: 0; min-height: 1em; {defaultRunStyle}";

                    htmlBuilder.Append($"<p data-mappable=\"{cellMappableAttr}\" data-element-id=\"{emptyParagraphId}\" " +
                                      $"data-type=\"paragraph\" data-docx-path=\"{emptyParagraphPath}\" " +
                                      $"data-nested-depth=\"{_nestedTableDepth}\" style='{emptyParagraphStyle}'>");
                    htmlBuilder.Append("&nbsp;");
                    htmlBuilder.Append("</p>");
                }
                else
                {
                    int cellParagraphIndex = 0;
                    foreach (var cellElement in cell.Elements())
                    {
                        if (cellElement is Paragraph cellParagraph)
                        {
                            ProcessParagraphWithPath(cellParagraph, mainPart, htmlBuilder, $"{cellPath}.p[{cellParagraphIndex}]", maxTableNestingLevel, isViewMode);
                            cellParagraphIndex++;
                        }
                        else if (cellElement is Table nestedTable)
                        {
                            ProcessTableWithPath(nestedTable, mainPart, htmlBuilder, $"{cellPath}.tbl[0]", maxTableNestingLevel, isViewMode);
                        }
                    }
                }
                htmlBuilder.Append("</td>");
                cellIndex++;
            }
            htmlBuilder.Append("</tr>");
            rowIndex++;
        }
        htmlBuilder.Append("</table>");

        _nestedTableDepth--;
    }

    private static string BuildColGroup(Table table)
    {
        var tableGrid = table.GetFirstChild<TableGrid>();
        if (tableGrid == null || !tableGrid.Elements<GridColumn>().Any())
            return "";

        var gridCols = tableGrid.Elements<GridColumn>().ToList();
        var sb = new StringBuilder();
        sb.Append("<colgroup>");

        // Calculate total width for percentage calculation
        var totalDxa = gridCols.Sum(gc => {
            if (uint.TryParse(gc.Width?.Value, out var width))
                return (long)width;
            return 0L;
        });
        if (totalDxa == 0) return "";

        foreach (var gridCol in gridCols)
        {
            var dxa = 0L;
            if (uint.TryParse(gridCol.Width?.Value, out var width))
                dxa = (long)width;
            if (dxa == 0)
            {
                sb.Append("<col>");
                continue;
            }
            float pct = dxa / (float)totalDxa * 100f;
            sb.Append($"<col style=\"width: {pct:F2}%\"></col>");
        }
        sb.Append("</colgroup>");
        return sb.ToString();
    }

    private static ImagePart? GetImagePartFromDrawing(Drawing drawing, MainDocumentPart mainPart)
    {
        var blip = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
        if (blip != null)
        {
            var embed = blip.Embed?.Value;
            if (embed != null)
            {
                return (ImagePart)mainPart.GetPartById(embed);
            }
        }
        return null;
    }

    private void ProcessStructuredDocumentTag(SdtRun sdtRun, MainDocumentPart mainPart, StringBuilder htmlBuilder, string parentPath, int runIndex, bool isViewMode = false)
    {
        // Check if this is a checkbox content control
        var checkBoxProp = sdtRun.SdtProperties?.GetFirstChild<CheckBox>();
        if (checkBoxProp != null)
        {
            if (isViewMode)
            {
                // VIEW MODE: HOÀN TOÀN BỞ QUA modern checkbox - không render gì cả
                return;
            }

            // MAPPING MODE: Render checkbox như cũ
            // Determine if checked (simplified logic)
            bool isChecked = false;
            var state = sdtRun.SdtContentRun?.Descendants<SymbolChar>().FirstOrDefault();
            if (state != null && (state.Char?.Value == "F0FC" || state.Char?.Value == "F0FE"))
            {
                // Wingdings F0FE = checked, F0FC = tick
                isChecked = true;
            }

            // Render checkbox icon (non-mappable)
            htmlBuilder.Append($"<span data-mappable=\"false\" data-id=\"chk-{_checkboxIndex}\" " +
                              $"data-type=\"checkbox\" data-docx-path=\"{parentPath}.r[{runIndex}].sdt\" " +
                              $"style=\"display: inline-block; margin-right: 3px; font-size: 14px; color: #333;\">");
            htmlBuilder.Append(isChecked ? "<i class=\"ti ti-checkbox\"></i>" : "<i class=\"ti ti-square\"></i>");
            htmlBuilder.Append("</span>");

            _checkboxIndex++;

            // IMPORTANT: Also render any text content after checkbox
            foreach (var child in sdtRun.SdtContentRun?.Elements() ?? Enumerable.Empty<OpenXmlElement>())
            {
                if (child is Run run)
                {
                    foreach (var text in run.Descendants<Text>())
                    {
                        if (!string.IsNullOrWhiteSpace(text.Text))
                        {
                            htmlBuilder.Append($"<span data-mappable=\"true\" data-id=\"r-{_paragraphIndex}-{runIndex}-text\" " +
                                              $"data-type=\"run\" data-docx-path=\"{parentPath}.r[{runIndex}].text\">");
                            htmlBuilder.Append(WebUtility.HtmlEncode(text.Text));
                            htmlBuilder.Append("</span>");
                        }
                    }
                }
            }
            return;
        }

        // Not a checkbox - render content normally
        foreach (var child in sdtRun.SdtContentRun?.Elements() ?? Enumerable.Empty<OpenXmlElement>())
        {
            if (child is Run run)
            {
                foreach (var text in run.Descendants<Text>())
                    htmlBuilder.Append(WebUtility.HtmlEncode(text.Text));
            }
        }
    }

    private bool HasCheckboxFormField(FieldChar fieldChar)
    {
        if (fieldChar?.FieldCharType?.Value != FieldCharValues.Begin)
            return false;

        var nextSibling = fieldChar.NextSibling();
        while (nextSibling != null)
        {
            if (nextSibling is Run run)
            {
                var instrText = run.Elements<FieldCode>().FirstOrDefault();
                if (instrText != null && instrText.Text.Contains("FORMCHECKBOX"))
                {
                    return true;
                }
            }
            nextSibling = nextSibling.NextSibling();
        }
        return false;
    }

    private string ParseParagraphProperties(ParagraphProperties? pPr, MainDocumentPart mainPart)
    {
        // Start with Word-like default spacing
        var styles = new List<string> { "margin: 0", "padding: 0", "line-height: 1.15" };
        if (pPr == null) return string.Join("; ", styles) + ";";

        var styleId = pPr.ParagraphStyleId?.Val?.Value;

        // Handle text alignment
        var jc = pPr.Justification ?? GetStyleProperty(styleId, mainPart, s => s.StyleParagraphProperties?.Justification);
        if (jc?.Val?.Value != null)
        {
            var justificationMap = new Dictionary<JustificationValues, string>
            {
                { JustificationValues.Center, "center" },
                { JustificationValues.Right, "right" },
                { JustificationValues.Both, "justify" },
                { JustificationValues.Left, "left" }
            };

            if (justificationMap.TryGetValue(jc.Val.Value, out var alignValue))
            {
                styles.Add($"text-align: {alignValue}");
            }
        }

        // Handle paragraph spacing (before/after)
        var spacingBefore = pPr.SpacingBetweenLines?.Before?.Value;
        var spacingAfter = pPr.SpacingBetweenLines?.After?.Value;
        var lineSpacing = pPr.SpacingBetweenLines?.Line?.Value;

        if (spacingBefore != null)
        {
            // Convert from twentieths of a point to CSS points
            var beforePt = int.Parse(spacingBefore) / 20.0;
            styles.Add($"margin-top: {beforePt}pt");
        }

        if (spacingAfter != null)
        {
            // Convert from twentieths of a point to CSS points
            var afterPt = int.Parse(spacingAfter) / 20.0;
            styles.Add($"margin-bottom: {afterPt}pt");
        }

        if (lineSpacing != null)
        {
            // Handle line spacing - Word uses different units
            var lineRule = pPr.SpacingBetweenLines?.LineRule?.Value;
            if (lineRule == LineSpacingRuleValues.Auto)
            {
                // Auto line spacing - convert to CSS line-height
                var lineHeight = int.Parse(lineSpacing) / 240.0; // 240 = 12pt * 20 (twentieths of point)
                styles.RemoveAll(s => s.StartsWith("line-height:"));
                styles.Add($"line-height: {lineHeight:F2}");
            }
        }

        // Handle indentation for bullets and regular paragraphs
        var indentation = pPr.Indentation;
        if (indentation != null)
        {
            if (indentation.Left?.Value != null)
            {
                var leftIndentPt = int.Parse(indentation.Left.Value) / 20.0;
                styles.Add($"margin-left: {leftIndentPt}pt");
            }

            if (indentation.Right?.Value != null)
            {
                var rightIndentPt = int.Parse(indentation.Right.Value) / 20.0;
                // Limit excessive margin-right to prevent images from being pushed out of view
                // Word documents sometimes have very large right indentation that breaks layout
                var maxRightMargin = 10.0; // Maximum 10pt margin-right
                var limitedRightIndent = Math.Min(rightIndentPt, maxRightMargin);
                styles.Add($"margin-right: {limitedRightIndent}pt");
            }

            if (indentation.FirstLine?.Value != null)
            {
                var firstLineIndentPt = int.Parse(indentation.FirstLine.Value) / 20.0;
                styles.Add($"text-indent: {firstLineIndentPt}pt");
            }
        }

        // Handle numbering properties (bullets/numbering)
        var numberingProperties = pPr.NumberingProperties;
        if (numberingProperties != null && indentation?.Left?.Value == null)
        {
            // Default bullet indentation if not explicitly set
            styles.Add("margin-left: 18pt"); // Word default bullet indent
        }

        return string.Join("; ", styles) + ";";
    }

    private string ParseRunProperties(RunProperties? rPr, MainDocumentPart mainPart)
    {
        var styles = new List<string>();
        if (rPr == null) return string.Empty;

        var styleId = rPr.RunStyle?.Val?.Value;

        // Bold
        var isBold = rPr.Bold ?? GetStyleProperty(styleId, mainPart, s => s.StyleRunProperties?.Bold);
        if (isBold != null && (isBold.Val == null || isBold.Val.Value))
            styles.Add("font-weight: bold");

        // Italic
        var isItalic = rPr.Italic ?? GetStyleProperty(styleId, mainPart, s => s.StyleRunProperties?.Italic);
        if (isItalic != null && (isItalic.Val == null || isItalic.Val.Value))
            styles.Add("font-style: italic");

        // Font Size (CRITICAL: preserve original size)
        var fontSize = rPr.FontSize ?? GetStyleProperty(styleId, mainPart, s => s.StyleRunProperties?.FontSize);
        if (fontSize?.Val?.Value != null)
        {
            // Word uses half-points, so divide by 2
            var sizeInPt = float.Parse(fontSize.Val.Value) / 2.0f;
            styles.Add($"font-size: {sizeInPt}pt");
        }
        else
        {
            // Default font size to prevent oversized text
            styles.Add("font-size: 11pt");
        }

        // Font Family
        var fontFamily = rPr.RunFonts;
        if (fontFamily != null)
        {
            var font = fontFamily.Ascii?.Value ?? fontFamily.HighAnsi?.Value ?? fontFamily.EastAsia?.Value;
            if (!string.IsNullOrEmpty(font))
            {
                // Normalize known "bold" font names
                if (font.Equals("Times New Roman Bold", StringComparison.OrdinalIgnoreCase))
                    font = "Times New Roman";
                else if (font.Equals("Arial Bold", StringComparison.OrdinalIgnoreCase))
                    font = "Arial";

                // Use double quotes for font names with spaces to avoid CSS parsing issues
                if (font.Contains(" "))
                {
                    styles.Add($"font-family: \"{font}\", Arial, sans-serif");
                }
                else
                {
                    styles.Add($"font-family: {font}, Arial, sans-serif");
                }
            }
        }

        // Text Color
        var color = rPr.Color;
        if (color != null && !string.IsNullOrEmpty(color.Val?.Value) && color.Val.Value != "auto")
        {
            styles.Add($"color: #{color.Val.Value}");
        }

        // Background/Highlight
        var highlight = rPr.Highlight;
        if (highlight?.Val?.Value != null && highlight.Val.Value != HighlightColorValues.None)
        {
            styles.Add($"background-color: {highlight.Val.Value.ToString().ToLower()}");
        }

        // Underline
        var underline = rPr.Underline;
        if (underline != null && underline.Val?.Value != UnderlineValues.None)
        {
            styles.Add("text-decoration: underline");
        }

        return string.Join("; ", styles) + (styles.Count > 0 ? ";" : "");
    }

    private T? GetStyleProperty<T>(string? styleId, MainDocumentPart mainPart, Func<Style, T?> propertySelector) where T : OpenXmlElement
    {
        if (string.IsNullOrEmpty(styleId) || mainPart.StyleDefinitionsPart?.Styles == null)
            return null;

        var style = mainPart.StyleDefinitionsPart.Styles.Elements<Style>().FirstOrDefault(s => s.StyleId == styleId);

        if (style == null) return null;

        var prop = propertySelector(style);
        if (prop != null)
            return prop;

        return GetStyleProperty(style.BasedOn?.Val?.Value, mainPart, propertySelector);
    }

    /// <summary>
    /// Gets the default run style from the document's styles part.
    /// </summary>
    private string GetDefaultRunStyle(MainDocumentPart mainPart)
    {
        // First, try to get the default run properties from the document defaults.
        // FIX: Access the RunProperties element correctly using GetFirstChild<T>()
        var rPrDefaultContainer = mainPart.StyleDefinitionsPart?.Styles?.DocDefaults?.RunPropertiesDefault;
        if (rPrDefaultContainer != null)
        {
            var defaultRunProps = rPrDefaultContainer.GetFirstChild<RunProperties>();
            if (defaultRunProps != null)
            {
                // Use the existing ParseRunProperties to convert OpenXML properties to a CSS string.
                // We clone it to avoid any potential modification issues.
                string defaultStyle = ParseRunProperties(new RunProperties(defaultRunProps.OuterXml), mainPart);
                if (!string.IsNullOrEmpty(defaultStyle)) return defaultStyle;
            }
        }

        // As a fallback, try to get the run properties from the "Normal" style.
        var normalStyle = mainPart.StyleDefinitionsPart?.Styles?.Elements<Style>()
            .FirstOrDefault(s => s.StyleId == "Normal" && s.Type == StyleValues.Paragraph);
        var rPrNormal = normalStyle?.StyleRunProperties;
        if (rPrNormal != null)
        {
            string normalStyleCss = ParseRunProperties(new RunProperties(rPrNormal.OuterXml), mainPart);
            if (!string.IsNullOrEmpty(normalStyleCss)) return normalStyleCss;
        }

        // If all else fails, return a hardcoded default.
        return "font-family: 'Times New Roman', Times, serif; font-size: 11pt;";
    }

    private string ParseTableProperties(TableProperties? tblPr)
    {
        if (tblPr == null) return "width: 100%; table-layout: fixed; border-collapse: collapse;";

        var styles = new List<string> { "width: 100%", "table-layout: fixed", "border-collapse: collapse" };

        // Enhanced border handling for nested tables
        var borders = tblPr.TableBorders;
        if (borders != null && HasVisibleBorders(borders))
        {
            // Add outer border for table
            styles.Add("border: 1px solid #000");
        }

        // Ensure nested tables also get proper border styling
        var insideH = borders?.InsideHorizontalBorder;
        var insideV = borders?.InsideVerticalBorder;
        if ((insideH?.Val?.Value != BorderValues.None && insideH?.Val?.Value != BorderValues.Nil) ||
            (insideV?.Val?.Value != BorderValues.None && insideV?.Val?.Value != BorderValues.Nil))
        {
            // This will be handled by cell borders, but ensure collapse is set
            styles.RemoveAll(s => s.StartsWith("border-collapse:"));
            styles.Add("border-collapse: collapse");
        }

        return string.Join("; ", styles);
    }

    private static bool HasVisibleBorders(TableBorders borders)
    {
        // Check if any border is visible (not none or nil)
        return (borders.TopBorder?.Val?.Value != BorderValues.None && borders.TopBorder?.Val?.Value != BorderValues.Nil) ||
               (borders.BottomBorder?.Val?.Value != BorderValues.None && borders.BottomBorder?.Val?.Value != BorderValues.Nil) ||
               (borders.LeftBorder?.Val?.Value != BorderValues.None && borders.LeftBorder?.Val?.Value != BorderValues.Nil) ||
               (borders.RightBorder?.Val?.Value != BorderValues.None && borders.RightBorder?.Val?.Value != BorderValues.Nil);
    }

    private static bool HasVisibleCellBorders(TableCellBorders borders)
    {
        // Check if any cell border is visible (not none or nil)
        return (borders.TopBorder?.Val?.Value != BorderValues.None && borders.TopBorder?.Val?.Value != BorderValues.Nil) ||
               (borders.BottomBorder?.Val?.Value != BorderValues.None && borders.BottomBorder?.Val?.Value != BorderValues.Nil) ||
               (borders.LeftBorder?.Val?.Value != BorderValues.None && borders.LeftBorder?.Val?.Value != BorderValues.Nil) ||
               (borders.RightBorder?.Val?.Value != BorderValues.None && borders.RightBorder?.Val?.Value != BorderValues.Nil);
    }

    private string ParseTableCellProperties(TableCellProperties? tcPr)
    {
        if (tcPr == null) return "padding: 4px; vertical-align: top; border: 1px solid #ccc;";

        var styles = new List<string> { "padding: 4px", "vertical-align: top" };

        // Enhanced border handling for nested table cells
        var borders = tcPr.TableCellBorders;
        if (borders != null && HasVisibleCellBorders(borders))
        {
            // Add specific borders based on DOCX cell borders
            var borderParts = new List<string>();

            if (borders.TopBorder?.Val?.Value != BorderValues.None && borders.TopBorder?.Val?.Value != BorderValues.Nil)
                borderParts.Add("border-top: 1px solid #000");
            if (borders.BottomBorder?.Val?.Value != BorderValues.None && borders.BottomBorder?.Val?.Value != BorderValues.Nil)
                borderParts.Add("border-bottom: 1px solid #000");
            if (borders.LeftBorder?.Val?.Value != BorderValues.None && borders.LeftBorder?.Val?.Value != BorderValues.Nil)
                borderParts.Add("border-left: 1px solid #000");
            if (borders.RightBorder?.Val?.Value != BorderValues.None && borders.RightBorder?.Val?.Value != BorderValues.Nil)
                borderParts.Add("border-right: 1px solid #000");

            if (borderParts.Count > 0)
                styles.AddRange(borderParts);
            else
                styles.Add("border: 1px solid #000"); // Fallback for all borders
        }
        else
        {
            // Default light border for nested tables to maintain structure
            styles.Add("border: 1px solid #ccc");
        }

        // Do NOT set width here - colgroup handles width proportions
        // Only handle borders, padding, vertical alignment

        // Vertical alignment
        var vAlign = tcPr.TableCellVerticalAlignment;
        if (vAlign?.Val?.Value != null)
        {
            var alignmentMap = new Dictionary<TableVerticalAlignmentValues, string>
            {
                { TableVerticalAlignmentValues.Top, "top" },
                { TableVerticalAlignmentValues.Center, "middle" },
                { TableVerticalAlignmentValues.Bottom, "bottom" }
            };

            if (alignmentMap.TryGetValue(vAlign.Val.Value, out var alignValue))
            {
                styles.RemoveAll(s => s.StartsWith("vertical-align:"));
                styles.Add($"vertical-align: {alignValue}");
            }
        }

        // Background color/shading
        var shading = tcPr.Shading;
        if (shading?.Fill?.Value != null && shading.Fill.Value != "auto")
        {
            var fillColor = shading.Fill.Value;
            // Convert DOCX color format to CSS
            if (fillColor.Length == 6 && !fillColor.StartsWith("#"))
            {
                styles.Add($"background-color: #{fillColor}");
            }
            else if (fillColor.Length == 6)
            {
                styles.Add($"background-color: {fillColor}");
            }
        }

        return string.Join("; ", styles);
    }

    /// <summary>
    /// Highlights placeholders in the format <<placeholder>> with CSS class for visual distinction
    /// </summary>
    /// <param name="text">Text content to process</param>
    /// <param name="isViewMode">True for view-only mode, false for mapping mode</param>
    /// <returns>HTML content with highlighted placeholders</returns>
    private static string HighlightPlaceholders(string text, bool isViewMode = false)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        // Đối với chế độ chỉ xem, chúng ta không cần các span tương tác, chỉ cần hiển thị text.
        if (isViewMode)
        {
            return WebUtility.HtmlEncode(text);
        }
        
        // Đối với chế độ mapping, tạo các span tương tác ở phía server theo đúng ý đồ thiết kế ban đầu.
        // Điều này khắc phục lỗi placeholder không hiển thị đúng.
        var placeholderRegex = new Regex(@"(<<[a-zA-Z0-9_]+>>)");
        
        // Tách chuỗi text bởi các placeholder, nhưng vẫn giữ lại các placeholder trong kết quả.
        var parts = placeholderRegex.Split(text);
        var sb = new StringBuilder();

        foreach (var part in parts)
        {
            if (placeholderRegex.IsMatch(part))
            {
                // Phần này là một placeholder, ví dụ: "<<FieldName>>"
                string fieldName = part.Trim('<', '>');
                
                // Tạo thẻ span cuối cùng. Nội dung của thẻ span là chính chuỗi placeholder,
                // nhưng được mã hóa HTML để hiển thị chính xác và an toàn.
                // Ví dụ: <span ...>&lt;&lt;FieldName&gt;&gt;</span>
                sb.Append($"<span class=\"field-placeholder\" contenteditable=\"false\" data-field-name=\"{fieldName}\">{WebUtility.HtmlEncode(part)}</span>");
            }
            else
            {
                // Phần này là text thông thường, chỉ cần mã hóa HTML.
                sb.Append(WebUtility.HtmlEncode(part));
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Decode special font characters (Wingdings, Symbol, etc.) to their Unicode equivalents
    /// This maps font-specific character codes to proper HTML entities based on actual numbering.xml content
    /// </summary>
    private string DecodeSpecialFontCharacter(string? lvlText, string fontName, bool isViewMode)
    {
        if (string.IsNullOrEmpty(lvlText))
            return "";

        // Font character mapping for special fonts - based on actual Word font character codes
        var fontCharacterMap = new Dictionary<string, Dictionary<string, string>>
        {
            ["Wingdings"] = new Dictionary<string, string>
            {
                // Common Wingdings bullet characters (hex codes from Word)
                ["F0B7"] = isViewMode ? "-" : "&#9632;",  // ■ Black square
                ["F0B8"] = isViewMode ? "-" : "&#9642;",  // ▪ Black small square
                ["F06C"] = isViewMode ? "-" : "&#9679;",  // ● Black circle
                ["F06D"] = isViewMode ? "-" : "&#9675;",  // ○ White circle
                ["F0D8"] = isViewMode ? "-" : "&#9830;",  // ♦ Diamond
                ["F0FC"] = isViewMode ? "-" : "&#9642;",  // ▪ Black small square
                ["F0FE"] = isViewMode ? "-" : "&#9632;",  // ■ Black square
                ["F0A7"] = isViewMode ? "-" : "&#9702;",  // ◦ White bullet
                ["F0A8"] = isViewMode ? "-" : "&#9642;",  // ▪ Black small square
                // Add more Wingdings mappings as needed
            },
            ["Wingdings 2"] = new Dictionary<string, string>
            {
                // Common Wingdings 2 bullet characters
                ["F020"] = isViewMode ? "-" : "&#9632;",  // ■ Black square
                ["F021"] = isViewMode ? "-" : "&#9642;",  // ▪ Black small square
                ["F022"] = isViewMode ? "-" : "&#9679;",  // ● Black circle
                ["F023"] = isViewMode ? "-" : "&#9675;",  // ○ White circle
                ["F024"] = isViewMode ? "-" : "&#9830;",  // ♦ Diamond
                ["F025"] = isViewMode ? "-" : "&#9642;",  // ▪ Black small square
                ["2A"] = isViewMode ? "□" : "&#9633;",    // □ White square (Wingdings 2 code 42)
                ["F02A"] = isViewMode ? "□" : "&#9633;",  // □ White square (alternative hex format)
                // Add more Wingdings 2 mappings as needed
            },
            ["Symbol"] = new Dictionary<string, string>
            {
                // Common Symbol font bullet characters
                ["F0B7"] = isViewMode ? "-" : "&#8226;",  // • Bullet
                ["F0B8"] = isViewMode ? "-" : "&#8226;",  // • Bullet
                ["F06C"] = isViewMode ? "-" : "&#9679;",  // ● Black circle
                ["F06D"] = isViewMode ? "-" : "&#9675;",  // ○ White circle
                ["F0D8"] = isViewMode ? "-" : "&#9830;",  // ♦ Diamond
                ["F0FC"] = isViewMode ? "-" : "&#9642;",  // ▪ Black small square
                ["F0FE"] = isViewMode ? "-" : "&#9632;",  // ■ Black square
                // Add more Symbol mappings as needed
            }
        };

        // Try to find exact font and character code match
        if (fontCharacterMap.TryGetValue(fontName, out var characterMap))
        {
            if (characterMap.TryGetValue(lvlText.ToUpper(), out var unicodeChar))
            {
                return unicodeChar;
            }
        }

        // If no exact match found, return empty string to use fallback
        return "";
    }

    /// <summary>
    /// Convert bullet text to safe HTML representation
    /// </summary>
    private string ConvertBulletToHtml(string bulletText, bool isViewMode)
    {
        if (string.IsNullOrEmpty(bulletText))
            return isViewMode ? "-" : "&#8226;";

        // Use existing IdentifyBulletSymbol method for consistent bullet handling
        var (isBullet, bulletSymbol) = IdentifyBulletSymbol(bulletText, isViewMode);
        if (isBullet)
        {
            return bulletSymbol;
        }

        // If not recognized as a bullet symbol, return as-is (could be custom text)
        return bulletText;
    }

    // [SỬA ĐỔI] Thêm hàm helper để tính SHA256
    private static string ComputeSha256Hash(string rawData)
    {
        if (string.IsNullOrEmpty(rawData))
        {
            return string.Empty;
        }
        
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
