#nullable enable

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Drawing; // a:*
using WpDrawing = DocumentFormat.OpenXml.Drawing.Wordprocessing; // wp:*
using W = DocumentFormat.OpenXml.Wordprocessing; // alias rõ ràng cho Wordprocessing
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions; // Thêm thư viện Regex để sử dụng
using System.Security.Cryptography; // [SỬA ĐỔI] Thêm using cho việc hash

namespace CTOM.Services
{
    /// <summary>
    /// [NÂNG CẤP LỚN] Service này được tái cấu trúc để hỗ trợ hệ thống mapping mới.
    /// - Chức năng chính: Chuyển đổi file DOCX thành HTML có cấu trúc.
    /// - Nâng cấp quan trọng:
    ///   1. Tự động gán ID duy nhất và bền vững (w14:paraId) cho tất cả các đoạn văn.
    ///   2. Nhúng các ID này và URI của document part vào HTML output dưới dạng thuộc tính data-*.
    ///   Điều này cung cấp cho client một "dấu vân tay" chính xác cho mỗi đoạn văn,
    ///   giải quyết triệt để vấn đề định vị không ổn định.
    /// </summary>
    public class DocxToStructuredHtmlService
    {
        private delegate void ElementHandler(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart currentPart);

        private readonly Dictionary<Type, ElementHandler> _handlers = new();
        private readonly List<int> _footnoteQueue = new();
        private readonly List<int> _endnoteQueue = new();

        // Ngữ cảnh field (w:fldChar/w:instrText) để nhận diện FORMCHECKBOX
        private enum FieldContextKind { Unknown, FormCheckbox, Other }
        private sealed class FieldContext
        {
            public FieldContextKind Kind = FieldContextKind.Unknown;
            public StringBuilder Instr = new();
            public bool Emitted = false;
        }

