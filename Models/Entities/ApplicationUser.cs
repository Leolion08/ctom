using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTOM.Models.Entities
{
    public class ApplicationUser : IdentityUser // Kế thừa IdentityUser<string>
    {
        // Thuộc tính tùy chỉnh TenUser (map từ yêu cầu TenUser)
        [PersonalData] // Đánh dấu dữ liệu cá nhân
        [Required(ErrorMessage = "Họ và Tên người dùng là bắt buộc.")]
        [StringLength(50)]
        [Display(Name = "Họ và Tên")] // Label hiển thị trên UI
        public required string TenUser { get; set; }

        // Thuộc tính tùy chỉnh MaPhong (map từ yêu cầu MaPhong)
        [Required(ErrorMessage = "Phòng ban là bắt buộc.")]
        [StringLength(20)]
        [Display(Name = "Phòng Ban")]
        public required string MaPhong { get; set; } // Khóa ngoại

        // Thuộc tính tùy chỉnh TrangThai (map từ yêu cầu TrangThai)
        [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
        [StringLength(1, MinimumLength = 1)]
        [Display(Name = "Trạng thái")] // A: Active, D: Disabled
        public string TrangThai { get; set; } = "A";

        // --- Navigation Properties ---
        [ForeignKey("MaPhong")]
        public virtual PhongBan? PhongBan { get; set; } // Liên kết đến Phòng Ban

        // Không cần thuộc tính MaNhomQuyen ở đây
        // Mối quan hệ User-Role được quản lý bởi Identity qua bảng riêng

        // Các thuộc tính kế thừa từ IdentityUser (Id, UserName, Email, PasswordHash...) vẫn tồn tại
        // UserName sẽ được dùng để lưu MaUser
    }
}