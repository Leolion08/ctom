using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace CTOM.ViewModels.Template
{
    /// <summary>
    /// ViewModel chính cho trang Mapping, chứa nội dung HTML đã được xử lý.
    /// </summary>
    public class TemplateMappingViewModel
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }= string.Empty;
        public List<FieldViewModel> AvailableFields { get; set; }= []; //new();

        /// <summary>
        /// Nội dung HTML của file DOCX đã được phân tích và nhúng metadata.
        /// Client sẽ render trực tiếp nội dung này.
        /// </summary>
        public string StructuredHtmlContent { get; set; }= string.Empty;
    }

    public class FieldViewModel
    {
        public string Name { get; set; }= string.Empty;
        public string DisplayName { get; set; }= string.Empty;
        // --- BỔ SUNG ---
        public string DataType { get; set; } = "TEXT"; // Thêm thuộc tính này
        public string DataSourceType { get; set; } = "CIF";
        public int DisplayOrder { get; set; } = 0;
        // --- HẾT BỔ SUNG ---
    }

    /// <summary>
    /// DTO (Data Transfer Object) được gửi từ client khi nhấn nút "Lưu".
    /// </summary>
    public class SaveMappingRequest
    {
        public int TemplateId { get; set; }
        public List<FieldMappingInfo> Fields { get; set; }= []; //new();
    }

    /// <summary>
    /// Thông tin mapping cho một trường dữ liệu cụ thể.
    /// </summary>
    public class FieldMappingInfo
    {
        public string FieldName { get; set; }= string.Empty;
        //Bổ sung giống TemplateField
        public string? DisplayName { get; set; }
        public string? DataType { get; set; } = "text";
        public string? DefaultValue { get; set; }
        public bool IsRequired { get; set; }= false;
        public int? DisplayOrder { get; set; }= 0;
        public string? Description { get; set; }
        public string? DataSourceType { get; set; }
        public string? CalculationFormula { get; set; }
        //Bổ sung - END
        public List<FieldPosition> Positions { get; set; }= []; //new();
    }

    /// <summary>
    /// Định nghĩa vị trí chèn placeholder theo cấu trúc của DOCX.
    /// Thay thế hoàn toàn cho XPath.
    /// </summary>
    public class FieldPosition
    {
        /// <summary>
        /// Chỉ số của Paragraph (w:p) trong tài liệu, bắt đầu từ 0.
        /// </summary>
        public int ParagraphId { get; set; }= 0;

        /// <summary>
        /// Chỉ số của Run (w:r) chứa text trong Paragraph, bắt đầu từ 0.
        /// </summary>
        public int RunId { get; set; }= 0;

        /// <summary>
        /// Vị trí của ký tự (offset) trong Run để chèn placeholder.
        /// </summary>
        public int CharOffset { get; set; }= 0;

        /// <summary>
        /// ID duy nhất của element (được sinh bởi DocxToStructuredHtmlService)
        /// </summary>
        public string? ElementId { get; set; }

        /// <summary>
        /// Đường dẫn DOCX của element (backup cho debugging)
        /// </summary>
        public string? DocxPath { get; set; }

        /// <summary>
        /// Có phải đang nằm trong bảng con (nested table) không
        /// </summary>
        public bool IsInNestedTable { get; set; } = false;

        /// <summary>
        /// Độ sâu của nested table (0 = không trong table, 1 = table cha, 2+ = table con)
        /// </summary>
        public int NestedDepth { get; set; } = 0;
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
}
