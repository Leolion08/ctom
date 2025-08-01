using System;

namespace CTOM.ViewModels.Form;

public sealed class FormListItemViewModel
{
    public long FormDataId { get; init; }
    public int TemplateId { get; init; }

    public string TemplateName { get; init; } = string.Empty;

    public string SoCif { get; init; } = string.Empty;
    public string? TenCif { get; init; }

    public string? Note { get; init; }

    public string CreatedBy { get; init; } = string.Empty;
    public string? CreatedDepartmentId { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public string? Status { get; init; }
}
