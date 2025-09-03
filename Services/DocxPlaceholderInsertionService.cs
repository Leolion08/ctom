using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using CTOM.ViewModels.Template;

namespace CTOM.Services
{
    /// <summary>
    /// [SERVICE MỚI] Service chuyên dụng để chèn các placeholder (ví dụ: "<<FieldName>>")
    /// vào tài liệu DOCX dựa trên một danh sách các "Dấu vân tay vị trí" (FieldPositionFingerprint).
    /// Service này hoạt động trực tiếp trên cấu trúc OpenXML, đảm bảo độ chính xác cao.
    /// </summary>
    public class DocxPlaceholderInsertionService
    {
        /// <summary>
        /// Phương thức chính để thực hiện việc chèn.
        /// </summary>
        /// <param name="originalDocxBytes">Nội dung file DOCX gốc dưới dạng byte array.</param>
        /// <param name="fingerprints">Danh sách các "dấu vân tay" chỉ định vị trí và tên trường cần chèn.</param>
        /// <returns>Nội dung file DOCX mới (đã chèn placeholder) dưới dạng byte array.</returns>
        public byte[] InsertPlaceholders(byte[] originalDocxBytes, List<FieldPositionFingerprint> fingerprints)
        {
            using var stream = new MemoryStream();
            stream.Write(originalDocxBytes, 0, originalDocxBytes.Length);

            using (var wordDoc = WordprocessingDocument.Open(stream, true))
            {
                var groupedByPart = fingerprints.GroupBy(f => f.PartUri);

                foreach (var partGroup in groupedByPart)
                {
                    var partUri = new Uri(partGroup.Key, UriKind.Relative);
                    var part = GetPartByUri(wordDoc, partUri);

                    if (part?.RootElement == null) continue;

                    var groupedByParagraph = partGroup.GroupBy(f => f.ParagraphId);

                    foreach (var paragraphGroup in groupedByParagraph)
                    {
                        var paragraphId = paragraphGroup.Key;
                        var paragraph = part.RootElement.Descendants<Paragraph>()
                                            .FirstOrDefault(p => p.ParagraphId?.Value == paragraphId);

                        if (paragraph == null) continue;

                        // [SỬA LỖI] Sử dụng logic tái tạo paragraph hoàn toàn mới và đáng tin cậy.
                        RebuildParagraphWithPlaceholders(paragraph, paragraphGroup.ToList());
                    }
                }
                wordDoc.Save();
            }
            return stream.ToArray();
        }

