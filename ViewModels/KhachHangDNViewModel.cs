using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;
//using CTOM.Validations;

namespace CTOM.ViewModels
{
    // Tạm thời bỏ valide ngày
    // // ViewModel cho việc Create/Edit KhachHangDN thủ công
    // [DateGreaterThan("NgayCapGiayToTuyThanDaiDienDN", "NgayHetHanGiayToTuyThanDaiDienDN",
    //     ErrorMessage = "Ngày hết hạn phải lớn hơn ngày cấp")]
    // [DateGreaterThan("NgayCapGiayToTuyThanKeToanTruong", "NgayHetHanGiayToTuyThanKeToanTruong",
    //     ErrorMessage = "Ngày hết hạn phải lớn hơn ngày cấp")]
    public class KhachHangDNViewModel
    {
        /* Thông tin cơ bản */
        [Required(ErrorMessage = "Số CIF là bắt buộc.")]
        [StringLength(20)]
        [Display(Name = "Số CIF")]
        public string SoCif { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên CIF là bắt buộc.")]
        [StringLength(150)]
        [Display(Name = "Tên CIF")]
        public string? TenCif { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Xếp hạng TD nội bộ")]
        public string? XepHangTinDungNoiBo { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Loại hình DN")]
        public string? LoaiHinhDN { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Số ĐKKD")]
        public string? SoGiayChungNhanDKKD { get; set; }

        [StringLength(200)]
        [Display(Name = "Nơi cấp ĐKKD")]
        public string? NoiCapGiayChungNhanDKKD { get; set; }

        [Display(Name = "Ngày cấp ĐKKD")]
        [DataType(DataType.Date)]
        public DateTime? NgayCapGiayChungNhanDKKD { get; set; }

        [StringLength(150)]
        [Display(Name = "Tên tiếng Anh")]
        public string? TenTiengAnh { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Địa chỉ trên ĐKKD")]
        public string? DiaChiTrenDKKD { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Địa chỉ KD hiện tại")]
        public string? DiaChiKinhDoanhHienTai { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Lĩnh vực KD chính")]
        public string? LinhVucKinhDoanhChinh { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá {1} ký tự")]
        [Display(Name = "Số điện thoại Doanh nghiệp")]
        //[VietnamesePhoneNumber]
        public string? SoDienThoaiDN { get; set; }

        [StringLength(20, ErrorMessage = "Số Fax không được vượt quá {1} ký tự")]
        [Display(Name = "Số Fax")]
        public string? SoFaxCongTy { get; set; }

        [StringLength(50)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email Công ty")]
        public string? EmailCongTy { get; set; }

        /* Người đại diện */
        [StringLength(255)]
        [Display(Name = "Người đại diện")]
        public string? TenNguoiDaiDienTheoPhapLuat { get; set; }

        [StringLength(100)]
        [Display(Name = "Chức vụ")]
        public string? ChucVu { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        //[NotFutureDate(ErrorMessage = "Ngày sinh không được lớn hơn ngày hiện tại")]
        public DateTime? NgaySinhDaiDienCongTy { get; set; }

        [StringLength(10)]
        [Display(Name = "Giới tính")]
        public string? GioiTinhDaiDienCongTy { get; set; } = string.Empty; // Có thể dùng dropdown nếu giá trị cố định

        [StringLength(50)]
        [Display(Name = "Quốc tịch")]
        public string? QuocTichDaiDienCongTy { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá {1} ký tự")]
        [Display(Name = "Số điện thoại")]
        //[VietnamesePhoneNumber]
        public string? SoDienThoaiDaiDienCongTy { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Số giấy tờ tùy thân")]
        public string? SoGiayToTuyThanDaiDienCongTy { get; set; } = string.Empty;

        [Display(Name = "Ngày cấp")]
        [DataType(DataType.Date)]
        public DateTime? NgayCapGiayToTuyThanDaiDienDN { get; set; }

        [StringLength(200)]
        [Display(Name = "Nơi cấp")]
        public string? NoiCapGiayToTuyThanDaiDienDN { get; set; }

        [Display(Name = "Ngày hết hạn")]
        [DataType(DataType.Date)]
        public DateTime? NgayHetHanGiayToTuyThanDaiDienDN { get; set; }

        [StringLength(200)]
        [Display(Name = "Địa chỉ")]
        public string? DiaChiDaiDienCongTy { get; set; } = string.Empty;

        [StringLength(50)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string? EmailDaiDienCongTy { get; set; }

        [StringLength(50)]
        [Display(Name = "Văn bản ủy quyền số")]
        public string? VanBanUyQuyenSo { get; set; } = string.Empty;

        /* Kế toán trưởng */
        [StringLength(50)]
        [Display(Name = "Kế toán trưởng")]
        public string? TenKeToanTruong { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Chức vụ")]
        public string? ChucVuKeToanTruong { get; set; } = string.Empty;

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        //[NotFutureDate(ErrorMessage = "Ngày sinh không được lớn hơn ngày hiện tại")]
        public DateTime? NgaySinhKeToanTruong { get; set; }

        [StringLength(10)]
        [Display(Name = "Giới tính")]
        public string? GioiTinhKeToanTruong { get; set; } = string.Empty; // Có thể dùng dropdown

        [StringLength(50)]
        [Display(Name = "Quốc tịch")]
        public string? QuocTichKeToanTruong { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Số giấy tờ tùy thân")]
        public string? SoGiayToTuyThanKeToanTruong { get; set; } = string.Empty;

        [Display(Name = "Ngày cấp")]
        [DataType(DataType.Date)]
        public DateTime? NgayCapGiayToTuyThanKeToanTruong { get; set; }

        [StringLength(200)]
        [Display(Name = "Nơi cấp")]
        public string? NoiCapGiayToTuyThanKeToanTruong { get; set; }

        [Display(Name = "Ngày hết hạn")]
        [DataType(DataType.Date)]
        public DateTime? NgayHetHanGiayToTuyThanKeToanTruong { get; set; }

        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá {1} ký tự")]
        [Display(Name = "Số điện thoại")]
        //[VietnamesePhoneNumber]
        public string? SoDienThoaiKeToanTruong { get; set; }

        [StringLength(50)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string? EmailKeToanTruong { get; set; }

        [StringLength(255)]
        [Display(Name = "Địa chỉ")]
        public string? DiaChiKeToanTruong { get; set; } = string.Empty;

        /* Trường ẩn */
        [StringLength(256)]
        [Display(Name = "Người thực hiện")]
        public string? UserThucHienId { get; set; } = string.Empty; //required, xử lý ở controller

        [StringLength(20)]
        [Display(Name = "Phòng thực hiện")]
        public string? PhongThucHien { get; set; } = string.Empty;

        [Display(Name = "Ngày cập nhật dữ liệu")]
        [DataType(DataType.DateTime)]
        public DateTime NgayCapNhatDuLieu { get; set; } = DateTime.Now;

        // Các trường audit như UserThucHienId, PhongThucHien, NgayCapNhatDuLieu
        // thường sẽ được xử lý ở Controller, không cần đưa vào ViewModel cho người dùng nhập
    }
}
