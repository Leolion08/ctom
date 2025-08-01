using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CTOM.Models.Entities;
using CTOM.ViewModels;
using CTOM.Models.Responses;

namespace CTOM.Controllers;

/// <summary>
/// Controller quản lý các nhóm quyền (roles) trong hệ thống
/// </summary>
[Authorize(Roles = "ADMIN")]
[Route("[controller]")]
public class NhomQuyenController(
    RoleManager<ApplicationRole> roleManager, 
    UserManager<ApplicationUser> userManager, 
    ILogger<NhomQuyenController> logger) : Controller
{
    private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ILogger<NhomQuyenController> _logger = logger;

    /// <summary>
    /// Hiển thị danh sách tất cả các nhóm quyền
    /// </summary>
    [HttpGet, Route(""), Route("Index")]
    public async Task<IActionResult> Index()
    {

        var roles = await _roleManager.Roles
            .OrderBy(r => r.Name)
            .AsNoTracking()
            .ToListAsync();
            
        return View(roles);
    }

    /// <summary>
    /// Hiển thị chi tiết thông tin nhóm quyền
    /// </summary>
    /// <param name="roleName">Tên nhóm quyền cần xem chi tiết</param>
    [HttpGet("Details/{roleName}")]
    public async Task<IActionResult> Details(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest("Tên nhóm quyền không hợp lệ.");
            
        var role = await _roleManager.FindByNameAsync(roleName);
        
        return role is null 
            ? NotFound($"""Không tìm thấy nhóm quyền '{roleName}'.""") 
            : View(role);
    }


    /// <summary>
    /// Hiển thị form tạo mới nhóm quyền
    /// </summary>
    [HttpGet("Create")]
    public IActionResult Create() => View(new CreateRoleViewModel 
    { 
        RoleName = string.Empty,
        TrangThai = "A" 
    });

    /// <summary>
    /// Xử lý tạo mới nhóm quyền
    /// </summary>
    [HttpPost("Create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRoleViewModel model)
    {
        if (model is null)
            return BadRequest("Dữ liệu không hợp lệ.");
            
        if (!ModelState.IsValid)
            return View(model);

        if (string.IsNullOrWhiteSpace(model.RoleName))
        {
            ModelState.AddModelError(nameof(model.RoleName), "Tên nhóm quyền không được để trống.");
            return View(model);
        }

        if (await _roleManager.RoleExistsAsync(model.RoleName))
        {
            ModelState.AddModelError(nameof(model.RoleName), "Mã nhóm này đã tồn tại.");
            return View(model);
        }

        var newRole = new ApplicationRole
        {
            Name = model.RoleName.Trim(),
            NormalizedName = model.RoleName.Trim().ToUpperInvariant(),
            TenNhomDayDu = model.TenNhomDayDu?.Trim() ?? string.Empty,
            TrangThai = string.IsNullOrWhiteSpace(model.TrangThai) ? "A" : model.TrangThai.Trim()
        };

        var result = await _roleManager.CreateAsync(newRole);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        _logger.LogInformation("Đã tạo nhóm quyền mới: {RoleName}", model.RoleName);
        TempData["SuccessMessage"] = $"""Đã tạo thành công nhóm quyền '{model.RoleName}'.""";
        
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Hiển thị form chỉnh sửa nhóm quyền
    /// </summary>
    /// <param name="roleName">Tên nhóm quyền cần chỉnh sửa</param>
    [HttpGet("Edit/{roleName}")]
    public async Task<IActionResult> Edit(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest("Tên nhóm quyền không hợp lệ.");

        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null)
            return NotFound($"""Không tìm thấy nhóm quyền '{roleName}'.""");

        var model = new EditRoleViewModel
        {
            RoleName = role.Name ?? string.Empty,
            TenNhomDayDu = role.TenNhomDayDu ?? string.Empty,
            TrangThai = role.TrangThai ?? "A" // Giá trị mặc định nếu null
        };
        
        return View(model);
    }

    /// <summary>
    /// Xử lý cập nhật thông tin nhóm quyền
    /// </summary>
    /// <param name="roleName">Tên nhóm quyền cần cập nhật</param>
    /// <param name="model">Dữ liệu cập nhật</param>
    [HttpPost("Edit/{roleName}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string roleName, EditRoleViewModel model)
    {
        if (model is null)
            return BadRequest("Dữ liệu không hợp lệ.");
            
        if (string.IsNullOrWhiteSpace(roleName) || string.IsNullOrWhiteSpace(model.RoleName) || 
            !roleName.Equals(model.RoleName, StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Thông tin nhóm quyền không khớp.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
            return View(model);

        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null)
            return NotFound($"""Không tìm thấy nhóm quyền '{roleName}'.""");

        // Cập nhật thông tin
        role.TenNhomDayDu = model.TenNhomDayDu?.Trim() ?? string.Empty;
        role.TrangThai = string.IsNullOrWhiteSpace(model.TrangThai) ? "A" : model.TrangThai.Trim();

        var result = await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        _logger.LogInformation("Đã cập nhật nhóm quyền: {RoleName}", role.Name);
        TempData["SuccessMessage"] = $"""Đã cập nhật thành công nhóm quyền '{role.Name}'.""";
        
        return RedirectToAction(nameof(Index));
    }


    /// <summary>
    /// Xóa một nhóm quyền
    /// </summary>
    /// <param name="roleName">Tên nhóm quyền cần xóa</param>
    [HttpPost("Delete/{roleName}"), ValidateAntiForgeryToken]
    [Route("Delete/{roleName}")]
    public async Task<IActionResult> Delete(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return Json(ApiResponse.Fail("Tên nhóm quyền không được để trống"));
        }

        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            return Json(ApiResponse.Fail("Không tìm thấy nhóm quyền cần xóa"));
        }
        
        // Không cho xóa role ADMIN
        if (role.Name?.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) == true)
        {
            return Json(ApiResponse.Fail("Không thể xóa nhóm quyền ADMIN hệ thống."));
        }

        try
        {
            // Kiểm tra xem có người dùng nào trong nhóm không
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            if (usersInRole.Any())
            {
                return Json(ApiResponse.Fail(
                    $"Không thể xóa nhóm quyền vì có {usersInRole.Count} người dùng đang sử dụng. Vui lòng xóa hoặc chuyển người dùng ra khỏi nhóm trước khi xóa."));
            }

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                _logger.LogInformation("Đã xóa nhóm quyền: {RoleName}", role.Name);
                return Json(ApiResponse.Ok($"Đã xóa thành công nhóm quyền '{role.Name}'"));
            }

            var errors = result.Errors.ToDictionary(
                e => e.Code,
                e => new[] { e.Description }
            );
            
            _logger.LogError("Lỗi khi xóa nhóm quyền {RoleName}: {Errors}", 
                roleName, string.Join(", ", errors.Values.SelectMany(v => v)));
                
            return Json(ApiResponse.Fail("Đã xảy ra lỗi khi xóa nhóm quyền", errors));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa nhóm quyền {RoleName}", roleName);
            return Json(ApiResponse.Fail($"Đã xảy ra lỗi khi xóa nhóm quyền: {ex.Message}"));
        }
    }

    private IActionResult HandleAjaxSuccess(string message, object? data = null)
    {
        if (Request.Headers.TryGetValue("X-Requested-With", out var headerValue) && 
            headerValue == "XMLHttpRequest")
        {
            return data != null 
                ? Json(ApiResponse.Ok(message, data))
                : Json(ApiResponse.Ok(message));
        }
        
        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    private IActionResult HandleAjaxError(string message, IDictionary<string, string[]>? errors = null)
    {
        if (Request.Headers.TryGetValue("X-Requested-With", out var headerValue) && 
            headerValue == "XMLHttpRequest")
        {
            return Json(ApiResponse.Fail(message, errors));
        }
        
        TempData["ErrorMessage"] = message;
        return RedirectToAction(nameof(Index));
    }
}