        /// <summary>
        /// [HÀM MỚI - CỐT LÕI] Tái tạo lại hoàn toàn một Paragraph với các placeholder được chèn vào.
        /// Cách tiếp cận này đảm bảo vị trí chính xác và bảo toàn định dạng.
        /// </summary>
        private void RebuildParagraphWithPlaceholders(Paragraph paragraph, List<FieldPositionFingerprint> fingerprints)
        {
            // 1. Phân tích Paragraph gốc thành một danh sách các "Token" (TextToken hoặc TabToken).
            // Mỗi token chứa nội dung và định dạng (RunProperties) của nó.
            var originalTokens = new List<BaseToken>();
            foreach (var run in paragraph.Elements<Run>())
            {
                var runProps = run.RunProperties?.CloneNode(true) as RunProperties;
                foreach (var element in run.ChildElements)
                {
                    if (element is Text text && !string.IsNullOrEmpty(text.Text))
                    {
                        originalTokens.Add(new TextToken { Content = text.Text, Properties = runProps });
                    }
                    else if (element is TabChar)
                    {
                        originalTokens.Add(new TabToken { Properties = runProps });
                    }
                    // Các element khác như Break, Drawing... có thể được xử lý ở đây nếu cần.
                }
            }

            // 2. Tạo một danh sách các "Insertion" (điểm chèn placeholder) từ fingerprints.
            // Sắp xếp chúng theo offset, sau đó theo tie-breaker để giữ đúng thứ tự người dùng đã chèn.
            var insertions = fingerprints
                .Select(fp => new Insertion
                {
                    Offset = fp.OffsetInParagraph,
                    Placeholder = $"<<{fp.FieldName}>>",
                    TieBreaker = fp.OffsetTieBreaker ?? 0
                })
                .OrderBy(i => i.Offset)
                .ThenBy(i => i.TieBreaker)
                .ToList();

            // 3. Xây dựng lại danh sách token mới bằng cách chèn các placeholder vào.
            var newTokens = new List<BaseToken>();
            int currentOffset = 0;
            int tokenIndex = 0;

            // Chèn các placeholder có offset 0 trước tiên
            while (insertions.Any() && insertions.First().Offset == 0)
            {
                // Lấy định dạng từ token đầu tiên (nếu có) hoặc tạo định dạng mặc định.
                var props = originalTokens.FirstOrDefault()?.Properties?.CloneNode(true) as RunProperties;
                newTokens.Add(new TextToken { Content = insertions.First().Placeholder, Properties = props });
                insertions.RemoveAt(0);
            }

            // Duyệt qua các token gốc và chèn placeholder vào giữa
            foreach (var token in originalTokens)
            {
                if (token is TextToken textToken)
                {
                    string text = textToken.Content;
                    int processedLength = 0;

                    while (processedLength < text.Length)
                    {
                        // Tìm điểm chèn tiếp theo trong đoạn text này
                        var nextInsertion = insertions.FirstOrDefault(i => i.Offset > currentOffset && i.Offset <= currentOffset + (text.Length - processedLength));

                        if (nextInsertion == null)
                        {
                            // Không có điểm chèn nào nữa, thêm phần text còn lại
                            var remainingText = text.Substring(processedLength);
                            if (!string.IsNullOrEmpty(remainingText))
                            {
                                newTokens.Add(new TextToken { Content = remainingText, Properties = textToken.Properties?.CloneNode(true) as RunProperties });
                            }
                            currentOffset += remainingText.Length;
                            break; // Kết thúc xử lý token này
                        }
                        else
                        {
                            // Chèn phần text trước điểm chèn
                            int insertPosInText = nextInsertion.Offset - currentOffset - processedLength;
                            var beforeText = text.Substring(processedLength, insertPosInText);
                            if (!string.IsNullOrEmpty(beforeText))
                            {
                                newTokens.Add(new TextToken { Content = beforeText, Properties = textToken.Properties?.CloneNode(true) as RunProperties });
                            }

                            // Chèn tất cả các placeholder tại vị trí này
                            var placeholdersAtThisOffset = insertions.Where(i => i.Offset == nextInsertion.Offset).OrderBy(i => i.TieBreaker).ToList();
                            foreach (var insertion in placeholdersAtThisOffset)
                            {
                                newTokens.Add(new TextToken { Content = insertion.Placeholder, Properties = textToken.Properties?.CloneNode(true) as RunProperties });
                            }
                            
                            // Cập nhật vị trí và xóa các placeholder đã chèn
                            currentOffset += beforeText.Length;
                            processedLength += beforeText.Length;
                            insertions.RemoveAll(i => i.Offset == nextInsertion.Offset);
                        }
                    }
                }
                else if (token is TabToken tabToken)
                {
                    newTokens.Add(tabToken);
                    currentOffset++; // Tab được tính là 1 ký tự
                }

                // Chèn các placeholder ngay sau token vừa xử lý
                 while (insertions.Any() && insertions.First().Offset == currentOffset)
                {
                    newTokens.Add(new TextToken { Content = insertions.First().Placeholder, Properties = token.Properties?.CloneNode(true) as RunProperties });
                    insertions.RemoveAt(0);
                }
            }
            
            // Chèn các placeholder còn lại ở cuối (nếu có)
            foreach(var insertion in insertions)
            {
                 var props = originalTokens.LastOrDefault()?.Properties?.CloneNode(true) as RunProperties;
                 newTokens.Add(new TextToken { Content = insertion.Placeholder, Properties = props });
            }

            // 4. Tạo các Run mới từ danh sách token đã hoàn chỉnh.
            var newRuns = new List<Run>();
            foreach (var token in newTokens)
            {
                var newRun = new Run();
                if (token.Properties != null)
                {
                    newRun.RunProperties = token.Properties;
                }
                if (token is TextToken txt)
                {
                    newRun.Append(new Text(txt.Content) { Space = SpaceProcessingModeValues.Preserve });
                }
                else if (token is TabToken)
                {
                    newRun.Append(new TabChar());
                }
                newRuns.Add(newRun);
            }

            // 5. Xóa nội dung cũ của paragraph và thay thế bằng các Run mới.
            paragraph.RemoveAllChildren<Run>();
            paragraph.Append(newRuns);
        }

        // --- Helper Classes for Rebuilding Logic ---
        private abstract class BaseToken
        {
            public RunProperties? Properties { get; set; }
        }

        private class TextToken : BaseToken
        {
            public string Content { get; set; } = string.Empty;
        }

        private class TabToken : BaseToken { }

        private class Insertion
        {
            public int Offset { get; set; }
            public string Placeholder { get; set; } = string.Empty;
            public double TieBreaker { get; set; }
        }


