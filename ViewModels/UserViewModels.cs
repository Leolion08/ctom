using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CTOM.ViewModels
{
    // ViewModel cho chức năng Tạo mới Người dùng
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")] // Giữ [Required] attribute
        [StringLength(256, ErrorMessage = "{0} phải dài tối thiểu {2} ký tự.", MinimumLength = 3)]
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; } = string.Empty; // Bỏ 'required', khởi tạo giá trị mặc định

        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [Display(Name = "Email")]
        public string? Email { get; set; } // Bỏ 'required' (đã làm ở bước trước)

        [Required(ErrorMessage = "Họ và Tên người dùng là bắt buộc.")] // Giữ [Required] attribute
        [StringLength(50)]
        [Display(Name = "Họ và Tên")]
        public string TenUser { get; set; } = string.Empty; // Bỏ 'required', khởi tạo giá trị mặc định

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")] // Giữ [Required] attribute
        [StringLength(100, ErrorMessage = "{0} phải dài tối thiểu {2} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty; // Bỏ 'required', khởi tạo giá trị mặc định

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc.")] // Thêm [Required] cho ConfirmPassword
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu và mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty; // Bỏ 'required', khởi tạo giá trị mặc định

        [Required(ErrorMessage = "Phòng ban là bắt buộc.")] // Giữ [Required] attribute
        [Display(Name = "Phòng Ban")]
        public string MaPhong { get; set; } = string.Empty; // Bỏ 'required', khởi tạo giá trị mặc định

        [Required(ErrorMessage = "Trạng thái là bắt buộc.")] // Giữ [Required] attribute
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "A"; // Không có 'required', có giá trị mặc định

        [Display(Name = "Nhóm quyền")]
        public List<string> SelectedRoleNames { get; set; } = new List<string>();

        // --- Thuộc tính để gửi danh sách lựa chọn cho View ---
        public IEnumerable<SelectListItem>? AvailablePhongBan { get; set; } //IEnumerable  -> readonly, phù hợp hiển thị danh sách
        public List<SelectListItem>? AvailableRoles { get; set; } // List -> có thể thêm/sửa/xóa
    }

    // ViewModel cho chức năng Chỉnh sửa Người dùng
    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty; // Khởi tạo giá trị mặc định

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; } = string.Empty; // Không có 'required' vì không sửa

        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [Display(Name = "Email")]
        public string? Email { get; set; } // Bỏ 'required'

        [Required(ErrorMessage = "Họ và Tên người dùng là bắt buộc.")] // Giữ [Required] attribute
        [StringLength(50)]
        [Display(Name = "Họ và Tên")]
        public string TenUser { get; set; } = string.Empty; // Bỏ 'required', khởi tạo giá trị mặc định

        [Required(ErrorMessage = "Phòng ban là bắt buộc.")] // Giữ [Required] attribute
        [Display(Name = "Phòng Ban")]
        public string MaPhong { get; set; } = string.Empty; // Bỏ 'required', khởi tạo giá trị mặc định

        [Required(ErrorMessage = "Trạng thái là bắt buộc.")] // Giữ [Required] attribute
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "A"; // Không có 'required'

        [Display(Name = "Nhóm quyền")]
        public List<string> SelectedRoleNames { get; set; } = new List<string>();

        // --- Thuộc tính để gửi danh sách lựa chọn và trạng thái hiện tại cho View ---
        public IEnumerable<SelectListItem>? AvailablePhongBan { get; set; }
        public List<SelectListItem>? AvailableRoles { get; set; }

        public List<string> CurrentRoleNames { get; set; } = new List<string>();
    }

    // ViewModel cho trang Index Người dùng (không thay đổi)
    public class UserIndexViewModel
    {
        public string Id { get; set; } = string.Empty;
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; } = string.Empty;
        [Display(Name = "Họ và Tên")]
        public string TenUser { get; set; } = string.Empty;
        // public string? Email { get; set; } // Đã bỏ Email
        [Display(Name = "Mã Phòng")]
        public string MaPhong { get; set; } = string.Empty;
        [Display(Name = "Tên Phòng")]
        public string? TenPhong { get; set; }
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = string.Empty;
        [Display(Name = "Nhóm quyền")]
        public List<string> Roles { get; set; } = new List<string>();
        //[Display(Name = "Bị khóa")]
        //public bool IsLockedOut { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty; // Sẽ lấy từ data attribute, đặt vào hidden input

        // Chỉ để hiển thị trên modal, không cần submit lại
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
        [StringLength(100, ErrorMessage = "{0} phải dài từ {2} đến {1} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public required string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu mới và mật khẩu xác nhận không khớp.")]
        public required string ConfirmPassword { get; set; }
    }
}
