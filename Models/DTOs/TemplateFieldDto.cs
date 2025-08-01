namespace CTOM.Models.DTOs;

/// <summary>
/// DTO đại diện cho một trường cấu hình trong TemplateFields bảng.
/// </summary>
public sealed record TemplateFieldDto(
    string FieldName,
    string DisplayName,
    string DataType,
    bool IsRequired,
    string? DataSourceType
);