        //HELPER: Lấy Part từ URI
        private OpenXmlPart? GetPartByUri(WordprocessingDocument doc, Uri partUri)
        {
            return doc.Parts
                    .Select(p => p.OpenXmlPart)
                    .FirstOrDefault(p => p.Uri == partUri);
        }
        //======================= OLD
    //     /// <summary>
    //     /// Phương thức chính để thực hiện việc chèn.
    //     /// </summary>
    //     /// <param name="originalDocxBytes">Nội dung file DOCX gốc dưới dạng byte array.</param>
    //     /// <param name="fingerprints">Danh sách các "dấu vân tay" chỉ định vị trí và tên trường cần chèn.</param>
    //     /// <returns>Nội dung file DOCX mới (đã chèn placeholder) dưới dạng byte array.</returns>
    //     public byte[] InsertPlaceholders(byte[] originalDocxBytes, List<FieldPositionFingerprint> fingerprints)
    //     {
    //         using var stream = new MemoryStream();
    //         stream.Write(originalDocxBytes, 0, originalDocxBytes.Length);

    //         using (var wordDoc = WordprocessingDocument.Open(stream, true))
    //         {
    //             var groupedByPart = fingerprints.GroupBy(f => f.PartUri);

    //             foreach (var partGroup in groupedByPart)
    //             {
    //                 var partUri = new Uri(partGroup.Key, UriKind.Relative);
    //                 var part = GetPartByUri(wordDoc, partUri);

    //                 if (part?.RootElement == null) continue;

    //                 var groupedByParagraph = partGroup.GroupBy(f => f.ParagraphId);

    //                 foreach (var paragraphGroup in groupedByParagraph)
    //                 {
    //                     var paragraphId = paragraphGroup.Key;
    //                     var paragraph = part.RootElement.Descendants<Paragraph>()
    //                                         .FirstOrDefault(p => p.ParagraphId?.Value == paragraphId);

    //                     if (paragraph == null) continue;

    //                     // [NÂNG CẤP] Chuẩn hóa offset dựa trên văn bản gốc của paragraph và context
    //                     var processedFingerprints = NormalizeOffsets(paragraph, paragraphGroup.ToList());

    //                     // [KIẾN TRÚC LẠI] Gọi hàm tái tạo Paragraph đã được nâng cấp
    //                     RebuildParagraphWithPlaceholders(paragraph, processedFingerprints);
    //                 }
    //             }
    //             wordDoc.Save();
    //         }
    //         return stream.ToArray();
    //     }

    //     /// <summary>
    //     /// [NÂNG CẤP] Thuật toán phỏng đoán để xử lý các trường hợp client gửi về các offset liền kề nhau.
    //     /// </summary>
    //     private List<FieldPositionFingerprint> PreProcessAdjacentFingerprints(List<FieldPositionFingerprint> fingerprints)
    //     {
    //         if (fingerprints.Count <= 1) return fingerprints;

    //         var sorted = fingerprints.OrderBy(f => f.OffsetInParagraph).ToList();
    //         int clusterBaseOffset = sorted[0].OffsetInParagraph;

    //         for (int i = 1; i < sorted.Count; i++)
    //         {
    //             if (sorted[i].OffsetInParagraph == sorted[i-1].OffsetInParagraph + 1)
    //             {
    //                 sorted[i].OffsetInParagraph = clusterBaseOffset;
    //             }
    //             else
    //             {
    //                 clusterBaseOffset = sorted[i].OffsetInParagraph;
    //             }
    //         }
    //         return sorted;
    //     }

    //     /// <summary>
    //     /// Chuẩn hóa OffsetInParagraph của các fingerprint để khớp với văn bản gốc (không có placeholder),
    //     /// sử dụng contextBefore/contextAfter sau khi loại bỏ các chuỗi placeholder kiểu <<...>> trong context.
    //     /// Giữ nguyên thứ tự người dùng đã chèn.
    //     /// </summary>
    //     private List<FieldPositionFingerprint> NormalizeOffsets(Paragraph paragraph, List<FieldPositionFingerprint> fingerprints)
    //     {
    //         // 1) Xây dựng baseText của paragraph: chỉ tính Text và TabChar (tab = 1 ký tự)
    //         var baseTextBuilder = new System.Text.StringBuilder();
    //         foreach (var run in paragraph.Elements<Run>())
    //         {
    //             foreach (var el in run.ChildElements)
    //             {
    //                 if (el is Text t)
    //                 {
    //                     baseTextBuilder.Append(t.Text ?? string.Empty);
    //                 }
    //                 else if (el is TabChar)
    //                 {
    //                     baseTextBuilder.Append('\t');
    //                 }
    //             }
    //         }
    //         var baseText = baseTextBuilder.ToString();

