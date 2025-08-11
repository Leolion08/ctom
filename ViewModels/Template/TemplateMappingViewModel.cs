// File: ViewModels/Template/TemplateMappingViewModel.cs
// Ghi chú: File này chứa các DTOs dùng để giao tiếp giữa client và server.

#nullable enable

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CTOM.ViewModels.Template
{
    /// <summary>
    /// ViewModel chính cho trang Mapping.
    /// [SỬA ĐỔI] Thêm MappedFieldsJson để truyền dữ liệu mapping đã lưu.
    /// </summary>
    public class TemplateMappingViewModel
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public List<FieldViewModel> AvailableFields { get; set; } = [];
        public string StructuredHtmlContent { get; set; } = string.Empty;
        
        /// <summary>
        /// [SỬA ĐỔI] Chuỗi JSON chứa danh sách các trường đã được ánh xạ (FieldMappingInfo).
        /// Dùng để khởi tạo lại giao diện mapping một cách bền vững.
        /// </summary>
        public string MappedFieldsJson { get; set; } = "[]";
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

    public class FieldViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string DataType { get; set; } = "TEXT";
        public string DataSourceType { get; set; } = "CIF";
        public int DisplayOrder { get; set; } = 0;
    }

    /// <summary>
    /// [SỬA ĐỔI] DTO chính được gửi từ client khi nhấn nút "Lưu".
    /// </summary>
    public class SaveMappingRequest
    {
        public int TemplateId { get; set; }
        public List<FieldMappingInfo> Fields { get; set; } = [];
    }

    /// <summary>
    /// [SỬA ĐỔI] Chứa thông tin đầy đủ của một trường được mapping, bao gồm danh sách các "Dấu vân tay" vị trí.
    /// </summary>
    public class FieldMappingInfo
    {
        public string FieldName { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? DataType { get; set; } = "text";
        public string? DefaultValue { get; set; }
        public bool IsRequired { get; set; }
        public int? DisplayOrder { get; set; }
        public string? Description { get; set; }
        public string? DataSourceType { get; set; }
        public string? CalculationFormula { get; set; }
        public List<FieldPositionFingerprint> Positions { get; set; } = [];
    }

    /// <summary>
    /// [SỬA ĐỔI] Định nghĩa cấu trúc "Dấu vân tay" (Fingerprint) để định vị placeholder.
    /// Đây là cấu trúc cốt lõi của giải pháp, đảm bảo việc lưu và tải lại mapping luôn chính xác.
    /// </summary>
    public class FieldPositionFingerprint
    {
        /// <summary>
        /// [Anker Chính] Hash SHA256 của toàn bộ nội dung text trong Paragraph chứa placeholder.
        /// Giúp tìm lại đúng đoạn văn một cách đáng tin cậy.
        /// </summary>
        [Required]
        public string ParagraphHash { get; set; } = string.Empty;

        /// <summary>
        /// [Anker Phụ] Một đoạn văn bản ngắn (khoảng 20 ký tự) ngay TRƯỚC vị trí con trỏ.
        /// Dùng để định vị chính xác điểm chèn bên trong một đoạn văn.
        /// </summary>
        public string ContextBeforeText { get; set; } = string.Empty;

        /// <summary>
        /// [Anker Phụ] Một đoạn văn bản ngắn (khoảng 20 ký tự) ngay SAU vị trí con trỏ.
        /// Kết hợp với ContextBeforeText để tạo ra một "khung" định vị duy nhất.
        /// </summary>
        public string ContextAfterText { get; set; } = string.Empty;

        /// <summary>
        /// Vị trí của ký tự (offset) tương đối bên trong thẻ <span> (run) chứa điểm chèn.
        /// </summary>
        public int CharOffsetInRun { get; set; }

        /// <summary>
        /// [Dự phòng] Đường dẫn cấu trúc do server tạo ra (ví dụ: "body.p[5].r[2]").
        /// Được sử dụng như phương án cuối cùng nếu các anker chính thất bại.
        /// </summary>
        public string StructuralPath { get; set; } = string.Empty;

        /// <summary>
        /// [THÊM MỚI] Lưu độ sâu của bảng lồng nhau (0 = không trong bảng, 1 = bảng cha, 2+ = bảng con).
        /// Thông tin này được lấy từ thuộc tính `data-nested-depth` ở client.
        /// </summary>
        public int NestedDepth { get; set; } = 0;
    }
}
