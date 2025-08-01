using System.Text.Json.Serialization;

namespace CTOM.Models.DTOs;

/// <summary>
/// Lớp lưu trữ thông tin lỗi khi import dữ liệu
/// </summary>
public class ImportError
{
    /// <summary>
    /// Số dòng xảy ra lỗi (bắt đầu từ 1)
    /// </summary>
    [JsonPropertyName("rowNumber")]
    public int RowNumber { get; set; }
    
    /// <summary>
    /// Tên cột xảy ra lỗi (nếu có)
    /// </summary>
    [JsonPropertyName("columnName")]
    public string? ColumnName { get; set; }
    
    /// <summary>
    /// Giá trị không hợp lệ (nếu có)
    /// </summary>
    [JsonPropertyName("invalidValue")]
    public string? InvalidValue { get; set; }
    
    /// <summary>
    /// Thông báo lỗi
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Mức độ nghiêm trọng của lỗi
    /// </summary>
    [JsonPropertyName("severity")]
    public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
}

/// <summary>
/// Mức độ nghiêm trọng của lỗi
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ErrorSeverity
{
    /// <summary>
    /// Cảnh báo - có thể bỏ qua
    /// </summary>
    Warning,
    
    /// <summary>
    /// Lỗi - cần sửa để tiếp tục
    /// </summary>
    Error,
    
    /// <summary>
    /// Lỗi nghiêm trọng - không thể xử lý
    /// </summary>
    Critical
}