    //         // 2) Helper loại bỏ placeholder "<<...>>" trong context
    //         static string StripPlaceholders(string? s)
    //         {
    //             if (string.IsNullOrEmpty(s)) return string.Empty;
    //             return Regex.Replace(s, @"<<[^>]+>>", string.Empty);
    //         }

    //         // 3) Tính lại offset từ context trước/sau đã làm sạch
    //         foreach (var fp in fingerprints)
    //         {
    //             var ctxBefore = StripPlaceholders(fp.ContextBeforeText);
    //             var ctxAfter = StripPlaceholders(fp.ContextAfterText);

    //             int? computed = null;
    //             if (!string.IsNullOrEmpty(ctxBefore))
    //             {
    //                 // Ưu tiên lần xuất hiện cuối để gần vị trí con trỏ hơn
    //                 var idx = baseText.LastIndexOf(ctxBefore, StringComparison.Ordinal);
    //                 if (idx >= 0)
    //                 {
    //                     computed = idx + ctxBefore.Length;
    //                 }
    //             }
    //             if (computed == null && !string.IsNullOrEmpty(ctxAfter))
    //             {
    //                 var idx = baseText.IndexOf(ctxAfter, StringComparison.Ordinal);
    //                 if (idx >= 0)
    //                 {
    //                     computed = idx; // chèn trước ctxAfter
    //                 }
    //             }

    //             // Fallback: kẹp trong [0, baseText.Length], ưu tiên offset client gửi nếu hợp lệ
    //             if (computed == null)
    //             {
    //                 computed = Math.Max(0, Math.Min(fp.OffsetInParagraph, baseText.Length));
    //             }

    //             var finalOffset = computed.Value;

    //             // Heuristic: nếu rơi giữa 2 ký tự chữ (ví dụ giữa 'C' và 'h' trong "Chức"),
    //             // thì lùi về ranh giới từ trước đó để không chèn giữa từ.
    //             if (finalOffset > 0 && finalOffset < baseText.Length
    //                 && char.IsLetter(baseText[finalOffset - 1])
    //                 && char.IsLetter(baseText[finalOffset]))
    //             {
    //                 int i = finalOffset - 1;
    //                 // lùi đến khi gặp whitespace hoặc đầu chuỗi
    //                 while (i > 0 && !char.IsWhiteSpace(baseText[i])) i--;
    //                 // i hiện ở 0 hoặc tại whitespace
    //                 finalOffset = i;
    //             }

    //             // Kẹp lại biên
    //             finalOffset = Math.Max(0, Math.Min(finalOffset, baseText.Length));
    //             fp.OffsetInParagraph = finalOffset;
    //         }

    //         // 4) ỔN ĐỊNH THỨ TỰ khi nhiều placeholder rơi vào cùng một offset
    //         //    Dựa vào quan hệ trong context: nếu ContextAfterText của A chứa <<B>> thì A phải đứng TRƯỚC B
    //         var ordered = new List<FieldPositionFingerprint>();
    //         var groupsByOffset = fingerprints
    //             .GroupBy(f => f.OffsetInParagraph)
    //             .OrderBy(g => g.Key);

    //         foreach (var g in groupsByOffset)
    //         {
    //             var groupList = g.ToList();
    //             if (groupList.Count <= 1)
    //             {
    //                 ordered.AddRange(groupList);
    //                 continue;
    //             }

    //             var groupOrdered = OrderByContextDependencies(groupList);
    //             ordered.AddRange(groupOrdered);
    //         }

    //         return ordered;
    //     }

    //     private static List<FieldPositionFingerprint> OrderByContextDependencies(List<FieldPositionFingerprint> group)
    //     {
    //         // 0) Nếu client đã cung cấp tie-breaker, ưu tiên sử dụng để bảo toàn thứ tự chèn
    //         if (group.Any(g => g.OffsetTieBreaker.HasValue))
    //         {
    //             return group
    //                 .OrderBy(g => g.OffsetTieBreaker ?? double.MaxValue)
    //                 .ThenBy(g => g.FieldName, StringComparer.Ordinal)
    //                 .ToList();
    //         }

    //         // Xây map tên -> index ban đầu để giữ ổn định
    //         var nameToFp = group.ToDictionary(f => f.FieldName, f => f, StringComparer.Ordinal);

    //         // Xây đồ thị phụ thuộc: A -> B nếu A.ContextAfterText chứa <<B>>
    //         var edges = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
    //         var indeg = new Dictionary<string, int>(StringComparer.Ordinal);

