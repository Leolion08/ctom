using CTOM.Models.DTOs;
using System.Text.Json.Serialization;

namespace CTOM.Models.Responses;

/// <summary>
/// Lớp phản hồi khi import dữ liệu
/// </summary>
public class ImportResponse
{
    /// <summary>
    /// Tổng số bản ghi đã xử lý
    /// </summary>
    [JsonPropertyName("totalRows")]
    public int TotalRows { get; set; }
    
    /// <summary>
    /// Số bản ghi xử lý thành công
    /// </summary>
    [JsonPropertyName("successCount")]
    public int SuccessCount { get; set; }
    
    /// <summary>
    /// Số lỗi đã phát hiện
    /// </summary>
    [JsonPropertyName("errorCount")]
    public int ErrorCount => Errors?.Count ?? 0;
    
    /// <summary>
    /// Danh sách các lỗi (nếu có)
    /// </summary>
    [JsonPropertyName("errors")]
    public List<ImportError>? Errors { get; set; }
    
    /// <summary>
    /// Có lỗi xảy ra hay không
    /// </summary>
    [JsonPropertyName("hasErrors")]
    public bool HasErrors => ErrorCount > 0;
    
    /// <summary>
    /// Thông điệp mô tả kết quả
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Thời gian xử lý (tính bằng giây)
    /// </summary>
    [JsonPropertyName("processingTimeSeconds")]
    public double ProcessingTimeSeconds { get; set; }
    
    /// <summary>
    /// Trạng thái thực thi (thành công/thất bại)
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    /// <summary>
    /// Thời gian thực thi (tính bằng milliseconds)
    /// </summary>
    [JsonIgnore]
    public TimeSpan ExecutionTime { get; set; }
    
    /// <summary>
    /// Tạo đối tượng phản hồi thành công
    /// </summary>
    public static ImportResponse CreateSuccessResponse(int total, int success, List<ImportError>? errors = null, string? message = null)
    {
        return new ImportResponse
        {
            Success = true,
            TotalRows = total,
            SuccessCount = success,
            Errors = errors ?? [],
            Message = message ?? $"Đã import thành công {success}/{total} bản ghi"
        };
    }
    
    /// <summary>
    /// Tạo đối tượng phản hồi thất bại
    /// </summary>
    public static ImportResponse CreateErrorResponse(string message, List<ImportError> errors)
    {
        return new ImportResponse
        {
            Success = false,
            Errors = errors,
            Message = message
        };
    }
}