        private static bool IsWingdings2ForLevel(W.Level lvl)
        {
            // Theo OpenXML: w:lvl/w:rPr trong numbering là NumberingSymbolRunProperties
            var rpr = lvl.NumberingSymbolRunProperties;
            var rFonts = rpr?.RunFonts;
            var fontName = rFonts?.Ascii?.Value
                           ?? rFonts?.HighAnsi?.Value
                           ?? rFonts?.ComplexScript?.Value;
            return !string.IsNullOrEmpty(fontName) && fontName.Equals("Wingdings 2", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPrivateUseGlyph(string? s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            // Lấy ký tự đầu tiên không phải placeholder số
            var ch = s[0];
            int cp = char.ConvertToUtf32(s, 0);
            // Private Use Area (PUA): U+E000..U+F8FF
            return cp >= 0xE000 && cp <= 0xF8FF;
        }

        private static bool ShouldKeepOriginalBullet(string? s)
        {
            // KHÔNG giữ nguyên nếu là ký tự vùng Private Use (PUA) – thường là Wingdings/Symbol
            // Các glyph này không có đảm bảo font, cần chuẩn hóa sang Unicode an toàn (ví dụ '□').
            if (IsPrivateUseGlyph(s)) return false;

            // Giữ nguyên nếu lvlText là một bullet phổ biến (ASCII/Unicode chuẩn: -, +, *, •, ·, –, —, ◦, ▪, ■, ●, ...)
            return IsBulletLike(s);
        }
        private readonly Stack<FieldContext> _fieldStack = new();

        public DocxToStructuredHtmlService()
        {
            InitHandlers();
        }

        private static string? NormalizeNumFmt(string? numFmt, string? lvlText)
        {
            // Loại các giá trị không hợp lệ từ SDK: "NumberFormatValues { }" hoặc chuỗi chứa '{'
            if (!string.IsNullOrWhiteSpace(numFmt))
            {
                var trimmed = numFmt.Trim();
                if (trimmed.Contains('{', StringComparison.Ordinal))
                {
                    numFmt = null;
                }
            }

            // Suy luận bullet từ lvlText nếu numFmt trống
            if (string.IsNullOrWhiteSpace(numFmt) && IsBulletLike(lvlText))
            {
                return "bullet";
            }

            if (string.IsNullOrWhiteSpace(numFmt)) return null;

            // Chuẩn hóa lowercase & một số alias
            var nf = numFmt.Trim().ToLowerInvariant();
            return nf switch
            {
                "bullet" => "bullet",
                "decimal" => "decimal",
                "lowerletter" => "lowerletter",
                "lowerlatin" => "lowerletter",
                "upperletter" => "upperletter",
                "upperlatin" => "upperletter",
                _ => nf
            };
        }

        private void InitHandlers()
        {
            _handlers[typeof(W.Paragraph)] = HandleParagraph;
            _handlers[typeof(W.Run)] = HandleRun;
            _handlers[typeof(W.Text)] = HandleText;
            _handlers[typeof(W.SymbolChar)] = HandleSymbol;      // w:sym
            _handlers[typeof(W.Table)] = HandleTable;
            _handlers[typeof(W.TableRow)] = HandleTableRow;
            _handlers[typeof(W.TableCell)] = HandleTableCell;
            _handlers[typeof(W.Break)] = HandleBreak;
            _handlers[typeof(W.TabChar)] = HandleTab;
            _handlers[typeof(W.Hyperlink)] = HandleHyperlink;
            _handlers[typeof(W.BookmarkStart)] = HandleBookmarkStart;
            _handlers[typeof(W.BookmarkEnd)] = HandleBookmarkEnd;
            _handlers[typeof(W.SimpleField)] = HandleFieldSimple;
            _handlers[typeof(W.FieldChar)] = HandleFieldChar;
            // Đăng ký handler cho w:instrText (InstrText/InstructionText tùy phiên bản SDK)
            var instrTextType = Type.GetType("DocumentFormat.OpenXml.Wordprocessing.InstrText, DocumentFormat.OpenXml")
                                 ?? Type.GetType("DocumentFormat.OpenXml.Wordprocessing.InstructionText, DocumentFormat.OpenXml");
            if (instrTextType != null)
                _handlers[instrTextType] = HandleInstrText;
            _handlers[typeof(W.FootnoteReference)] = HandleFootnoteRef;
            _handlers[typeof(W.EndnoteReference)] = HandleEndnoteRef;
            _handlers[typeof(W.CommentReference)] = HandleCommentRef;
            _handlers[typeof(W.CommentRangeStart)] = HandleCommentRangeStart;
            _handlers[typeof(W.CommentRangeEnd)] = HandleCommentRangeEnd;
            _handlers[typeof(W.SdtRun)] = HandleSdtRun;
            _handlers[typeof(W.SdtBlock)] = HandleSdtBlock;
            _handlers[typeof(W.SdtCell)] = HandleSdtCell;
            _handlers[typeof(Drawing)] = HandleDrawing;

            // BỎ QUA (KHÔNG RENDER) CÁC PHẦN TỬ THUỘC NHÓM PROPERTIES HOẶC TRANG TRÍ
            _handlers[typeof(W.ParagraphProperties)] = IgnoreElement; // w:pPr
            _handlers[typeof(W.RunProperties)] = IgnoreElement;       // w:rPr
            _handlers[typeof(W.TableProperties)] = IgnoreElement;     // w:tblPr
            _handlers[typeof(W.TableRowProperties)] = IgnoreElement;  // w:trPr
            _handlers[typeof(W.TableCellProperties)] = IgnoreElement; // w:tcPr
            _handlers[typeof(W.SectionProperties)] = IgnoreElement;   // w:sectPr
            _handlers[typeof(W.TableGrid)] = IgnoreElement;           // w:tblGrid
            _handlers[typeof(W.ProofError)] = IgnoreElement;          // w:proofErr
            _handlers[typeof(W.NoProof)] = IgnoreElement;             // w:noProof
        }

        private void ProcessElement(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart currentPart)
        {
            if (el == null) return;

            if (_handlers.TryGetValue(el.GetType(), out var handler))
            {
                handler(el, sb, mainPart, currentPart);
                return;
            }

            // Mặc định: bỏ qua phần tử không hỗ trợ (tránh hiển thị XML thô ra UI)
            // Nếu sau này cần debug, có thể thêm cờ để bật lại hành vi render XML thô.
            return;
        }

        private void ProcessChildren(OpenXmlElement parent, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart currentPart)
        {
            foreach (var child in parent.ChildElements)
                ProcessElement(child, sb, mainPart, currentPart);
        }

        private void HandleParagraph(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart currentPart)
        {
            var p = (W.Paragraph)el;
            var paraId = p.ParagraphId?.Value ?? "";
            var partUri = (currentPart ?? (OpenXmlPart)mainPart)?.Uri?.ToString() ?? "";

            // pPr cơ bản: căn lề và thụt dòng
            var classes = new List<string> { "w-p" };
            var styleSb = new StringBuilder();

            var pPr = p.ParagraphProperties;
            string? paraStyleIdForDebug = pPr?.ParagraphStyleId?.Val?.Value;
            string jcSource = "none";
            string? jcRaw = null;

            // --- ALIGNMENT (áp dụng cả khi pPr=null) ---
            string? jcLower = null;
            var styleId = pPr?.ParagraphStyleId?.Val?.Value;
            // Ưu tiên: pPr.Justification; nếu không có, lấy từ style chain
            W.Justification? jcElem = pPr?.Justification ?? GetStyleProperty(styleId, mainPart, s => s.StyleParagraphProperties?.Justification);
            var jcFromPPrOrStyle = GetJustificationString(jcElem);
            if (!string.IsNullOrEmpty(jcFromPPrOrStyle))
            {
                jcRaw = jcFromPPrOrStyle;
                jcLower = jcRaw.ToLowerInvariant();
                jcSource = (pPr?.Justification != null) ? "pPr" : (!string.IsNullOrEmpty(styleId) ? $"style:{styleId}" : "style");
            }
            else
            {
                var jcDefault = ResolveDefaultParagraphJustification(mainPart);
                if (!string.IsNullOrEmpty(jcDefault))
                {
                    jcRaw = jcDefault;
                    jcLower = jcRaw.ToLowerInvariant();
                    jcSource = "defaultStyle";
                }

                if (string.IsNullOrEmpty(jcLower))
                {
                    // 4) Fallback nữa: docDefaults
                    var jcDocDefaults = ResolveDocDefaultsParagraphJustification(mainPart);
                    if (!string.IsNullOrEmpty(jcDocDefaults))
                    {
                        jcRaw = jcDocDefaults;
                        jcLower = jcRaw.ToLowerInvariant();
                        jcSource = "docDefaults";
                    }
                }
            }

            if (!string.IsNullOrEmpty(jcLower))
            {
                switch (jcLower)
                {
                    case "center": classes.Add("w-align-center"); break;
                    case "right": classes.Add("w-align-right"); break;
                    case "both": classes.Add("w-align-justify"); break; // justify
                    case "justify": classes.Add("w-align-justify"); break;
                    case "distribute": classes.Add("w-align-justify"); break;
                    case "start": classes.Add("w-align-left"); break;
                    case "end": classes.Add("w-align-right"); break;
                }
            }

            // --- INDENTATION: chỉ khi có pPr ---
            if (pPr != null)
            {
                // Indentation (twips -> px). 1 twip = 1/1440 inch; px = inch*96 => px = twip * 96 / 1440 = twip * 0.0666667
                double TwipToPx(long twip) => Math.Round(twip * 0.0666667, 2);

                var ind = pPr.Indentation;
                if (ind != null)
                {
                    if (ind.Left != null && long.TryParse(ind.Left.Value, out var leftTw))
                        styleSb.Append($"margin-left:{TwipToPx(leftTw)}px;");
                    if (ind.Right != null && long.TryParse(ind.Right.Value, out var rightTw))
                        styleSb.Append($"margin-right:{TwipToPx(rightTw)}px;");
                    if (ind.FirstLine != null && long.TryParse(ind.FirstLine.Value, out var flTw))
                        styleSb.Append($"text-indent:{TwipToPx(flTw)}px;");
                    if (ind.Hanging != null && long.TryParse(ind.Hanging.Value, out var hangTw))
                        styleSb.Append($"text-indent:-{TwipToPx(hangTw)}px;");
                }
            }

            // --- NUMBERING (numPr) ---
            int? numId = null; int? ilvl = null; string? numFmt = null; string? lvlText = null; int? startVal = null;
            TryResolveNumbering(p, mainPart, out numId, out ilvl, out numFmt, out lvlText, out startVal);
            var nfmt = NormalizeNumFmt(numFmt, lvlText);
            if (numId.HasValue && ilvl.HasValue)
            {
                classes.Add("w-num");
                classes.Add($"w-lvl-{ilvl.Value}");
                if (!string.IsNullOrEmpty(nfmt))
                {
                    classes.Add($"w-num-{nfmt}");
                }
            }

            // Phát hiện đoạn chỉ chứa checkbox đơn lẻ (□ hoặc ☑) để ẩn khỏi UI (tránh ô vuông lơ lửng)
            bool isCheckboxOnly = IsCheckboxOnlyParagraph(p);

            var classAttr = string.Join(' ', classes);
            sb.Append($"<p class=\"{classAttr}\" data-oxml=\"w:p\" data-paragraph-id=\"{WebUtility.HtmlEncode(paraId)}\" data-part-uri=\"{WebUtility.HtmlEncode(partUri)}\"");
            if (!string.IsNullOrEmpty(paraStyleIdForDebug))
                sb.Append($" data-style-id=\"{WebUtility.HtmlEncode(paraStyleIdForDebug)}\"");
            if (!string.IsNullOrEmpty(jcSource) && jcSource != "none")
                sb.Append($" data-jc-source=\"{WebUtility.HtmlEncode(jcSource)}\"");
            sb.Append($" data-jc-val=\"{WebUtility.HtmlEncode(jcRaw ?? "none")}\"");
            if (numId.HasValue)
                sb.Append($" data-num-id=\"{numId.Value}\"");
            if (ilvl.HasValue)
                sb.Append($" data-ilvl=\"{ilvl.Value}\"");
            if (!string.IsNullOrEmpty(nfmt))
            {
                sb.Append($" data-num-fmt=\"{WebUtility.HtmlEncode(nfmt.ToLowerInvariant())}\"");
                sb.Append($" data-num-fmt-lc=\"{WebUtility.HtmlEncode(nfmt.ToLowerInvariant())}\"");
            }
            if (!string.IsNullOrEmpty(lvlText))
                sb.Append($" data-lvl-text=\"{WebUtility.HtmlEncode(lvlText)}\"");
            if (startVal.HasValue)
                sb.Append($" data-start=\"{startVal.Value}\"");
            if (isCheckboxOnly)
                sb.Append(" data-checkbox-only=\"true\"");
            if (styleSb.Length > 0)
                sb.Append($" style=\"{WebUtility.HtmlEncode(styleSb.ToString())}\"");
            sb.Append(">");
            // Chèn soft-gap giữa các w:r liền kề nếu cần (không thay đổi nội dung/offset)
            bool usedSoftGap = ProcessParagraphChildrenWithSoftGaps(p, sb, mainPart, currentPart ?? mainPart);
            if (usedSoftGap)
            {
                // Gắn cờ để CSS không áp dụng fallback margin
                // (Chèn trực tiếp thuộc tính vào phần tử vừa được mở)
                // Không thể sửa thuộc tính sau khi đã append, nên thêm như data-attr trong close tag thông qua 1 marker.
            }
            sb.Append("</p>");
        }

        // Render children của paragraph và chèn soft-gap giữa 2 run liền kề khi thiếu khoảng trắng.
        // Trả về true nếu có chèn ít nhất một soft-gap.
        private bool ProcessParagraphChildrenWithSoftGaps(W.Paragraph p, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart currentPart)
        {
            bool injectedAny = false;
            var children = p.ChildElements.ToList();
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                ProcessElement(child, sb, mainPart, currentPart);

                if (i + 1 < children.Count)
                {
                    var next = children[i + 1];
                    if (child is W.Run r1 && next is W.Run r2)
                    {
                        if (NeedSoftGapBetween(r1, r2))
                        {
                            // Soft-gap: phần tử rỗng chỉ-UI, không thêm ký tự
                            sb.Append("<span class=\"w-soft-gap\" aria-hidden=\"true\"></span>");
                            injectedAny = true;
                        }
                    }
                }
            }

            return injectedAny;
        }

        // Quyết định có cần soft-gap giữa 2 run liền kề không: khi bên trái kết thúc bằng chữ/số,
        // bên phải bắt đầu bằng chữ/số, và không có khoảng trắng.
        private static bool NeedSoftGapBetween(W.Run left, W.Run right)
        {
            char? last = GetLastVisibleChar(left);
            char? first = GetFirstVisibleChar(right);
            if (last == null || first == null) return false;

            // Nếu đã có khoảng trắng ở biên, không cần
            if (char.IsWhiteSpace(last.Value) || char.IsWhiteSpace(first.Value)) return false;

            // Bỏ qua khi phải là dấu câu (không cần space trước dấu)
            if (IsPunctuationLeading(first.Value)) return false;

            // Chỉ chèn khi cả hai phía là chữ/số (tách từ) để tránh chèn giữa cùng một từ
            if ((char.IsLetterOrDigit(last.Value) || IsVietnameseLetter(last.Value))
                && (char.IsLetterOrDigit(first.Value) || IsVietnameseLetter(first.Value)))
            {
                return true;
            }
            return false;
        }

        private static char? GetLastVisibleChar(W.Run r)
        {
            var t = r.Descendants<W.Text>().LastOrDefault();
            if (t == null || string.IsNullOrEmpty(t.Text)) return null;
            return t.Text.Last();
        }

        private static char? GetFirstVisibleChar(W.Run r)
        {
            var t = r.Descendants<W.Text>().FirstOrDefault();
            if (t == null || string.IsNullOrEmpty(t.Text)) return null;
            return t.Text.First();
        }

        private static bool IsPunctuationLeading(char c)
        {
            // Một số dấu câu thường gặp không cần khoảng cách trước
            return c == ':' || c == ';' || c == ',' || c == '.' || c == ')' || c == ']' || c == '!' || c == '?' || c == '"' || c == '»';
        }

        private static bool IsVietnameseLetter(char c)
        {
            // Đơn giản: coi như mọi ký tự Letter theo Unicode là chữ (bao gồm tiếng Việt có dấu)
            return char.IsLetter(c);
        }

        // Xác định đoạn chỉ chứa checkbox đơn (ví dụ: '□' hoặc '☑'), bỏ qua field/bookmark và khoảng trắng
        private static bool IsCheckboxOnlyParagraph(W.Paragraph p)
        {
            // Thu thập mọi node W.Text
            var texts = p.Descendants<W.Text>().Select(t => t.Text ?? string.Empty);
            if (!texts.Any()) return false;

            var combined = string.Concat(texts);
            if (string.IsNullOrWhiteSpace(combined)) return false;

            var visible = combined.Trim();
            // Một số ký tự checkbox phổ biến
            return string.Equals(visible, "\u25A1") // □
                   || string.Equals(visible, "\u2611") // ☑
                   || string.Equals(visible, "□", StringComparison.Ordinal)
                   || string.Equals(visible, "☑", StringComparison.Ordinal);
        }

        private static void TryResolveNumbering(W.Paragraph p, MainDocumentPart mainPart,
            out int? numId, out int? ilvl, out string? numFmt, out string? lvlText, out int? startVal)
        {
            numId = null; ilvl = null; numFmt = null; lvlText = null; startVal = null;
            var pPr = p.ParagraphProperties;
            var numPr = pPr?.NumberingProperties;
            if (numPr == null) return;

            if (numPr.NumberingId?.Val?.Value != null)
                numId = (int)numPr.NumberingId.Val.Value;
            if (numPr.NumberingLevelReference?.Val?.Value != null)
                ilvl = (int)numPr.NumberingLevelReference.Val.Value;
            if (!numId.HasValue || !ilvl.HasValue) return;
            var numIdLocal = numId.Value;
            var ilvlLocal = ilvl.Value;

            var numbering = mainPart.NumberingDefinitionsPart?.Numbering;
            if (numbering == null) return;

            // numId -> abstractNumId
            var num = numbering.Elements<W.NumberingInstance>()
                               .FirstOrDefault(n => n.NumberID?.Value == numIdLocal);
            if (num == null) return;
            var absId = num.AbstractNumId?.Val?.Value;
            if (absId == null) return;

            var abs = numbering.Elements<W.AbstractNum>()
                               .FirstOrDefault(a => a.AbstractNumberId?.Value == absId);
            if (abs == null) return;

            var lvl = abs.Elements<W.Level>().FirstOrDefault(l => l.LevelIndex?.Value == ilvlLocal);
            if (lvl == null) return;

            var nf = lvl.NumberingFormat?.Val; // EnumValue<NumberFormatValues>
            if (nf != null && nf.HasValue)
                numFmt = nf.Value.ToString(); // e.g., Bullet, Decimal, LowerLetter
            lvlText = lvl.LevelText?.Val?.Value;      // e.g., "%1.", "•", "-", or Wingdings PUA glyph

            // Nếu bullet của level dùng font Wingdings 2 hoặc lvlText là ký tự thuộc Private Use Area (Wingdings/Symbol),
            // chuẩn hóa về ký tự Unicode ô vuông rỗng để hiển thị đúng và ổn định,
            // TRỪ KHI lvlText vốn là các ký tự ASCII đơn giản mà ta cần giữ nguyên như '-', '+', '*'
            if ((IsWingdings2ForLevel(lvl) || IsPrivateUseGlyph(lvlText)) && !ShouldKeepOriginalBullet(lvlText))
            {
                // Dùng một ký tự duy nhất để không thay đổi offset
                lvlText = "\u25A1"; // □
                // Đảm bảo front-end nhận diện là bullet để áp dụng ::before
                numFmt = "bullet";
            }

            // SUY LUẬN BULLET khi numFmt không có nhưng lvlText là ký hiệu
            if (string.IsNullOrEmpty(numFmt))
            {
                if (IsBulletLike(lvlText)) numFmt = "bullet";
            }
            startVal = lvl.StartNumberingValue?.Val?.Value;
        }

        private static bool IsBulletLike(string? lvlText)
        {
            if (string.IsNullOrWhiteSpace(lvlText)) return false;
            // Nếu mẫu có chứa % (placeholder số), coi như numbering theo số/chữ, không phải bullet thuần
            if (lvlText.Contains('%')) return false;
            var trimmed = lvlText.Trim();
            // Một số bullet phổ biến trong Word
            var bullets = new HashSet<string>(StringComparer.Ordinal)
            {
                "-",            // U+002D Hyphen-Minus
                "+",            // U+002B Plus
                "*",            // U+002A Asterisk
                "o",            // U+006F Latin Small Letter o
                "•",            // U+2022 Bullet
                "·",            // U+00B7 Middle Dot (Symbol code 183)
                "§",            // U+00A7 Section Sign (Wingdings code 167 mapping case)
                "–",            // U+2013 En Dash
                "—",            // U+2014 Em Dash
                "◦",            // U+25E6 White Bullet
                "▪",            // U+25AA Black Small Square
                "■",            // U+25A0 Black Square
                "●"             // U+25CF Black Circle
            };
            if (bullets.Contains(trimmed)) return true;
            // Nếu độ dài ngắn (<=3) và không có chữ số/chữ cái, tạm coi là bullet ký hiệu
            if (trimmed.Length <= 3 && !trimmed.Any(char.IsLetterOrDigit)) return true;
            return false;
        }

        private static string? ResolveParagraphJustificationFromStyle(MainDocumentPart mainPart, string styleId)
        {
            var styles = mainPart.StyleDefinitionsPart?.Styles;
            if (styles == null) return null;

            W.Style? current = styles.Elements<W.Style>()
                .FirstOrDefault(s => string.Equals(s.StyleId?.Value, styleId, StringComparison.OrdinalIgnoreCase));

            while (current != null)
            {
                var jc = GetJustificationString(current.StyleParagraphProperties?.Justification);
                if (!string.IsNullOrEmpty(jc)) return jc;

                var basedOnId = current.BasedOn?.Val?.Value;
                if (string.IsNullOrEmpty(basedOnId)) break;

                current = styles.Elements<W.Style>()
                    .FirstOrDefault(s => string.Equals(s.StyleId?.Value, basedOnId, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        private static string? ResolveDefaultParagraphJustification(MainDocumentPart mainPart)
        {
            var styles = mainPart.StyleDefinitionsPart?.Styles;
            if (styles == null) return null;

            var defaultParaStyle = styles.Elements<W.Style>()
                .FirstOrDefault(s => s.Default != null && s.Default.Value
                                     && s.Type?.Value == W.StyleValues.Paragraph);
            var jc = GetJustificationString(defaultParaStyle?.StyleParagraphProperties?.Justification);
            return string.IsNullOrEmpty(jc) ? null : jc;
        }

        private static string? ResolveDocDefaultsParagraphJustification(MainDocumentPart mainPart)
        {
            var styles = mainPart.StyleDefinitionsPart?.Styles;
            if (styles == null) return null;

            var pPrDefault = styles.DocDefaults?
                .ParagraphPropertiesDefault?
                .Elements<W.ParagraphProperties>()
                .FirstOrDefault();
            var jc = GetJustificationString(pPrDefault?.Justification);
            return string.IsNullOrEmpty(jc) ? null : jc;
        }

        private static string? GetJustificationString(W.Justification? jc)
        {
            if (jc == null) return null;
            var val = jc.Val;
            if (val != null && val.HasValue)
            {
                var v = val.Value;
                if (v == W.JustificationValues.Center) return "center";
                if (v == W.JustificationValues.Right) return "right";
                if (v == W.JustificationValues.Both) return "both";
                if (v == W.JustificationValues.Left) return "left";
                if (v == W.JustificationValues.Distribute) return "distribute";
                if (v == W.JustificationValues.Start) return "start";
                if (v == W.JustificationValues.End) return "end";
            }
            // Fallback: đọc trực tiếp thuộc tính XML nếu EnumValue không populate hoặc enum không thuộc nhóm hỗ trợ
            var raw = jc.GetAttribute("val", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");
            if (!string.IsNullOrWhiteSpace(raw.Value))
                return raw.Value.ToLowerInvariant();
            return null;
        }

        private static T? GetStyleProperty<T>(string? styleId, MainDocumentPart mainPart, Func<W.Style, T?> selector)
            where T : OpenXmlElement
        {
            if (string.IsNullOrEmpty(styleId) || mainPart.StyleDefinitionsPart?.Styles == null)
                return null;

            var styles = mainPart.StyleDefinitionsPart.Styles;
            var style = styles.Elements<W.Style>()
                .FirstOrDefault(s => string.Equals(s.StyleId?.Value, styleId, StringComparison.OrdinalIgnoreCase));
            if (style == null) return null;

            var prop = selector(style);
            if (prop != null) return prop;

            var basedOn = style.BasedOn?.Val?.Value;
            if (string.IsNullOrEmpty(basedOn)) return null;
            return GetStyleProperty(basedOn, mainPart, selector);
        }

        private void HandleRun(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart currentPart)
        {
            var r = (W.Run)el;
            var styleSb = new StringBuilder();

            var rPr = r.RunProperties;
            if (rPr != null)
            {
                // Bold/Italic (nếu có tag mà không có Val => coi như true)
                if (rPr.Bold != null && (rPr.Bold.Val == null || rPr.Bold.Val.Value)) styleSb.Append("font-weight:bold;");
                if (rPr.Italic != null && (rPr.Italic.Val == null || rPr.Italic.Val.Value)) styleSb.Append("font-style:italic;");

                // Underline
                var uVal = rPr.Underline?.Val?.Value;
                if (uVal != null && uVal.ToString() != "none")
                {
                    styleSb.Append("text-decoration:underline;");
                }

                // Strike
                if (rPr.Strike != null && (rPr.Strike.Val == null || rPr.Strike.Val.Value))
                    styleSb.Append("text-decoration:line-through;");

                // Color (hex without #)
                var color = rPr.Color?.Val?.Value;
                if (!string.IsNullOrEmpty(color))
                {
                    // Word có thể dùng "auto" – bỏ qua để dùng màu mặc định.
                    if (!string.Equals(color, "auto", StringComparison.OrdinalIgnoreCase))
                        styleSb.Append($"color:#{color};");
                }

                // Font size (half-point)
                var sz = rPr.FontSize?.Val?.Value;
                if (!string.IsNullOrEmpty(sz) && double.TryParse(sz, NumberStyles.Any, CultureInfo.InvariantCulture, out var halfPt))
                {
                    var pt = halfPt / 2.0;
                    styleSb.Append($"font-size:{pt.ToString(CultureInfo.InvariantCulture)}pt;");
                }
            }

            sb.Append("<span class=\"w-r\" data-oxml=\"w:r\"");
            if (styleSb.Length > 0)
                sb.Append($" style=\"{WebUtility.HtmlEncode(styleSb.ToString())}\"");
            sb.Append(">");
            ProcessChildren(r, sb, mainPart, currentPart);
            sb.Append("</span>");
        }

        private void HandleText(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart currentPart)
        {
            var t = (W.Text)el;
            var run = t.Ancestors<W.Run>().FirstOrDefault();
            var raw = t.Text ?? string.Empty;
            // Map một-sang-một các ký tự đặc biệt (không thay đổi số lượng ký tự)
            raw = MapSpecialSymbols(raw, run);
            // Giữ nguyên chuỗi gốc, chỉ bao bọc placeholder bằng span, không thêm/bớt ký tự
            sb.Append(HighlightPlaceholders(raw));
        }

        // Chuyển một-sang-một các ký tự đặc biệt để hiển thị đúng: Checkbox/Wingdings 2 code 42 -> □
        private static string MapSpecialSymbols(string text, W.Run? run)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Wingdings 2 (w:rFonts @ascii/@hAnsi/@cs)
            var font = run?.RunProperties?.RunFonts;
            var fontName = font?.Ascii?.Value ?? font?.HighAnsi?.Value ?? font?.ComplexScript?.Value;
            if (!string.IsNullOrEmpty(fontName) && fontName!.Equals("Wingdings 2", StringComparison.OrdinalIgnoreCase))
            {
                // Ký tự 42 ("*") trong Wingdings 2 thường là ô vuông rỗng
                // Để không thay đổi số lượng ký tự: thay thế 1-1: '*' -> '□' (U+25A1)
                var chars = text.ToCharArray();
                for (int i = 0; i < chars.Length; i++)
                {
                    if (chars[i] == '*') chars[i] = '\u25A1';
                }
                return new string(chars);
            }

            return text;
        }

        // Xây dựng <colgroup> dựa vào w:tblGrid -> w:gridCol@w (đơn vị dxa/twips)
        private static void BuildColGroup(W.Table tbl, StringBuilder sb)
        {
            var grid = tbl.Elements<W.TableGrid>().FirstOrDefault();
            if (grid == null) return;

            var cols = grid.Elements<W.GridColumn>().ToList();
            if (cols.Count == 0) return;

            // Thu thập width (twips). Nếu có cột 0 và có cột >0, gán 0 = trung bình các cột >0 để tránh bóp méo bố cục
            var widths = new List<int>();
            foreach (var c in cols)
            {
                if (int.TryParse(c.Width?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
                    widths.Add(parsed);
                else
                    widths.Add(0);
            }

            var nonZero = widths.Where(w => w > 0).ToList();
            if (nonZero.Count > 0 && widths.Any(w => w == 0))
            {
                var avg = (int)Math.Max(1, Math.Round(nonZero.Average()));
                for (int i = 0; i < widths.Count; i++) if (widths[i] == 0) widths[i] = avg;
            }

            long total = widths.Sum(w => (long)w);
            if (total <= 0)
            {
                // Không có thông tin grid hợp lệ -> đừng render colgroup để tránh layout hỏng
                return;
            }

            sb.Append("<colgroup>");
            foreach (var w in widths)
            {
                var pct = w * 100.0 / total;
                var pctStr = pct.ToString("0.####", CultureInfo.InvariantCulture);
                sb.Append($"<col style=\"width:{pctStr}%;\">");
            }
            sb.Append("</colgroup>");
        }

        // Bao bọc <<...>> bằng span.placeholder-span/placeholder-empty mà không thay đổi chuỗi gốc
        private static string HighlightPlaceholders(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var pattern = new Regex(@"<<(?<name>[^\r\n]{0,200}?)>>", RegexOptions.Compiled);
            var sb = new StringBuilder();
            int last = 0;
            foreach (Match m in pattern.Matches(text))
            {
                if (m.Index > last)
                {
                    var before = text.Substring(last, m.Index - last);
                    sb.Append(WebUtility.HtmlEncode(before));
                }

                var raw = m.Value; // ví dụ: <<FIELD>> hoặc <<>>
                var name = m.Groups["name"].Value;
                var isEmpty = string.IsNullOrWhiteSpace(name);
                var classes = isEmpty ? "placeholder-span placeholder-empty" : "placeholder-span";
                var encodedInner = WebUtility.HtmlEncode(raw);
                var dataName = WebUtility.HtmlEncode(name);
                sb.Append($"<span class=\"{classes}\" data-ph-name=\"{dataName}\">{encodedInner}</span>");

                last = m.Index + m.Length;
            }
            if (last < text.Length)
            {
                var tail = text.Substring(last);
                sb.Append(WebUtility.HtmlEncode(tail));
            }
            return sb.ToString();
        }

        private void HandleTable(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart currentPart)
        {
            var tbl = (W.Table)el;
            // Xác định mức lồng nhau của bảng để áp dụng class phù hợp
            var nestingLevel = el.Ancestors<W.Table>().Count(); // không tính chính nó
            var tblClass = nestingLevel == 0 ? "parent-table" : "nested-table";

            // Đọc w:tblPr/w:tblW để áp width
            string? widthStyle = null;
            var tblPr = tbl.GetFirstChild<W.TableProperties>();
            var tblW = tblPr?.TableWidth;
            if (tblW != null)
            {
                var type = tblW.Type != null ? tblW.Type.Value.ToString().ToLowerInvariant() : null;
                var wValStr = tblW.Width?.Value;
                if (!string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(wValStr))
                {
                    if (type == "pct")
                    {
                        if (int.TryParse(wValStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var fiftieths))
                        {
                            // pct trong Word = phần trăm * 50 (5000 = 100%)
                            var pct = Math.Max(0, fiftieths) / 50.0;
                            widthStyle = $"width:{pct.ToString("0.##", CultureInfo.InvariantCulture)}%;";
                        }
                    }
                    else if (type == "dxa")
                    {
                        if (int.TryParse(wValStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var twips) && twips > 0)
                        {
                            // 1 twip = 1/20 pt; px = pt * 96/72 => px = twips * 96 / 1440 = twips / 15
                            var px = twips / 15.0;
                            widthStyle = $"width:{px.ToString("0.##", CultureInfo.InvariantCulture)}px;";
                        }
                    }
                    else if (type == "auto")
                    {
                        widthStyle = null; // để auto theo CSS
                    }
                }
            }

            sb.Append($"<table class=\"w-tbl {tblClass}\" data-nested-depth=\"{nestingLevel}\"");
            if (!string.IsNullOrEmpty(widthStyle)) sb.Append($" style=\"{WebUtility.HtmlEncode(widthStyle)}\"");
            sb.Append(">");

            // Dựng colgroup nếu có w:tblGrid
            BuildColGroup(tbl, sb);

            ProcessChildren(el, sb, mainPart, currentPart);
            sb.Append("</table>");
        }

        private void HandleTableRow(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart currentPart)
        {
            sb.Append("<tr class=\"w-tr\">");
            ProcessChildren(el, sb, mainPart, currentPart);
            sb.Append("</tr>");
        }

        private void HandleTableCell(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart currentPart)
        {
            var tc = (W.TableCell)el;
            var tcPr = tc.TableCellProperties;
            int colspan = 1;
            var gridSpanVal = tcPr?.GridSpan?.Val?.Value;
            if (gridSpanVal.HasValue && gridSpanVal.Value > 1)
                colspan = gridSpanVal.Value;

            string? style = null;
            var shd = tcPr?.Shading; // w:tcPr/w:shd
            if (shd != null)
            {
                var fill = shd.Fill?.Value;
                if (!string.IsNullOrWhiteSpace(fill) && !fill!.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    style = $"background-color:#{fill};";
                }
            }

            sb.Append("<td class=\"w-td\"");
            if (colspan > 1) sb.Append($" colspan=\"{colspan}\"");
            if (!string.IsNullOrEmpty(style)) sb.Append($" style=\"{WebUtility.HtmlEncode(style)}\"");
            sb.Append(">");
            ProcessChildren(tc, sb, mainPart, currentPart);
            sb.Append("</td>");
        }

        private void HandleBreak(OpenXmlElement el, StringBuilder sb, MainDocumentPart _, OpenXmlPart __)
        {
            var br = (W.Break)el;
            // Value của Enum (BreakValues) là value-type, không thể dùng toán tử ?. trực tiếp trên nó
            // Kiểm tra null ở cấp EnumValue trước, sau đó gọi ToString() bình thường
            string type = br.Type != null ? br.Type.Value.ToString() : "textWrapping";
            sb.Append($"<br class=\"w-br\" data-oxml=\"w:br\" data-br-type=\"{WebUtility.HtmlEncode(type)}\"/>");
        }

        private void HandleTab(OpenXmlElement el, StringBuilder sb, MainDocumentPart _, OpenXmlPart __)
        {
            sb.Append("<span class=\"w-tab\" data-oxml=\"w:tab\">&#9;</span>");
        }

        private void HandleHyperlink(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart currentPart)
        {
            var link = (W.Hyperlink)el;
            string? href = null;
            string? anchor = link.Anchor?.Value;
            var rid = link.Id?.Value;

            if (!string.IsNullOrEmpty(rid))
            {
                var rel = currentPart?.HyperlinkRelationships?.FirstOrDefault(r => r.Id == rid)
                        ?? mainPart.HyperlinkRelationships.FirstOrDefault(r => r.Id == rid);
                href = rel?.Uri?.ToString();
            }
            if (!string.IsNullOrEmpty(anchor))
            {
                href = "#" + anchor;
            }

            sb.Append("<a class=\"w-hyperlink\" data-oxml=\"w:hyperlink\"");
            if (!string.IsNullOrEmpty(href))
                sb.Append($" href=\"{WebUtility.HtmlEncode(href)}\"");
            if (!string.IsNullOrEmpty(anchor))
                sb.Append($" data-anchor=\"{WebUtility.HtmlEncode(anchor)}\"");
            if (!string.IsNullOrEmpty(rid))
                sb.Append($" data-rid=\"{WebUtility.HtmlEncode(rid)}\"");
            sb.Append(">");

            ProcessChildren(link, sb, mainPart, currentPart ?? mainPart);
            sb.Append("</a>");
        }

        private void HandleBookmarkStart(OpenXmlElement el, StringBuilder sb, MainDocumentPart _, OpenXmlPart __)
        {
            var b = (W.BookmarkStart)el;
            var idStr = b.Id?.Value?.ToString() ?? "";
            var nameStr = b.Name?.Value ?? "";
            var safeId = WebUtility.HtmlEncode(idStr);
            var safeName = WebUtility.HtmlEncode(nameStr);
            sb.Append($"<span class=\"w-bookmark-start\" data-oxml=\"w:bookmarkStart\" data-bmk-id=\"{safeId}\" data-bmk-name=\"{safeName}\"></span>");

            // Heuristic: Một số tài liệu không có w:instrText FORMCHECKBOX, nhưng có bookmark tên 'Check*'
            // Nếu đang trong ngữ cảnh field và tên bookmark gợi ý checkbox, gắn loại là FormCheckbox
            if (_fieldStack.Count > 0 && !string.IsNullOrWhiteSpace(nameStr))
            {
                var ctx = _fieldStack.Peek();
                if (ctx.Kind == FieldContextKind.Unknown)
                {
                    var nameLc = nameStr.Trim().ToLowerInvariant();
                    if (nameLc.StartsWith("check") || nameLc.Contains("checkbox") || nameLc.StartsWith("cb"))
                    {
                        ctx.Kind = FieldContextKind.FormCheckbox;
                    }
                }
            }
        }

        private void HandleBookmarkEnd(OpenXmlElement el, StringBuilder sb, MainDocumentPart _, OpenXmlPart __)
        {
            var b = (W.BookmarkEnd)el;
            sb.Append($"<span class=\"w-bookmark-end\" data-oxml=\"w:bookmarkEnd\" data-bmk-id=\"{b.Id?.Value}\"></span>");
        }

        private void HandleFieldSimple(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart currentPart)
        {
            var f = (W.SimpleField)el;
            string instr = f.Instruction?.Value ?? "";
            sb.Append($"<span class=\"w-field\" data-oxml=\"w:fldSimple\" data-instr=\"{WebUtility.HtmlEncode(instr)}\">");
            ProcessChildren(f, sb, mainPart, currentPart);
            sb.Append("</span>");
        }

        

        private void HandleFieldChar(OpenXmlElement el, StringBuilder sb, MainDocumentPart _, OpenXmlPart __)
        {
            var fc = (W.FieldChar)el;
            var type = fc.FieldCharType?.Value;
            if (type == null)
            {
                sb.Append("<span class=\"w-fldChar\" data-oxml=\"w:fldChar\" data-type=\"unknown\"></span>");
                return;
            }

            var v = type.Value;
            if (v == W.FieldCharValues.Begin)
            {
                _fieldStack.Push(new FieldContext());
                // Render dấu mốc ẩn (không có nội dung hiển thị)
                sb.Append("<span class=\"w-fldChar\" data-oxml=\"w:fldChar\" data-type=\"begin\"></span>");
            }
            else if (v == W.FieldCharValues.Separate)
            {
                // Thời điểm bắt đầu vùng hiển thị kết quả field
                if (_fieldStack.Count > 0)
                {
                    var ctx = _fieldStack.Peek();
                    if (ctx.Kind == FieldContextKind.FormCheckbox && !ctx.Emitted)
                    {
                        // Chèn đúng 1 ký tự Unicode để giữ nguyên độ dài/offset văn bản
                        sb.Append('\u25A1'); // □
                        ctx.Emitted = true;
                    }
                }
                sb.Append("<span class=\"w-fldChar\" data-oxml=\"w:fldChar\" data-type=\"separate\"></span>");
            }
            else if (v == W.FieldCharValues.End)
            {
                if (_fieldStack.Count > 0)
                {
                    var ctx = _fieldStack.Peek();
                    if (ctx.Kind == FieldContextKind.FormCheckbox && !ctx.Emitted)
                    {
                        sb.Append('\u25A1'); // □
                        ctx.Emitted = true;
                    }
                    _fieldStack.Pop();
                }
                sb.Append("<span class=\"w-fldChar\" data-oxml=\"w:fldChar\" data-type=\"end\"></span>");
            }
            else
            {
                sb.Append($"<span class=\"w-fldChar\" data-oxml=\"w:fldChar\" data-type=\"{WebUtility.HtmlEncode(type.Value.ToString())}\"></span>");
            }
        }

        private void HandleInstrText(OpenXmlElement el, StringBuilder sb, MainDocumentPart _, OpenXmlPart __)
        {
            var text = el.InnerText ?? string.Empty;
            // Nếu đang ở trong một field, gom instruction và không render ra để tránh lộ code
            if (_fieldStack.Count > 0)
            {
                var ctx = _fieldStack.Peek();
                ctx.Instr.Append(text);
                var instrUpper = ctx.Instr.ToString().ToUpperInvariant();
                if (ctx.Kind == FieldContextKind.Unknown)
                {
                    if (instrUpper.Contains("FORMCHECKBOX")) ctx.Kind = FieldContextKind.FormCheckbox;
                    else ctx.Kind = FieldContextKind.Other;
                }
                return; // không render instrText ra ngoài
            }
            sb.Append($"<span class=\"w-instrText\" data-oxml=\"w:instrText\">{WebUtility.HtmlEncode(text)}</span>");
        }

        private void HandleFootnoteRef(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart __)
        {
            var f = (W.FootnoteReference)el;
            int id = Convert.ToInt32(f.Id?.Value ?? -1);
            if (id >= 0) _footnoteQueue.Add(id);

            sb.Append($"<sup class=\"w-footnote-ref\" data-oxml=\"w:footnoteReference\" data-id=\"{id}\"><a href=\"#footnote-{id}\">{id}</a></sup>");
        }

        private void HandleEndnoteRef(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart __)
        {
            var f = (W.EndnoteReference)el;
            int id = Convert.ToInt32(f.Id?.Value ?? -1);
            if (id >= 0) _endnoteQueue.Add(id);

            sb.Append($"<sup class=\"w-endnote-ref\" data-oxml=\"w:endnoteReference\" data-id=\"{id}\"><a href=\"#endnote-{id}\">{id}</a></sup>");
        }

        private void HandleCommentRef(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart __)
        {
            var cr = (W.CommentReference)el;
            var id = cr.Id?.Value ?? "";
            sb.Append($"<sup class=\"w-comment-ref\" data-oxml=\"w:commentReference\" data-id=\"{WebUtility.HtmlEncode(id)}\">[c{id}]</sup>");
        }

        private void HandleCommentRangeStart(OpenXmlElement el, StringBuilder sb, MainDocumentPart _, OpenXmlPart __)
        {
            var cs = (W.CommentRangeStart)el;
            sb.Append($"<span class=\"w-comment-range-start\" data-oxml=\"w:commentRangeStart\" data-id=\"{WebUtility.HtmlEncode(cs.Id?.Value ?? "")}\"></span>");
        }

        private void HandleCommentRangeEnd(OpenXmlElement el, StringBuilder sb, MainDocumentPart _, OpenXmlPart __)
        {
            var ce = (W.CommentRangeEnd)el;
            sb.Append($"<span class=\"w-comment-range-end\" data-oxml=\"w:commentRangeEnd\" data-id=\"{WebUtility.HtmlEncode(ce.Id?.Value ?? "")}\"></span>");
        }

        private void HandleSdtRun(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart currentPart)
        {
            var sdt = (W.SdtRun)el;
            sb.Append("<span class=\"w-sdt-run\" data-oxml=\"w:sdtRun\">");
            ProcessChildren(sdt, sb, mainPart, currentPart);
            sb.Append("</span>");
        }

        private void HandleSdtBlock(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart currentPart)
        {
            var sdt = (W.SdtBlock)el;
            sb.Append("<div class=\"w-sdt-block\" data-oxml=\"w:sdt\">");
            ProcessChildren(sdt, sb, mainPart, currentPart);
            sb.Append("</div>");
        }

        private void HandleSdtCell(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart currentPart)
        {
            var sdt = (W.SdtCell)el;
            sb.Append("<td class=\"w-sdt-cell\" data-oxml=\"w:sdt\">");
            ProcessChildren(sdt, sb, mainPart, currentPart);
            sb.Append("</td>");
        }

        private void HandleDrawing(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart currentPart)
        {
            // Tìm a:blip để lấy r:embed (rId đến ImagePart)
            var blip = el.Descendants<Blip>().FirstOrDefault();
            var rId = blip?.Embed?.Value;

            // Lấy kích thước từ wp:inline/wp:extent nếu có (EMU -> px)
            int? widthPx = null, heightPx = null;
            var extent = el.Descendants<WpDrawing.Extent>().FirstOrDefault();
            if (extent?.Cx != null && extent?.Cy != null)
            {
                const double emuPerPx = 9525.0; // 914400 EMU/inch / 96 px/inch
                try
                {
                    widthPx = (int)Math.Round(extent.Cx.Value / emuPerPx);
                    heightPx = (int)Math.Round(extent.Cy.Value / emuPerPx);
                }
                catch { /* bỏ qua nếu parse lỗi */ }
            }

            if (!string.IsNullOrEmpty(rId))
            {
                try
                {
                    // Ưu tiên lấy từ currentPart, fallback mainPart
                    OpenXmlPart? imgPartContainer = currentPart ?? mainPart;
                    var part = imgPartContainer?.GetPartById(rId!);
                    if (part is ImagePart imagePart)
                    {
                        using var s = imagePart.GetStream(FileMode.Open, FileAccess.Read);
                        using var ms = new MemoryStream();
                        s.CopyTo(ms);
                        var base64 = Convert.ToBase64String(ms.ToArray());
                        var mime = imagePart.ContentType; // ví dụ: image/png, image/jpeg

                        // Render ảnh với style co giãn theo container, không vượt quá kích thước phần tử chứa.
                        // Ghi kích thước gốc dưới dạng data-* để client có thể dùng khi cần.
                        var style = "max-width:100%; max-height:100%; height:auto; width:auto; object-fit:contain;";
                        sb.Append("<img class=\"w-drawing\" data-oxml=\"w:drawing\"");
                        if (widthPx.HasValue) sb.Append($" data-natural-width=\"{widthPx.Value}\"");
                        if (heightPx.HasValue) sb.Append($" data-natural-height=\"{heightPx.Value}\"");
                        sb.Append($" style=\"{WebUtility.HtmlEncode(style)}\"");
                        sb.Append(" src=\"");
                        sb.Append($"data:{WebUtility.HtmlEncode(mime)};base64,{base64}\"");
                        sb.Append(" alt=\"embedded-image\" />");
                        return;
                    }
                }
                catch { /* bỏ qua, dùng fallback */ }
            }

            // Fallback nếu không tìm thấy ảnh: ẩn phần tử (không render XML thô)
            // Có thể thay bằng placeholder nếu muốn: sb.Append("<span class=\"w-drawing-missing\"></span>");
        }

        // Handler cho w:sym: cố gắng giải mã ký tự từ mã hex; riêng Wingdings 2 map về ô vuông rỗng
        private void HandleSymbol(OpenXmlElement el, StringBuilder sb, MainDocumentPart _, OpenXmlPart __)
        {
            var sym = (W.SymbolChar)el;
            var font = sym.Font?.Value;
            var hex = sym.Char?.Value; // mã hex, thường là Unicode code point

            // Mặc định: một ký tự ô vuông rỗng
            int codePoint = 0x25A1; // □

            // Nếu không phải Wingdings 2, thử giải mã mã hex để giữ đúng ký tự gốc
            if (!string.IsNullOrEmpty(hex)
                && int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                codePoint = parsed;
            }

            // Nếu là Wingdings 2 (thường dùng làm checkbox), ép về ô vuông rỗng để hiển thị consistent
            if (!string.IsNullOrEmpty(font) && font.Equals("Wingdings 2", StringComparison.OrdinalIgnoreCase))
            {
                codePoint = 0x25A1; // □
            }

            // Append đúng 1 ký tự
            sb.Append(char.ConvertFromUtf32(codePoint));
        }

        private void IgnoreElement(OpenXmlElement el, StringBuilder sb, MainDocumentPart mainPart, OpenXmlPart currentPart)
        {
            // Không render gì cho các phần tử properties/trang trí
        }

        private void RenderNotesSection(StringBuilder sb, MainDocumentPart mainPart)
        {
            if (_footnoteQueue.Count > 0 && mainPart.FootnotesPart?.Footnotes != null)
            {
                sb.Append("<aside class=\"w-footnotes\"><ol>");
                foreach (var id in _footnoteQueue.Distinct())
                {
                    var item = mainPart.FootnotesPart.Footnotes.Elements<W.Footnote>().FirstOrDefault(x => x.Id?.Value == id);
                    sb.Append($"<li id=\"footnote-{id}\">");
                    if (item != null) foreach (var c in item.ChildElements) ProcessElement(c, sb, mainPart, mainPart.FootnotesPart);
                    sb.Append("</li>");
                }
                sb.Append("</ol></aside>");
            }

            if (_endnoteQueue.Count > 0 && mainPart.EndnotesPart?.Endnotes != null)
            {
                sb.Append("<aside class=\"w-endnotes\"><ol>");
                foreach (var id in _endnoteQueue.Distinct())
                {
                    var item = mainPart.EndnotesPart.Endnotes.Elements<W.Endnote>().FirstOrDefault(x => x.Id?.Value == id);
                    sb.Append($"<li id=\"endnote-{id}\">");
                    if (item != null) foreach (var c in item.ChildElements) ProcessElement(c, sb, mainPart, mainPart.EndnotesPart);
                    sb.Append("</li>");
                }
                sb.Append("</ol></aside>");
            }
        }

        public string ConvertToHtml(byte[] docxBytes, bool isViewMode = false)
        {
            if (docxBytes == null || docxBytes.Length == 0)
                throw new ArgumentException("docxBytes không hợp lệ (null hoặc rỗng)", nameof(docxBytes));

            using var ms = new MemoryStream(docxBytes, writable: false);
            using var doc = WordprocessingDocument.Open(ms, false);
            var mainPart = doc.MainDocumentPart ?? throw new InvalidOperationException("DOCX không có MainDocumentPart");
            var sb = new StringBuilder();

            // TODO: Sử dụng isViewMode để điều chỉnh render khi cần
            sb.Append("<div class=\"docx-html\">");
            foreach (var child in mainPart.Document.Body.ChildElements)
            {
                ProcessElement(child, sb, mainPart, mainPart);
            }
            RenderNotesSection(sb, mainPart);
            sb.Append("</div>");

            return sb.ToString();
        }
    }
}