    //         foreach (var fp in group)
    //         {
    //             edges[fp.FieldName] = new HashSet<string>(StringComparer.Ordinal);
    //             indeg[fp.FieldName] = 0;
    //         }

    //         // Xây phụ thuộc từ cả ContextAfterText/ContextBeforeText và nhận diện token bị cắt đôi
    //         foreach (var a in group)
    //         {
    //             // 1) Full-token ở ContextAfter của A: A trước B
    //             var afterMentions = ExtractPlaceholderNames(a.ContextAfterText);
    //             foreach (var bName in afterMentions)
    //             {
    //                 if (nameToFp.ContainsKey(bName) && bName != a.FieldName)
    //                 {
    //                     if (edges[a.FieldName].Add(bName)) indeg[bName] = indeg[bName] + 1;
    //                 }
    //             }

    //             // 2) Full-token ở ContextBefore của A: B trước A
    //             var beforeMentions = ExtractPlaceholderNames(a.ContextBeforeText);
    //             foreach (var bName in beforeMentions)
    //             {
    //                 if (nameToFp.ContainsKey(bName) && bName != a.FieldName)
    //                 {
    //                     if (edges[bName].Add(a.FieldName)) indeg[a.FieldName] = indeg[a.FieldName] + 1;
    //                 }
    //             }

    //             // 3) Partial-token bị cắt đôi quanh vị trí chèn
    //             foreach (var b in group)
    //             {
    //                 if (b.FieldName == a.FieldName) continue;
    //                 if (ComesBeforeByPartial(a, b))
    //                 {
    //                     if (edges[a.FieldName].Add(b.FieldName)) indeg[b.FieldName] = indeg[b.FieldName] + 1;
    //                 }
    //                 else if (ComesAfterByPartial(a, b))
    //                 {
    //                     if (edges[b.FieldName].Add(a.FieldName)) indeg[a.FieldName] = indeg[a.FieldName] + 1;
    //                 }
    //             }
    //         }

    //         // Nếu không suy ra được bất kỳ phụ thuộc nào (mọi indegree = 0 và không có cạnh),
    //         // dùng tie-breaker ổn định theo tên trường để tránh đảo thứ tự ngẫu nhiên giữa các lần chạy
    //         bool hasAnyEdge = edges.Values.Any(set => set.Count > 0);
    //         if (!hasAnyEdge && indeg.Values.All(v => v == 0))
    //         {
    //             return group
    //                 .OrderBy(f => f.FieldName, StringComparer.Ordinal)
    //                 .ToList();
    //         }

    //         // Kahn topo với ổn định thứ tự gốc khi tie
    //         var queue = new Queue<FieldPositionFingerprint>(group.Where(f => indeg[f.FieldName] == 0));
    //         var result = new List<FieldPositionFingerprint>();

    //         // Duy trì thứ tự ổn định: khởi tạo queue theo thứ tự xuất hiện trong group
    //         queue = new Queue<FieldPositionFingerprint>(group.Where(f => indeg[f.FieldName] == 0));

    //         var visited = new HashSet<string>(StringComparer.Ordinal);
    //         while (queue.Count > 0)
    //         {
    //             var u = queue.Dequeue();
    //             if (!visited.Add(u.FieldName)) continue;
    //             result.Add(u);

    //             foreach (var v in edges[u.FieldName])
    //             {
    //                 indeg[v] = indeg[v] - 1;
    //             }

    //             // Enqueue các node mới có indegree=0 theo thứ tự gốc
    //             foreach (var f in group)
    //             {
    //                 if (!visited.Contains(f.FieldName) && indeg[f.FieldName] == 0 && !queue.Any(q => q.FieldName == f.FieldName))
    //                 {
    //                     queue.Enqueue(f);
    //                 }
    //             }
    //         }

    //         // Nếu còn node chưa thăm (chu trình hoặc không thể xác định), nối theo thứ tự gốc
    //         if (result.Count < group.Count)
    //         {
    //             foreach (var f in group)
    //             {
    //                 if (!result.Any(x => x.FieldName == f.FieldName))
    //                     result.Add(f);
    //             }
    //         }

    //         return result;
    //     }

    //     private static bool ComesBeforeByPartial(FieldPositionFingerprint a, FieldPositionFingerprint b)
    //     {
    //         // Heuristic bảo thủ: partial gợi ý A đang chen giữa token của B -> ưu tiên B trước A (xử lý ở ComesAfterByPartial)
    //         // Vì vậy ở đây trả về false để tránh khẳng định A trước B bằng partial mơ hồ
    //         return false;
    //     }

