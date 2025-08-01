namespace CTOM.ViewModels.Form;

/// <summary>
/// ViewModel cho bước 1 của luồng tạo Form động – chọn Template.
/// </summary>
using System.ComponentModel.DataAnnotations;
public sealed record FormCreateStep1VM
{
    [Display(Name = "Template")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn Template")]
    public int TemplateId { get; init; }

    //[Display(Name = "Ghi chú")]
    //public string? Note { get; init; }
}
