using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTOM.Models.Entities
{
    public class PhongBan // Tên class tiếng Việt
    {
        [Key]
        [StringLength(20)]
        [Display(Name = "Mã phòng")]
        [Required(ErrorMessage = "Vui lòng nhập Mã phòng.")]
        public required string MaPhong { get; set; } // PK là MaPhong ('DTTT', 'KHDN1')

        [StringLength(20)]
        [Display(Name = "Mã phòng HR")]
        public string? MaPhongHR { get; set; }

        [StringLength(50)]
        [Display(Name = "Tên phòng")]
        [Required(ErrorMessage = "Vui lòng nhập Tên phòng.")]
        public required string TenPhong { get; set; }

        [StringLength(20)]
        [Display(Name = "Tên viết tắt")]
        public string? TenVietTat { get; set; }

        [Required] // Theo file docx Nullable=Y nhưng Default=A -> Ưu tiên NOT NULL
        [StringLength(1, MinimumLength = 1)]
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "A";

        // Navigation properties
        public virtual ICollection<ApplicationUser> NguoiSuDungs { get; set; } = new List<ApplicationUser>(); // Collection of ApplicationUser
    }
}