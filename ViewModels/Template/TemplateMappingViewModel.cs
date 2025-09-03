// File: ViewModels/Template/TemplateMappingViewModel.cs
// Ghi chú: File này chứa các DTOs dùng để giao tiếp giữa client và server.

#nullable enable

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json; // Sử dụng Newtonsoft để nhất quán

namespace CTOM.ViewModels.Template
{
    /// <summary>
    /// ViewModel chính cho trang Mapping.
    /// </summary>
    public class TemplateMappingViewModel
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public List<FieldViewModel> AvailableFields { get; set; } = [];
        public string StructuredHtmlContent { get; set; } = string.Empty;
        
        /// <summary>
        /// Chuỗi JSON chứa danh sách các trường đã được ánh xạ (FieldMappingInfo).
        /// Dùng để khởi tạo lại giao diện mapping.
        /// </summary>
        public string MappedFieldsJson { get; set; } = "[]";
    }

    public class FieldViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string DataType { get; set; } = "TEXT";
        public string DataSourceType { get; set; } = "CIF";
        public int DisplayOrder { get; set; } = 0;
    }

    /// <summary>
    /// Thông tin trường dữ liệu có sẵn để mapping
    /// </summary>
    public class AvailableField
    {
        /// <summary>
        /// Tên trường (dùng để mapping)
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tên hiển thị
        /// </summary>
        [Required]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Mô tả về trường
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Kiểu dữ liệu của trường (text, textarea, number, date, ...)
        /// </summary>
        public string? FieldType { get; set; }
        /// <summary>
        /// Loại nguồn dữ liệu (CIF, INPUT, CALC, ...)
        /// </summary>
        public string? DataSourceType { get; set; }

        /// <summary>
        /// Chỉ số hiển thị
        /// </summary>
        public int? DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Công thức tính toán cho trường loại CALC
        /// </summary>
        public string? CalculationFormula { get; set; }
    }

    // GHI CHÚ: Class FieldMappingInfo cũ đã được loại bỏ và thay thế bằng FieldPositionFingerprint.

    /// <summary>
    /// [KIẾN TRÚC MỚI] Đại diện cho "Dấu vân tay bền vững" của một vị trí mapping.
    /// Cấu trúc này được thiết kế để chống lại sự thay đổi cấu trúc (run/text) của tài liệu.
    /// Nó sẽ được serialize thành JSON và lưu vào TemplateField.MappingPositionsJson.
    /// </summary>
    public class FieldPositionFingerprint
    {
        /// <summary>
        /// Tên của trường được chèn, ví dụ: "DaiDien", "SoCif".
        /// </summary>
        [JsonProperty("fieldName")]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// [Anker Chính 1] URI của Document Part chứa đoạn văn.
        /// Ví dụ: "/word/document.xml", "/word/header1.xml".
        /// Giúp xác định đúng khu vực (main, header, footer) cần chỉnh sửa.
        /// </summary>
        [JsonProperty("partUri")]
        public string PartUri { get; set; } = string.Empty;

        /// <summary>
        /// [Anker Chính 2] ID duy nhất và bất biến của đoạn văn (w14:paraId).
        /// Đây là mỏ neo đáng tin cậy nhất để tìm lại đúng đoạn văn.
        /// </summary>
        [JsonProperty("paragraphId")]
        public string ParagraphId { get; set; } = string.Empty;

        /// <summary>
        /// [Anker Chính 3] Vị trí (offset) của điểm chèn, tính từ đầu chuỗi văn bản thuần túy của cả đoạn văn.
        /// Ví dụ: "Kính gửi Ông/Bà: <<TenKhachHang>>" -> OffsetInParagraph của TenKhachHang là 19.
        /// </summary>
        [JsonProperty("offsetInParagraph")]
        public int OffsetInParagraph { get; set; }

        /// <summary>
        /// [Hỗ trợ UI/Debug] Một đoạn văn bản ngắn (khoảng 20 ký tự) ngay TRƯỚC vị trí chèn.
        /// Giúp người dùng và dev dễ dàng nhận biết vị trí trên giao diện.
        /// </summary>
        [JsonProperty("contextBeforeText")]
        public string ContextBeforeText { get; set; } = string.Empty;

        /// <summary>
        /// [Hỗ trợ UI/Debug] Một đoạn văn bản ngắn (khoảng 20 ký tự) ngay SAU vị trí chèn.
        /// </summary>
        [JsonProperty("contextAfterText")]
        public string ContextAfterText { get; set; } = string.Empty;

        /// <summary>
        /// [Tie-breaker] Chỉ số phụ (số thực) dùng để phân biệt thứ tự khi nhiều placeholder cùng OffsetInParagraph trong cùng ParagraphId.
        /// Được gán phía client theo thứ tự chèn và có thể là số thập phân để hỗ trợ chèn "lọt giữa" (ví dụ: -1, -0.5, 0, 0.5, 1,...).
        /// </summary>
        [JsonProperty("offsetTieBreaker")]
        public double? OffsetTieBreaker { get; set; }
        
        /// <summary>
        /// Độ sâu bảng lồng hiện tại tại vị trí chèn.
        /// 0 = không trong bảng, 1 = bảng cha, 2 = bảng con cấp 1, ...
        /// </summary>
        [JsonProperty("nestedDepth")]
        public int NestedDepth { get; set; } = 0;
    }

    /// <summary>
    /// DTO dùng để client gửi thông tin mapping lên server.
    /// </summary>
    public class SaveMappingRequest
    {
        [Required]
        public int TemplateId { get; set; }

        /// <summary>
        /// Danh sách tất cả các "dấu vân tay" vị trí của các trường trên tài liệu.
        /// Client sẽ thu thập thông tin này và gửi lên.
        /// </summary>
        public List<FieldPositionFingerprint> Fingerprints { get; set; } = [];

        /// <summary>
        /// Danh sách các trường với thông tin đầy đủ (bao gồm DataType và DataSourceType)
        /// </summary>
        public List<FieldViewModel>? Fields { get; set; }
    }

    // Đã sử dụng FieldViewModel thay thế cho FieldInfo
}
