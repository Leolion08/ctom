using Microsoft.AspNetCore.Identity;
using CTOM.Data;
using CTOM.Models.Entities;

namespace CTOM.Models;

/// <summary>
/// Chứa thông tin ngữ cảnh khi thực hiện import dữ liệu
/// </summary>
public class ImportContext
{
    /// <summary>
    /// Tên người dùng thực hiện import
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Mã phòng của người dùng
    /// </summary>
    public string? MaPhong { get; set; }

    /// <summary>
    /// Thời điểm thực hiện import
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Tạo ImportContext từ HttpContext
    /// </summary>
    public static async Task<ImportContext> FromHttpContextAsync(
        HttpContext httpContext, 
        UserManager<ApplicationUser> userManager)
    {
        var timestamp = DateTime.Now;
        
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return new ImportContext
            {
                UserName = "System",
                MaPhong = null,
                Timestamp = timestamp
            };
        }

        try
        {
            // Lấy thông tin người dùng từ UserManager
            var currentUser = await userManager.GetUserAsync(httpContext.User);
            if (currentUser == null)
            {
                throw new InvalidOperationException("Không tìm thấy thông tin người dùng");
            }

            var userName = currentUser.UserName ?? "System";
            var maPhong = currentUser.MaPhong;
            
            // Ghi log cảnh báo nếu không tìm thấy MaPhong
            if (string.IsNullOrEmpty(maPhong))
            {
                _ = Task.Run(() => 
                    System.Diagnostics.Debug.WriteLine($"[Warning] Không tìm thấy MaPhong cho user: {userName}"));
            }

            return new ImportContext
            {
                UserName = userName,
                MaPhong = maPhong,
                Timestamp = timestamp
            };
        }
        catch (Exception ex)
        {
            _ = Task.Run(() => 
                System.Diagnostics.Debug.WriteLine($"[Error] Lỗi khi lấy thông tin người dùng: {ex.ToString()}"));
            
            return new ImportContext
            {
                UserName = "System",
                MaPhong = null,
                Timestamp = timestamp
            };
        }
    }
}
