using System.Collections.Generic;

namespace CTOM.ViewModels.Form;

/// <summary>
/// ViewModel cho trang Form/Create với hỗ trợ HTML preview từ server.
/// </summary>
public sealed record FormCreateVM
{
    /// <summary>
    /// ID của template được chọn
    /// </summary>
    public int TemplateId { get; init; }
    
    /// <summary>
    /// Tên hiển thị của template
    /// </summary>
    public string? TemplateName { get; init; }
    
    /// <summary>
    /// Ghi chú về form
    /// </summary>
    public string? Note { get; init; }
    
    /// <summary>
    /// Nội dung HTML đã được convert từ file docx bằng DocxToStructuredHtmlService
    /// </summary>
    public string? HtmlContent { get; init; }
    
    /// <summary>
    /// Field cho biết template có file docx và đã được convert thành công hay không.
    /// </summary>
    public bool HasFile { get; init; }
    
    /// <summary>
    /// Các trường dữ liệu động của form
    /// </summary>
    public Dictionary<string, string?>? FormValues { get; init; }
}
