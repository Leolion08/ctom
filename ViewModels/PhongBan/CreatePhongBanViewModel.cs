using System.ComponentModel.DataAnnotations;

namespace CTOM.ViewModels.PhongBan;

public class CreatePhongBanViewModel : PhongBanBaseViewModel
{
    [Display(Name = "Mã phòng")]
    [Required(ErrorMessage = "Vui lòng nhập Mã phòng.")]
    [StringLength(20, ErrorMessage = "Mã phòng không được vượt quá 20 ký tự.")]
    public required string MaPhong { get; set; } = string.Empty;
}
