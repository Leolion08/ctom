using System.Security.Claims;

namespace CTOM.Services.Interfaces;

/// <summary>
/// Truy cập thông tin người dùng hiện tại (UserName, DepartmentId) theo HttpContext.
/// Giúp Service không cần phụ thuộc Controller truyền tham số.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>
    /// Tên đăng nhập hiện tại (UserName). Empty string nếu chưa xác thực.
    /// </summary>
    string UserName { get; }

    /// <summary>
    /// Mã phòng ban của người dùng, nếu có.
    /// </summary>
    string? MaPhong { get; }

    /// <summary>
    /// Tập hợp claim của người dùng hiện tại (để mở rộng nếu cần).
    /// </summary>
    IEnumerable<Claim>? Claims { get; }

    /// <summary>
    /// Kiểm tra người dùng hiện tại có thuộc role nào đó không.
    /// </summary>
    bool IsInRole(string roleName);
}
