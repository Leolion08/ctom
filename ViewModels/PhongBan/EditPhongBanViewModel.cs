using System.ComponentModel.DataAnnotations;

namespace CTOM.ViewModels.PhongBan;

public class EditPhongBanViewModel : PhongBanBaseViewModel
{
    [Display(Name = "Mã phòng")]
    public required string MaPhong { get; set; } = string.Empty;  // Chỉ đọc, không cho sửa
}