    //     private static bool ComesAfterByPartial(FieldPositionFingerprint a, FieldPositionFingerprint b)
    //     {
    //         // Trường hợp ContextBefore của A kết thúc bằng "<<{b}" hoặc "<<{b}>" => B trước A
    //         var before = a.ContextBeforeText ?? string.Empty;
    //         if (before.EndsWith($"<<{b.FieldName}", StringComparison.Ordinal)
    //             || before.EndsWith($"<<{b.FieldName}>", StringComparison.Ordinal)
    //             || before.EndsWith($"<<{b.FieldName}>>", StringComparison.Ordinal))
    //         {
    //             return true; // B trước A
    //         }
    //         // Hoặc A đang đứng giữa token mở của B: before kết thúc bằng "<"/"<<" và after có một phần của B
    //         var after = a.ContextAfterText ?? string.Empty;
    //         if ((before.EndsWith("<", StringComparison.Ordinal) || before.EndsWith("<<", StringComparison.Ordinal))
    //             && (after.Contains($"<{b.FieldName}", StringComparison.Ordinal)
    //                 || after.Contains($"{b.FieldName}>>", StringComparison.Ordinal)
    //                 || after.StartsWith($"{b.FieldName}", StringComparison.Ordinal)
    //                 || after.StartsWith($">{b.FieldName}", StringComparison.Ordinal)))
    //         {
    //             return true;
    //         }
    //         return false;
    //     }

    //     private static List<string> ExtractPlaceholderNames(string? text)
    //     {
    //         var result = new List<string>();
    //         if (string.IsNullOrEmpty(text)) return result;
    //         foreach (Match m in Regex.Matches(text, @"<<([^>]+)>>"))
    //         {
    //             if (m.Groups.Count > 1)
    //             {
    //                 var name = m.Groups[1].Value;
    //                 if (!string.IsNullOrWhiteSpace(name)) result.Add(name);
    //             }
    //         }
    //         return result;
    //     }

    //     /// <summary>
    //     /// [HÀM MỚI - CỐT LÕI] Tái tạo lại hoàn toàn một Paragraph với các placeholder được chèn vào.
    //     /// </summary>
    //     private void RebuildParagraphWithPlaceholders(Paragraph paragraph, List<FieldPositionFingerprint> fingerprints)
    //     {
    //         // Helper: tìm cỡ chữ (half-points) gần nhất từ ParagraphMark hoặc từ các paragraph lân cận trong cùng ô bảng
    //         static string ResolveFontSizeHalfPoints(Paragraph para)
    //         {
    //             // 1) Paragraph mark run properties
    //             var markSz = para.ParagraphProperties?
    //                 .ParagraphMarkRunProperties?
    //                 .Elements<FontSize>()
    //                 .FirstOrDefault()?.Val;
    //             if (!string.IsNullOrWhiteSpace(markSz)) return markSz!;

    //             // 2) Trong cùng TableCell: tìm paragraph trước/sau có run/mark có size
    //             if (para.Parent is TableCell cell)
    //             {
    //                 var paras = cell.Elements<Paragraph>().ToList();
    //                 var idx = paras.IndexOf(para);
    //                 if (idx >= 0)
    //                 {
    //                     // backward
    //                     for (int i = idx - 1; i >= 0; i--)
    //                     {
    //                         var p = paras[i];
    //                         var rpSz = p.Elements<Run>()
    //                                     .Select(r => r.RunProperties?.Elements<FontSize>().FirstOrDefault()?.Val)
    //                                     .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    //                         if (!string.IsNullOrWhiteSpace(rpSz)) return rpSz!;

    //                         var pmSz = p.ParagraphProperties?
    //                                     .ParagraphMarkRunProperties?
    //                                     .Elements<FontSize>()
    //                                     .FirstOrDefault()?.Val;
    //                         if (!string.IsNullOrWhiteSpace(pmSz)) return pmSz!;
    //                     }
    //                     // forward
    //                     for (int i = idx + 1; i < paras.Count; i++)
    //                     {
    //                         var p = paras[i];
    //                         var rpSz = p.Elements<Run>()
    //                                     .Select(r => r.RunProperties?.Elements<FontSize>().FirstOrDefault()?.Val)
    //                                     .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    //                         if (!string.IsNullOrWhiteSpace(rpSz)) return rpSz!;

