using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CTOM.Models.Config
{
    // Lớp đại diện cho cấu hình của một trường trong file Excel
    /// <summary>
    /// Lớp đại diện cho cấu hình của một trường trong file Excel
    /// </summary>
    public class FieldConfig
    {
        /// <summary>
        /// Tên trường tương ứng trong database/entity
        /// </summary>
        [JsonPropertyName("field")]
        public string? Field { get; set; }

        /// <summary>
        /// Nhãn hiển thị trong Excel (tên cột)
        /// </summary>
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Trường có bắt buộc không, mặc định là false nếu thiếu
        /// </summary>
        [JsonPropertyName("required")]
        public bool Required { get; set; } = false;

        /// <summary>
        /// Độ dài tối đa cho phép, null nếu không quy định
        /// </summary>
        [JsonPropertyName("maxLength")]
        public int? MaxLength { get; set; }

        /// <summary>
        /// Độ dài tối thiểu cho phép, null nếu không quy định
        /// </summary>
        [JsonPropertyName("minLength")]
        public int? MinLength { get; set; }

        /// <summary>
        /// Kiểu dữ liệu: "string", "email", "date", "number". Null nếu không quy định.
        /// </summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Định dạng cho kiểu "date" hoặc "number". Null nếu không quy định.
        /// </summary>
        [JsonPropertyName("format")]
        public string? Format { get; set; }

        /// <summary>
        /// Mẫu Regex tùy chỉnh để validate. Null nếu không quy định.
        /// </summary>
        [JsonPropertyName("regexPattern")]
        public string? RegexPattern { get; set; }

        // Ví dụ về một thuộc tính khác có thể có hoặc không
        // [JsonPropertyName("allowedValues")]
        // public List<string>? AllowedValues { get; set; }
    }

    // Lớp ExcelImportConfig không thay đổi cấu trúc, chỉ là FieldConfig bên trong nó thay đổi
    public class ExcelImportConfig
    {
        [JsonPropertyName("maxRows")]
        public int MaxRows { get; set; } = 100;

        [JsonPropertyName("maxFileSizeMB")]
        public double MaxFileSizeMB { get; set; } = 1;

        [JsonPropertyName("fields")]
        public Dictionary<string, FieldConfig> Fields { get; set; } = new Dictionary<string, FieldConfig>(); // Khởi tạo để tránh null
    }
}
