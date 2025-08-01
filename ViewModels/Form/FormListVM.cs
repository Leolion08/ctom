using System.Collections.Generic;

namespace CTOM.ViewModels.Form;

/// <summary>
/// View model bao gói danh sách form động cùng metadata (paging/filter nếu cần).
/// </summary>
public sealed class FormListVM
{
    public required IReadOnlyCollection<FormListItemVM> Items { get; init; }
}