    //                         var pmSz = p.ParagraphProperties?
    //                                     .ParagraphMarkRunProperties?
    //                                     .Elements<FontSize>()
    //                                     .FirstOrDefault()?.Val;
    //                         if (!string.IsNullOrWhiteSpace(pmSz)) return pmSz!;
    //                     }
    //                 }
    //             }
    //             // 3) Fallback 11pt = 22 half-points
    //             return "22";
    //         }
    //         // Gộp các placeholder cần chèn theo vị trí offset, BẢO TOÀN THỨ TỰ người dùng đã chèn
    //         // Không sắp xếp theo tên trường nữa để tránh đảo vị trí
    //         var insertions = new Dictionary<int, List<string>>();
    //         foreach (var fp in fingerprints)
    //         {
    //             if (!insertions.TryGetValue(fp.OffsetInParagraph, out var list))
    //             {
    //                 list = new List<string>();
    //                 insertions[fp.OffsetInParagraph] = list;
    //             }
    //             list.Add($"<<{fp.FieldName}>>");
    //         }

    //         var originalRuns = paragraph.Elements<Run>().ToList();
    //         var isParagraphEmpty = !originalRuns.Any();

    //         // Base RunProperties cho placeholder trong trường hợp paragraph trống.
    //         // - Font: Times New Roman
    //         // - Không bold/italic
    //         // - Size: ưu tiên ParagraphMarkRunProperties nếu có; fallback 11pt (22 half-points)
    //         RunProperties BuildPlaceholderBaseRunProps()
    //         {
    //             var rp = new RunProperties();
    //             rp.RunFonts = new RunFonts
    //             {
    //                 Ascii = "Times New Roman",
    //                 HighAnsi = "Times New Roman",
    //                 ComplexScript = "Times New Roman"
    //             };
    //             rp.Bold = new Bold { Val = false };
    //             rp.Italic = new Italic { Val = false };

    //             var resolved = ResolveFontSizeHalfPoints(paragraph);
    //             rp.FontSize = new FontSize { Val = resolved };
    //             return rp;
    //         }
    //         var placeholderBaseRunProps = BuildPlaceholderBaseRunProps();
    //         var newRuns = new List<Run>();
    //         int cumulativeOffset = 0;

    //         foreach (var run in originalRuns)
    //         {
    //             var runProperties = run.RunProperties?.CloneNode(true) as RunProperties;
                
    //             // CHÈN TRƯỚC RANH GIỚI RUN: nếu có điểm chèn đúng tại vị trí hiện tại
    //             if (insertions.TryGetValue(cumulativeOffset, out var placeholdersAtRunBoundary))
    //             {
    //                 foreach (var placeholder in placeholdersAtRunBoundary)
    //                 {
    //                     var placeholderRun = new Run(new Text(placeholder) { Space = SpaceProcessingModeValues.Preserve });
    //                     if (isParagraphEmpty)
    //                     {
    //                         placeholderRun.RunProperties = (RunProperties)placeholderBaseRunProps.CloneNode(true);
    //                     }
    //                     else if (runProperties != null)
    //                     {
    //                         placeholderRun.RunProperties = (RunProperties)runProperties.CloneNode(true);
    //                     }
    //                     newRuns.Add(placeholderRun);
    //                 }
    //                 insertions.Remove(cumulativeOffset);
    //             }

    //             // Duyệt qua các phần tử con của Run (Text, Tab, Break...)
    //             foreach (var element in run.ChildElements.ToList()) // ToList để tạo bản sao
    //             {
    //                 if (element is Text text)
    //                 {
    //                     string currentText = text.Text ?? "";
    //                     int processedLength = 0;

    //                     while (processedLength < currentText.Length)
    //                     {
    //                         // Kiểm tra xem có điểm chèn nào trong đoạn text này không
    //                         if (insertions.TryGetValue(cumulativeOffset, out var placeholdersToInsert))
    //                         {
    //                             // Chèn tất cả placeholder tại vị trí này
    //                             foreach (var placeholder in placeholdersToInsert)
    //                             {
    //                                 var placeholderRun = new Run(new Text(placeholder) { Space = SpaceProcessingModeValues.Preserve });
    //                                 if (isParagraphEmpty)
    //                                 {
    //                                     placeholderRun.RunProperties = (RunProperties)placeholderBaseRunProps.CloneNode(true);
    //                                 }
    //                                 else if (runProperties != null)
    //                                 {
    //                                     placeholderRun.RunProperties = (RunProperties)runProperties.CloneNode(true);
    //                                 }
    //                                 newRuns.Add(placeholderRun);
    //                             }
    //                             insertions.Remove(cumulativeOffset); // Xóa để không xử lý lại
    //                         }

    //                         // Tìm điểm chèn tiếp theo
    //                         var nextInsertionOffset = insertions.Keys
    //                             .Where(k => k > cumulativeOffset)
    //                             .DefaultIfEmpty(-1)
    //                             .Min();
                            
