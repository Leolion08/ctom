using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CTOM.Data;
using CTOM.Models.Entities;
using CTOM.Models.Responses;
using CTOM.ViewModels.PhongBan;
using Microsoft.Extensions.Logging;

namespace CTOM.Controllers;

/// <summary>
/// Controller quản lý thông tin phòng ban
/// </summary>
[Authorize(Roles = "ADMIN")]
[Route("[controller]")]
public class PhongBanController(
    ApplicationDbContext context,
    ILogger<PhongBanController> logger) : Controller
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<PhongBanController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<IActionResult> Index()
    {

        var phongBans = await _context.PhongBans
            .AsNoTracking()
            .OrderBy(p => p.MaPhong)
            .ToListAsync();
            
        return View(phongBans);
    }
        
    /// <summary>
    /// Hiển thị chi tiết thông tin phòng ban
    /// </summary>
    /// <param name="maPhong">Mã phòng cần xem chi tiết</param>
    [HttpGet("Details/{maPhong}")]
    public async Task<IActionResult> Details(string maPhong)
    {
        if (string.IsNullOrWhiteSpace(maPhong))
            return BadRequest("Mã phòng không hợp lệ.");
            
        var phongBan = await _context.PhongBans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MaPhong == maPhong);
            
        if (phongBan is null)
            return NotFound($"""Không tìm thấy phòng ban có mã '{maPhong}'""");
            
        var viewModel = new EditPhongBanViewModel
        {
            MaPhong = phongBan.MaPhong,
            MaPhongHR = phongBan.MaPhongHR,
            TenPhong = phongBan.TenPhong,
            TenVietTat = phongBan.TenVietTat,
            TrangThai = phongBan.TrangThai
        };
            
        return View(viewModel);
    }
        
    /// <summary>
    /// Hiển thị form tạo mới phòng ban
    /// </summary>
    [HttpGet("Create")]
    public IActionResult Create() => View(new CreatePhongBanViewModel 
    { 
        MaPhong = string.Empty,
        TenPhong = string.Empty,
        TrangThai = "A"
    });
    
    /// <summary>
    /// Xử lý tạo mới phòng ban
    /// </summary>
    [HttpPost("Create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePhongBanViewModel model)
    {
        if (model is null)
            return BadRequest("Dữ liệu không hợp lệ.");
            
        if (!ModelState.IsValid)
            return View(model);

        // Kiểm tra trùng mã phòng
        if (await _context.PhongBans.AnyAsync(p => p.MaPhong == model.MaPhong.Trim()))
        {
            ModelState.AddModelError(nameof(model.MaPhong), "Mã phòng đã tồn tại.");
            return View(model);
        }

        try
        {
            var phongBan = new PhongBan
            {
                MaPhong = model.MaPhong.Trim(),
                MaPhongHR = model.MaPhongHR?.Trim(),
                TenPhong = model.TenPhong.Trim(),
                TenVietTat = model.TenVietTat?.Trim(),
                TrangThai = model.TrangThai
            };

            _context.Add(phongBan);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thêm mới phòng ban thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thêm mới phòng ban");
            ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu dữ liệu. Vui lòng thử lại.");
            return View(model);
        }
    }

    /// <summary>
    /// Hiển thị form chỉnh sửa phòng ban
    /// </summary>
    [HttpGet("Edit/{maPhong}")]
    public async Task<IActionResult> Edit(string maPhong)
    {
        if (string.IsNullOrWhiteSpace(maPhong))
            return BadRequest("Mã phòng không hợp lệ.");
            
        var phongBan = await _context.PhongBans
            .FirstOrDefaultAsync(p => p.MaPhong == maPhong);
            
        if (phongBan is null)
            return NotFound($"""Không tìm thấy phòng ban có mã '{maPhong}'""");
            
        var model = new EditPhongBanViewModel
        {
            MaPhong = phongBan.MaPhong,
            MaPhongHR = phongBan.MaPhongHR,
            TenPhong = phongBan.TenPhong,
            TenVietTat = phongBan.TenVietTat,
            TrangThai = phongBan.TrangThai
        };
        
        return View(model);
    }
        
    /// <summary>
    /// Xử lý cập nhật thông tin phòng ban
    /// </summary>
    [HttpPost("Edit/{maPhong}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string maPhong, EditPhongBanViewModel model)
    {
        if (string.IsNullOrWhiteSpace(maPhong) || model is null)
            return BadRequest("Dữ liệu không hợp lệ.");
            
        if (!ModelState.IsValid)
            return View(model);
            
        var phongBan = await _context.PhongBans
            .FirstOrDefaultAsync(p => p.MaPhong == maPhong);
            
        if (phongBan is null)
            return NotFound($"""Không tìm thấy phòng ban có mã '{maPhong}'""");
            
        try
        {
            // Cập nhật thông tin
            phongBan.MaPhongHR = model.MaPhongHR?.Trim();
            phongBan.TenPhong = model.TenPhong.Trim();
            phongBan.TenVietTat = model.TenVietTat?.Trim();
            phongBan.TrangThai = model.TrangThai;
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Cập nhật thông tin phòng ban thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật phòng ban {MaPhong}", maPhong);
            ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật dữ liệu. Vui lòng thử lại.");
            return View(model);
        }
    }
        
    /// <summary>
    /// Xử lý xóa phòng ban
    /// </summary>
    [HttpPost("Delete/{maPhong}"), ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string maPhong)
    {
        if (string.IsNullOrWhiteSpace(maPhong))
            return Json(ApiResponse.Fail("Mã phòng không được để trống"));
            
        try
        {
            var phongBan = await _context.PhongBans
                .FirstOrDefaultAsync(p => p.MaPhong == maPhong);
                
            if (phongBan is null)
                return Json(ApiResponse.Fail($"Không tìm thấy phòng ban có mã '{maPhong}'"));
            
            // Kiểm tra ràng buộc khóa ngoại trước khi xóa
            var userCount = await _context.Users.CountAsync(u => u.MaPhong == maPhong);
            if (userCount > 0)
            {
                return Json(ApiResponse.Fail(
                    $"Không thể xóa phòng ban vì có {userCount} người dùng đang thuộc phòng ban này. Vui lòng chuyển hoặc xóa người dùng trước khi xóa phòng ban."));
            }
            
            _context.PhongBans.Remove(phongBan);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Đã xóa phòng ban: {MaPhong} - {TenPhong}", phongBan.MaPhong, phongBan.TenPhong);
            return Json(ApiResponse.Ok($"Đã xóa thành công phòng ban '{phongBan.TenPhong}'", new { id = phongBan.MaPhong }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa phòng ban {MaPhong}", maPhong);
            return Json(ApiResponse.Fail($"Đã xảy ra lỗi khi xóa phòng ban: {ex.Message}"));
        }
    }
}
