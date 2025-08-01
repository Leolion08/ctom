using System.ComponentModel.DataAnnotations;

namespace CTOM.ViewModels.PhongBan;

public abstract class PhongBanBaseViewModel
{
    [Display(Name = "Tên phòng")]
    [Required(ErrorMessage = "Vui lòng nhập Tên phòng.")]
    [StringLength(50, ErrorMessage = "Tên phòng không được vượt quá 50 ký tự.")]
    public required string TenPhong { get; set; }

    [Display(Name = "Tên viết tắt")]
    [StringLength(20, ErrorMessage = "Tên viết tắt không được vượt quá 20 ký tự.")]
    public string? TenVietTat { get; set; }

    [Display(Name = "Mã phòng HR")]
    [StringLength(20, ErrorMessage = "Mã phòng HR không được vượt quá 20 ký tự.")]
    public string? MaPhongHR { get; set; }

    [Display(Name = "Trạng thái")]
    [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
    [StringLength(1)]
    public required string TrangThai { get; set; } = "A";
}
