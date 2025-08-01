using System.Collections.Generic;

namespace CTOM.ViewModels.Form;

/// <summary>
/// ViewModel cho bước 2 – dữ liệu nhập từ form động.
/// </summary>
public sealed record FormCreateStep2VM(
    int TemplateId,
    string? Note,
    Dictionary<string, string?> FormValues
);
