using System.ComponentModel.DataAnnotations;

namespace CTOM.ViewModels
{
    public class CreateRoleViewModel
    {
        [Required(ErrorMessage = "Mã nhóm là bắt buộc.")]
        [Display(Name = "Mã nhóm quyền")]
        [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Mã nhóm chỉ chứa chữ cái, số, dấu gạch dưới, dấu chấm, dấu gạch ngang.")]
        public required string RoleName { get; set; } // Tương ứng với Name (PK logic)

        [Required(ErrorMessage = "Tên nhóm là bắt buộc.")]
        [StringLength(50)]
        [Display(Name = "Tên nhóm quyền")]
        public string? TenNhomDayDu { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
        [StringLength(1, MinimumLength = 1)]
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "A";
    }

    public class EditRoleViewModel
    {
        // Không cần Id ở đây vì dùng Name làm định danh trong route
        // public string Id { get; set; }

        // Name không cho sửa, chỉ dùng để hiển thị và xác định route
        [Required]
        [Display(Name = "Mã nhóm quyền")]
        [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Mã nhóm chỉ chứa chữ cái, số, dấu gạch dưới, dấu chấm, dấu gạch ngang.")]
        public required string RoleName { get; set; }

        [StringLength(50)]
        [Display(Name = "Tên nhóm quyền")]
        public string? TenNhomDayDu { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
        [StringLength(1, MinimumLength = 1)]
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "A";
    }
}
