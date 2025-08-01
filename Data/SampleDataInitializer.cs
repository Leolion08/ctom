using CTOM.Models.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CTOM.Data
{
    public class SampleDataInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            // Kiểm tra xem đã có dữ liệu mẫu chưa
            if (context.KhachHangDNs.Any())
            {
                return; // Đã có dữ liệu
            }

            // Tạo user mẫu nếu chưa có
            var user = new ApplicationUser
            {
                UserName = "testuser@example.com",
                Email = "testuser@example.com",
                EmailConfirmed = true,
                MaPhong = "PHONG_KD",
                TenUser = "Test User"
            };

            var result = await userManager.CreateAsync(user, "P@ssw0rd!");
            if (result.Succeeded)
            {
                // Thêm role nếu cần
                await userManager.AddToRoleAsync(user, "HTTD");
            }

            // Tạo dữ liệu khách hàng mẫu
            var khachHangs = new List<KhachHangDN>
            {
                new KhachHangDN
                {
                    SoCif = "CIF001",
                    TenCif = "Công ty TNHH MTV ABC",
                    SoGiayChungNhanDKKD = "0101234567",
                    DiaChiKinhDoanhHienTai = "123 Đường Lê Lợi, Q.1, TP.HCM",
                    SoDienThoaiDN = "02838223344",
                    EmailCongTy = "info@abc.com.vn",
                    UserThucHienId = user.Id
                },
                new KhachHangDN
                {
                    SoCif = "CIF002",
                    TenCif = "Công ty Cổ phần XYZ",
                    SoGiayChungNhanDKKD = "0107654321",
                    DiaChiKinhDoanhHienTai = "456 Đường Nguyễn Huệ, Q.1, TP.HCM",
                    SoDienThoaiDN = "02839998877",
                    EmailCongTy = "contact@xyz.com.vn",
                    UserThucHienId = user.Id
                }
            };

            await context.KhachHangDNs.AddRangeAsync(khachHangs);
            await context.SaveChangesAsync();
        }
    }
}
