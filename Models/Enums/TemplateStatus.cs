using System.ComponentModel.DataAnnotations;

namespace CTOM.Models.Enums
{
    public enum TemplateStatus
    {
        [Display(Name = "Bản nháp")]
        Draft,

        [Display(Name = "Đã ánh xạ")]
        Mapped
    }
}
