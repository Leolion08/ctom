using System;
using System.ComponentModel.DataAnnotations;

namespace CTOM.ViewModels.Template;

/// <summary>
/// ViewModel cho trang Template/Details – chỉ dùng để hiển thị (read-only)
/// </summary>
public class TemplateDetailsVM
{
    public required int TemplateId { get; init; }

    [Display(Name = "Mã Template")]
    public string TemplateCode => $"TPL-{TemplateId}"; // Hiển thị mã định dạng, có thể chỉnh sửa nếu cần.

    [Display(Name = "Tên nghiệp vụ")]
    public string? BusinessOperationName { get; init; }

    [Display(Name = "Tên Template")]
    public required string TemplateName { get; init; }

    [Display(Name = "Mô tả")]
    public string? Description { get; init; }

    [Display(Name = "Trạng thái")]
    public required string Status { get; init; }

    [Display(Name = "Đang sử dụng")]
    public bool IsActive { get; init; }

    /// <summary>
    /// Người dùng hiện tại có thể thực hiện thao tác Ánh xạ (Mapping) hay không.
    /// Được tính toán tại controller (chỉ đọc).
    /// </summary>
    public bool CanMapping { get; init; }

    // ---------- Các trường hỗ trợ hiển thị file ----------
    /// <summary>
    /// Nội dung HTML đã được convert từ file docx bằng DocxToStructuredHtmlService
    /// </summary>
    public string? HtmlContent { get; init; }

    /// <summary>
    /// Field cho biết template có file docx và đã được convert thành công hay không.
    /// </summary>
    public bool HasFile { get; init; }

    public string MappedFieldsJson { get; set; } = "[]"; //Thông tin mapping của các field

    // ---------- Thông tin tạo / sửa ----------
    [Display(Name = "Người tạo")]
    public string? CreatedByUserName { get; init; }

    [Display(Name = "Phòng ban tạo")]
    public string? CreatedDepartmentID { get; init; }

    [Display(Name = "Ngày tạo")]
    public DateTime? CreationTimestamp { get; init; }

    [Display(Name = "Ngày sửa cuối")]
    public DateTime? LastModificationTimestamp { get; init; }

    [Display(Name = "Người sửa cuối")]
    public string? LastModifiedByUserName { get; init; }
}
