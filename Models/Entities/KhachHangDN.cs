using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CTOM.Models.Entities
{
    // Đặt Index để đảm bảo SoCif là duy nhất
    [Index(nameof(SoCif), IsUnique = true, Name = "IX_KhachHangDN_SoCIF")]
    public class KhachHangDN
    {
        [Key]
        [StringLength(20)]
        [Display(Name = "Số CIF")]
        public string SoCif { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên CIF là bắt buộc.")]
        [StringLength(150)]
        [Display(Name = "Tên CIF")]
        public string TenCif { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Xếp hạng TD nội bộ")]
        public string? XepHangTinDungNoiBo { get; set; }

        [StringLength(100)]
        [Display(Name = "Loại hình DN")]
        public string? LoaiHinhDN { get; set; }

        [StringLength(50)]
        [Display(Name = "Số Giấy chứng nhận ĐKKD")]
        public string? SoGiayChungNhanDKKD { get; set; }

        [StringLength(200)]
        [Display(Name = "Nơi cấp Giấy chứng nhận ĐKKD")]
        public string? NoiCapGiayChungNhanDKKD { get; set; }

        [Display(Name = "Ngày cấp Giấy chứng nhận ĐKKD")]
        [DataType(DataType.Date)]
        public DateTime? NgayCapGiayChungNhanDKKD { get; set; }

        [StringLength(150)]
        [Display(Name = "Tên tiếng Anh")]
        public string? TenTiengAnh { get; set; }

        [StringLength(200)]
        [Display(Name = "Địa chỉ trên ĐKKD")]
        public string? DiaChiTrenDKKD { get; set; }

        [StringLength(200)]
        [Display(Name = "Địa chỉ kinh doanh hiện tại")]
        public string? DiaChiKinhDoanhHienTai { get; set; }

        [StringLength(255)]
        [Display(Name = "Lĩnh vực kinh doanh chính")]
        public string? LinhVucKinhDoanhChinh { get; set; }

        [StringLength(20)]
        [Phone]
        [Display(Name = "Số ĐT DN")]
        public string? SoDienThoaiDN { get; set; }

        [StringLength(20)]
        [Display(Name = "Số Fax công ty")]
        public string? SoFaxCongTy { get; set; }

        [StringLength(50)]
        [EmailAddress]
        [Display(Name = "Email công ty")]
        public string? EmailCongTy { get; set; }

        [StringLength(100)]
        [Display(Name = "Tên Người đại diện theo pháp luật")]
        public string? TenNguoiDaiDienTheoPhapLuat { get; set; }

        [StringLength(100)]
        [Display(Name = "Chức vụ")]
        public string? ChucVu { get; set; }

        [Display(Name = "Ngày sinh đại diện Công ty")]
        [DataType(DataType.Date)]
        public DateTime? NgaySinhDaiDienCongTy { get; set; }

        [StringLength(10)]
        [Display(Name = "Giới tính đại diện Công ty")]
        public string? GioiTinhDaiDienCongTy { get; set; }

        [StringLength(50)]
        [Display(Name = "Quốc tịch đại diện Công ty")]
        public string? QuocTichDaiDienCongTy { get; set; }

        [StringLength(50)]
        [Display(Name = "Số giấy tờ tùy thân đại diện Công ty")]
        public string? SoGiayToTuyThanDaiDienCongTy { get; set; }

        [Display(Name = "Ngày cấp giấy tờ tùy thân đại diện DN")]
        [DataType(DataType.Date)]
        public DateTime? NgayCapGiayToTuyThanDaiDienDN { get; set; }

        [StringLength(200)]
        [Display(Name = "Nơi cấp giấy tờ tùy thân đại diện DN")]
        public string? NoiCapGiayToTuyThanDaiDienDN { get; set; }

        [Display(Name = "Ngày hết hạn giấy tờ tùy thân đại diện DN")]
        [DataType(DataType.Date)]
        public DateTime? NgayHetHanGiayToTuyThanDaiDienDN { get; set; }

        [StringLength(200)]
        [Display(Name = "Địa chỉ đại diện Công ty")]
        public string? DiaChiDaiDienCongTy { get; set; }

        [StringLength(50)]
        [Display(Name = "Số điện thoại đại diện Công ty")]
        public string? SoDienThoaiDaiDienCongTy { get; set; }

        [StringLength(50)]
        [EmailAddress]
        [Display(Name = "Email đại diện Công ty")]
        public string? EmailDaiDienCongTy { get; set; }

        [StringLength(50)]
        [Display(Name = "Văn bản ủy quyền số")]
        public string? VanBanUyQuyenSo { get; set; }

        [StringLength(100)]
        [Display(Name = "Tên Kế toán trưởng")]
        public string? TenKeToanTruong { get; set; }

        [StringLength(100)]
        [Display(Name = "Chức vụ Kế toán trưởng")]
        public string? ChucVuKeToanTruong { get; set; }

        [Display(Name = "Ngày sinh Kế toán trưởng")]
        [DataType(DataType.Date)]
        public DateTime? NgaySinhKeToanTruong { get; set; }

        [StringLength(10)]
        [Display(Name = "Giới tính Kế toán trưởng")]
        public string? GioiTinhKeToanTruong { get; set; }

        [StringLength(50)]
        [Display(Name = "Quốc tịch Kế toán trưởng")]
        public string? QuocTichKeToanTruong { get; set; }

        [StringLength(50)]
        [Display(Name = "Số giấy tờ tùy thân Kế toán trưởng")]
        public string? SoGiayToTuyThanKeToanTruong { get; set; }

        [Display(Name = "Ngày cấp giấy tờ tùy thân Kế toán trưởng")]
        [DataType(DataType.Date)]
        public DateTime? NgayCapGiayToTuyThanKeToanTruong { get; set; }

        [StringLength(200)]
        [Display(Name = "Nơi cấp giấy tờ tùy thân Kế toán trưởng")]
        public string? NoiCapGiayToTuyThanKeToanTruong { get; set; }

        [Display(Name = "Ngày hết hạn giấy tờ tùy thân Kế toán trưởng")]
        [DataType(DataType.Date)]
        public DateTime? NgayHetHanGiayToTuyThanKeToanTruong { get; set; }

        [StringLength(50)]
        [Display(Name = "Số điện thoại Kế toán trưởng")]
        public string? SoDienThoaiKeToanTruong { get; set; }

        [StringLength(50)]
        [EmailAddress]
        [Display(Name = "Email Kế toán trưởng")]
        public string? EmailKeToanTruong { get; set; }

        [StringLength(200)]
        [Display(Name = "Địa chỉ kế toán trưởng")]
        public string? DiaChiKeToanTruong { get; set; }



        [StringLength(256)]
        [Display(Name = "User thực hiện")]
        public string? UserThucHienId { get; set; }

        [StringLength(20)]
        [Display(Name = "Phòng thực hiện")]
        public string? PhongThucHien { get; set; }


        [Display(Name = "Ngày cập nhật dữ liệu")]
        public DateTime NgayCapNhatDuLieu { get; set; } = DateTime.Now;

        // === KHÓA NGOẠI ĐẾN USER (Dùng Id) ===
        [ForeignKey("UserThucHienId")]
        public virtual ApplicationUser? UserThucHien { get; set; }


        [ForeignKey("PhongThucHien")]
        public virtual PhongBan? PhongBanThucHien { get; set; }
    }
}
