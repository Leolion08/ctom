using System;

namespace CTOM.ViewModels.Form;

/// <summary>
/// Lightweight item view model representing a single form transaction on the listing page.
/// </summary>
/// <param name="Id">Primary key</param>
/// <param name="FormCode">Business code of the form</param>
/// <param name="FormName">Display name of the form</param>
/// <param name="IsActive">Activation flag</param>
/// <param name="UpdatedAt">Last modification timestamp</param>
public sealed record FormListItemVM(
    int FormDataId,
    int TemplateId,
    string TemplateName,
    string? Note,
    string SoCif,
    string TenCif,
    string? CreatedDepartmentID,
    string CreatedByUserName,
    DateTime CreationTimestamp,
    DateTime LastModificationTimestamp,
    string Status);