    //                         int charsToProcess = currentText.Length - processedLength;
    //                         if (nextInsertionOffset != -1 && nextInsertionOffset < cumulativeOffset + charsToProcess)
    //                         {
    //                             charsToProcess = nextInsertionOffset - cumulativeOffset;
    //                         }

    //                         if (charsToProcess > 0)
    //                         {
    //                             var subText = currentText.Substring(processedLength, charsToProcess);
    //                             var textRun = new Run(new Text(subText) { Space = SpaceProcessingModeValues.Preserve });
    //                             if (runProperties != null)
    //                                 textRun.RunProperties = (RunProperties)runProperties.CloneNode(true);
    //                             newRuns.Add(textRun);

    //                             cumulativeOffset += charsToProcess;
    //                             processedLength += charsToProcess;
    //                         }
    //                     }
    //                 }
    //                 else if (element is TabChar)
    //                 {
    //                     // Xử lý điểm chèn ngay tại vị trí của Tab
    //                     if (insertions.TryGetValue(cumulativeOffset, out var placeholdersToInsert))
    //                     {
    //                         foreach (var placeholder in placeholdersToInsert)
    //                         {
    //                             var placeholderRun = new Run(new Text(placeholder) { Space = SpaceProcessingModeValues.Preserve });
    //                             if (isParagraphEmpty)
    //                             {
    //                                 placeholderRun.RunProperties = (RunProperties)placeholderBaseRunProps.CloneNode(true);
    //                             }
    //                             else if (runProperties != null)
    //                             {
    //                                 placeholderRun.RunProperties = (RunProperties)runProperties.CloneNode(true);
    //                             }
    //                             newRuns.Add(placeholderRun);
    //                         }
    //                         insertions.Remove(cumulativeOffset);
    //                     }

    //                     // Thêm lại Tab
    //                     var tabRun = new Run(new TabChar());
    //                     if (runProperties != null)
    //                         tabRun.RunProperties = (RunProperties)runProperties.CloneNode(true);
    //                     newRuns.Add(tabRun);
                        
    //                     cumulativeOffset++; // Coi Tab là 1 ký tự
    //                 }
    //                 else
    //                 {
    //                     // Trước khi thêm phần tử không phải Text/Tab, kiểm tra chèn tại vị trí hiện tại
    //                     if (insertions.TryGetValue(cumulativeOffset, out var placeholdersBeforeOther))
    //                     {
    //                         foreach (var placeholder in placeholdersBeforeOther)
    //                         {
    //                             var placeholderRun = new Run(new Text(placeholder) { Space = SpaceProcessingModeValues.Preserve });
    //                             if (runProperties != null)
    //                                 placeholderRun.RunProperties = (RunProperties)runProperties.CloneNode(true);
    //                             newRuns.Add(placeholderRun);
    //                         }
    //                         insertions.Remove(cumulativeOffset);
    //                     }

    //                     // Giữ lại các phần tử khác như Break, Drawing, FieldChar...
    //                     var otherRun = new Run(element.CloneNode(true));
    //                     if (runProperties != null)
    //                         otherRun.RunProperties = (RunProperties)runProperties.CloneNode(true);
    //                     newRuns.Add(otherRun);
    //                 }
    //             }
    //         }

    //         // Xử lý các placeholder còn lại (chèn ở cuối)
    //         if (insertions.TryGetValue(cumulativeOffset, out var lastPlaceholders))
    //         {
    //             var lastRunProps = originalRuns.LastOrDefault()?.RunProperties;
    //             foreach (var placeholder in lastPlaceholders)
    //             {
    //                 var placeholderRun = new Run(new Text(placeholder) { Space = SpaceProcessingModeValues.Preserve });
    //                 if (isParagraphEmpty)
    //                 {
    //                     placeholderRun.RunProperties = (RunProperties)placeholderBaseRunProps.CloneNode(true);
    //                 }
    //                 else if (lastRunProps != null)
    //                 {
    //                     placeholderRun.RunProperties = (RunProperties)lastRunProps.CloneNode(true);
    //                 }
    //                 newRuns.Add(placeholderRun);
    //             }
    //         }

    //         // Xóa nội dung cũ và thêm nội dung đã tái tạo
    //         paragraph.RemoveAllChildren<Run>();
    //         paragraph.Append(newRuns);
    //     }

    //     //HELPER
    //     //OpenXML SDK không có hàm GetPartByUri() sẵn cho WordprocessingDocument
    //     private OpenXmlPart? GetPartByUri(WordprocessingDocument doc, Uri partUri)
    //     {
    //         return doc.Parts
    //                 .Select(p => p.OpenXmlPart)
    //                 .FirstOrDefault(p => p.Uri == partUri);
    //     }

    }
}
