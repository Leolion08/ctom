using CTOM.Data;
using CTOM.Models.Entities;
using CTOM.Models.Responses;
using CTOM.ViewModels;
using CTOM.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace CTOM.Controllers
{
    [Authorize(Roles = "ADMIN")]
    [Route("[controller]")]
    public class NguoiSuDungController(UserManager<ApplicationUser> userManager, 
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext context,
        ILogger<NguoiSuDungController> logger) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<NguoiSuDungController> _logger = logger;

        // GET: /NguoiSuDung
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {

            var users = await _userManager.Users
                                .Include(u => u.PhongBan)
                                .OrderBy(u => u.UserName)
                                .ToListAsync();

            var userViewModels = new List<UserIndexViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserIndexViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName?.Trim() ?? string.Empty,
                    TenUser = user.TenUser,
                    // Email = user.Email, // Đã bỏ Email
                    MaPhong = user.MaPhong,
                    TenPhong = user.PhongBan?.TenPhong ?? string.Empty,
                    TrangThai = user.TrangThai,
                    Roles = roles?.ToList() ?? []
                    //IsLockedOut = await _userManager.IsLockedOutAsync(user)
                });
            }

            return View(userViewModels.ToList()); // Truyền List<UserIndexViewModel>
        }


        // GET: NguoiSuDung/Details/{id}
        /// <summary>
        /// Hiển thị chi tiết thông tin người dùng
        /// </summary>
        /// <param name="id">ID của người dùng cần xem</param>
        /// <summary>
        /// Xem chi tiết thông tin người dùng
        /// </summary>
        /// <param name="id">ID của người dùng cần xem</param>
        /// <returns>Trang hiển thị chi tiết thông tin người dùng</returns>
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Yêu cầu xem chi tiết với ID rỗng");
                TempData["ErrorMessage"] = "ID người dùng không hợp lệ";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var user = await _userManager.Users
                    .AsNoTracking()
                    .Include(u => u.PhongBan)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user is null)
                {
                    _logger.LogWarning("Không tìm thấy người dùng với ID: {UserId}", id);
                    TempData["ErrorMessage"] = $"Không tìm thấy người dùng với ID: {id}";
                    return RedirectToAction(nameof(Index));
                }

                var roles = await _userManager.GetRolesAsync(user);
                var model = new UserIndexViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName?.Trim() ?? string.Empty,
                    TenUser = user.TenUser ?? string.Empty,
                    MaPhong = user.MaPhong ?? string.Empty,
                    TenPhong = user.PhongBan?.TenPhong ?? string.Empty,
                    TrangThai = user.TrangThai ?? "A", // C# 8.0+: Null-coalescing operator (trước đây: user.TrangThai != null ? user.TrangThai : "A")
                    Roles = [..roles] // C# 12: Collection expression (trước đây: roles?.ToList() ?? new List<string>())
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin chi tiết người dùng {UserId}", id);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xử lý yêu cầu";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: NguoiSuDung/Create
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            var model = new CreateUserViewModel(); // Sử dụng CreateUserViewModel từ namespace ViewModels
            await PopulateSelectionLists(model);
            return View(model);
        }

        /// <summary>
        /// Xử lý tạo người dùng mới
        /// </summary>
        /// <param name="model">Dữ liệu người dùng mới</param>
        /// <returns>Redirect về trang danh sách nếu thành công, ngược lại hiển thị form với thông báo lỗi</returns>
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            try
            {
                // Không cần validate Email
                ModelState.Remove(nameof(model.Email));
                
                if (!ModelState.IsValid)
                {
                    await PopulateSelectionLists(model);
                    return View(model);
                }

                // Kiểm tra tên đăng nhập đã tồn tại chưa
                var existingUser = await _userManager.FindByNameAsync(model.UserName);
                if (existingUser != null)
                {
                    ModelState.AddModelError(nameof(model.UserName), "Tên đăng nhập đã tồn tại.");
                    await PopulateSelectionLists(model);
                    return View(model);
                }

                // Tạo người dùng mới
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    TenUser = model.TenUser ?? string.Empty,
                    MaPhong = model.MaPhong ?? string.Empty,
                    TrangThai = model.TrangThai ?? "A",
                    Email = null,
                    EmailConfirmed = false,
                    PhoneNumberConfirmed = false,
                    TwoFactorEnabled = false,
                    LockoutEnabled = true
                };

                var createResult = await _userManager.CreateAsync(user, model.Password);
                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    await PopulateSelectionLists(model);
                    return View(model);
                }

                // Gán vai trò nếu có
                if (model.SelectedRoleNames?.Count > 0)
                {
                    var addToRolesResult = await _userManager.AddToRolesAsync(user, model.SelectedRoleNames);
                    if (!addToRolesResult.Succeeded)
                    {
                        _logger.LogWarning("Lỗi khi gán vai trò cho người dùng {UserName}. Lỗi: {Errors}", 
                            user.UserName, string.Join(", ", addToRolesResult.Errors.Select(e => e.Description)));
                        
                        // Vẫn coi là thành công nhưng ghi log lỗi gán vai trò
                        _logger.LogInformation("Đã tạo người dùng {UserName} nhưng có lỗi khi gán vai trò", user.UserName);
                        TempData["WarningMessage"] = $"Đã tạo người dùng '{user.UserName}' nhưng có lỗi khi gán nhóm quyền.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                _logger.LogInformation("Đã tạo thành công người dùng {UserName}", user.UserName);
                TempData["SuccessMessage"] = $"Đã tạo thành công người dùng '{user.UserName}'.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo người dùng mới");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tạo người dùng. Vui lòng thử lại sau.");
                await PopulateSelectionLists(model);
                return View(model);
            }
        }


        // GET: NguoiSuDung/Edit/{id}
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) 
            {
                _logger.LogWarning("Yêu cầu chỉnh sửa với ID rỗng");
                return BadRequest("ID không hợp lệ");
            }
            
            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                _logger.LogWarning("Không tìm thấy user với ID: {UserId}", id);
                return NotFound();
            }
            
            var currentRoles = await _userManager.GetRolesAsync(user);
            var model = new EditUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty, // C# 8.0+: Null-coalescing operator
                TenUser = user.TenUser ?? string.Empty, // C# 8.0+: Null-coalescing operator
                MaPhong = user.MaPhong ?? string.Empty, // C# 8.0+: Null-coalescing operator
                TrangThai = user.TrangThai ?? "A", // C# 8.0+: Null-coalescing operator với giá trị mặc định
                CurrentRoleNames = [.. currentRoles], // C# 12: Collection expression (trước đây: currentRoles.ToList())
                SelectedRoleNames = [.. currentRoles] // C# 12: Collection expression (trước đây: currentRoles.ToList())
            };
            
            await PopulateSelectionLists(model);
            return View(model);
        }

        /// <summary>
        /// Xử lý cập nhật thông tin người dùng
        /// </summary>
        /// <param name="id">ID của người dùng cần cập nhật</param>
        /// <param name="model">Dữ liệu cập nhật</param>
        /// <returns>Redirect về trang danh sách nếu thành công, ngược lại hiển thị form với thông báo lỗi</returns>
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel model)
        {
            if (string.IsNullOrEmpty(id) || id != model.Id)
            {
                _logger.LogWarning("ID không hợp lệ hoặc không khớp: {Id} != {ModelId}", id, model.Id);
                TempData["ErrorMessage"] = "ID không hợp lệ";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Bỏ qua validation cho các trường không cần validate
                ModelState.Remove(nameof(model.UserName));
                ModelState.Remove(nameof(model.Email));

                if (!ModelState.IsValid)
                {
                    await PopulateSelectionLists(model);
                    return View(model);
                }


                var user = await _userManager.FindByIdAsync(id);
                if (user is null)
                {
                    _logger.LogWarning("Không tìm thấy người dùng với ID: {UserId}", id);
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng";
                    return RedirectToAction(nameof(Index));
                }

                // Cập nhật thông tin cơ bản
                user.TenUser = model.TenUser ?? string.Empty;
                user.MaPhong = model.MaPhong ?? string.Empty;
                user.TrangThai = model.TrangThai ?? "A";

                // Xử lý email nếu cần
                if (string.IsNullOrEmpty(user.Email))
                {
                    user.Email = null;
                    user.NormalizedEmail = null;
                    user.EmailConfirmed = false;
                }

                else
                {
                    // Xóa email nếu trống
                    user.Email = null;
                    user.NormalizedEmail = null;
                    user.EmailConfirmed = false;
                }


                // Cập nhật thông tin user
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                        _logger.LogWarning("Lỗi khi cập nhật người dùng {UserName}: {Error}", user.UserName, error.Description);
                    }
                    await PopulateSelectionLists(model);
                    return View(model);
                }

                // Xử lý cập nhật vai trò
                try
                {
                    await UpdateUserRolesAsync(user, model.SelectedRoleNames);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi cập nhật vai trò cho người dùng {UserId}", user.Id);
                    // Vẫn tiếp tục xử lý dù có lỗi cập nhật vai trò
                }

                _logger.LogInformation("Đã cập nhật thành công thông tin người dùng {UserName}", user.UserName);
                TempData["SuccessMessage"] = $"Đã cập nhật thành công thông tin người dùng '{user.UserName}'";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật người dùng {UserId}", id);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi cập nhật thông tin. Vui lòng thử lại sau.");
                await PopulateSelectionLists(model);
                return View(model);
            }
        }

        private async Task UpdateUserRolesAsync(ApplicationUser user, IList<string>? selectedRoleNames)
        {
            ArgumentNullException.ThrowIfNull(user); //Từ .NET 6 trở lên

            selectedRoleNames ??= []; // C# 12: Collection initialization (trước đây: selectedRoleNames ??= new List<string>())
            var currentRoles = await _userManager.GetRolesAsync(user);
            
            var rolesToRemove = currentRoles.Except(selectedRoleNames).ToList();
            var rolesToAdd = selectedRoleNames.Except(currentRoles).ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    _logger.LogWarning("Lỗi khi xóa roles cũ của user {UserName}", user.UserName);
                }
            }

            if (rolesToAdd.Count > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    _logger.LogWarning("Lỗi khi thêm roles mới cho user {UserName}", user.UserName);
                }
            }
        }

        /// <summary>
        /// Kiểm tra tên đăng nhập đã tồn tại chưa
        /// </summary>
        /// <param name="username">Tên đăng nhập cần kiểm tra</param>
        /// <param name="id">ID của người dùng hiện tại (nếu có) để loại trừ khi kiểm tra</param>
        /// <returns>Kết quả kiểm tra dạng JSON</returns>
        [HttpGet, HttpPost] // Hỗ trợ cả GET và POST
        [Route("CheckUsernameExists")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)] // Không cache response
        public async Task<IActionResult> CheckUsernameExists(string username, string? id = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return Json(ApiResponse.Fail("Tên đăng nhập không được để trống"));
                }

                var user = await _userManager.FindByNameAsync(username);
                
                // Nếu không tìm thấy user
                if (user is null)
                {
                    return Json(ApiResponse.NotExisted("Tên đăng nhập có thể sử dụng"));
                }
                
                // Nếu đây là user đang chỉnh sửa và username không thay đổi
                if (!string.IsNullOrEmpty(id) && user.Id == id)
                {
                    return Json(ApiResponse.NotExisted("Tên đăng nhập hợp lệ"));
                }

                // Nếu tìm thấy user và không phải đang chỉnh sửa
                return Json(ApiResponse.Existed("Tên đăng nhập này đã được sử dụng"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra tên đăng nhập: {Username}", username);
                return Json(ApiResponse.Fail("Đã xảy ra lỗi khi kiểm tra tên đăng nhập"));
            }
        }

        /// <summary>
        /// Đặt lại mật khẩu cho người dùng
        /// </summary>
        /// <param name="id">ID của người dùng cần đặt lại mật khẩu (truyền qua form data)</param>
        /// <param name="newPassword">Mật khẩu mới (truyền qua form data)</param>
        /// <returns>Kết quả thực hiện dạng JSON</returns>
        [HttpPost("ResetPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            [FromForm] string id, 
            [FromForm] string newPassword)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(newPassword))
            {
                _logger.LogWarning("Thông tin đặt lại mật khẩu không hợp lệ. ID: {UserId}, HasPassword: {HasPassword}", 
                    id, !string.IsNullOrEmpty(newPassword));
                return Json(ApiResponse.Fail("Thông tin không hợp lệ"));
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("Không tìm thấy người dùng với ID: {UserId}", id);
                    return Json(ApiResponse.Fail("Không tìm thấy người dùng"));
                }

                // Tạo token đặt lại mật khẩu
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Đã đặt lại mật khẩu thành công cho người dùng {UserId}", id);
                    return Json(ApiResponse.Ok("Đã đặt lại mật khẩu thành công"));
                }

                // Xử lý lỗi đặt lại mật khẩu
                var errors = result.Errors.ToDictionary(
                    e => e.Code.ToCamelCase(), // Chuyển đổi sang camelCase
                    e => new[] { e.Description }
                );
                
                _logger.LogError("Lỗi khi đặt lại mật khẩu cho người dùng {UserId}: {Errors}", 
                    id, string.Join(", ", result.Errors.Select(e => e.Description)));
                
                return Json(ApiResponse.Fail(
                    message: "Không thể đặt lại mật khẩu",
                    errors: errors
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đặt lại mật khẩu cho người dùng {UserId}", id);
                return Json(ApiResponse.Fail("Đã xảy ra lỗi không xác định khi đặt lại mật khẩu"));
            }
        }

        private async Task PopulateSelectionLists(CreateUserViewModel model)
        {
            model.AvailablePhongBan = await _context.PhongBans
                                            .Where(p => p.TrangThai == "A")
                                            .OrderBy(p => p.TenPhong)
                                            .Select(p => new SelectListItem 
                                            { 
                                                // C# 8.0+: Null-coalescing operator
                                                Value = p.MaPhong ?? string.Empty, 
                                                // C# 6.0+: String interpolation (trước đây: p.TenPhong + " (" + p.MaPhong + ")")
                                                Text = $"{p.TenPhong} ({p.MaPhong})" 
                                            })
                                            .ToListAsync();
            model.AvailableRoles = await _roleManager.Roles
                                            .Where(r => r.TrangThai == "A")
                                            .OrderBy(r => r.Name)
                                            .Select(r => new SelectListItem 
                                            { 
                                                Value = r.Name, 
                                                Text = r.Name 
                                            })
                                            .ToListAsync();
        }

        //Không cho chọn phòng bị Disabled (không bao gồm phòng hiện tại)
        private async Task PopulateSelectionLists(EditUserViewModel model)
        {
            // Lấy danh sách Phòng Ban Active
            var activePhongBans = await _context.PhongBans
                                            .Where(p => p.TrangThai == "A")
                                            .OrderBy(p => p.TenPhong)
                                            .Select(p => new SelectListItem 
                                            { 
                                                Value = p.MaPhong, 
                                                Text = $"{p.TenPhong} ({p.MaPhong})" 
                                            })
                                            .ToListAsync();

            // Kiểm tra xem phòng ban hiện tại của user có trong danh sách active không
            bool currentPhongBanIsActive = activePhongBans.Exists(p => p.Value == model.MaPhong);

            // Nếu phòng ban hiện tại không active (bị Disabled)
            if (!currentPhongBanIsActive && !string.IsNullOrEmpty(model.MaPhong))
            {
                // Lấy thông tin phòng ban hiện tại (dù bị Disabled)
                var currentPhongBan = await _context.PhongBans
                                            .Where(p => p.MaPhong == model.MaPhong)
                                            .Select(p => new SelectListItem 
                                            { 
                                                Value = p.MaPhong, 
                                                Text = $"{p.TenPhong} ({p.MaPhong}) - Ngừng hoạt động" 
                                            })
                                            .FirstOrDefaultAsync();

                // Thêm phòng ban hiện tại vào đầu danh sách để nó hiển thị và được chọn
                if (currentPhongBan != null)
                {
                    activePhongBans.Insert(0, currentPhongBan);
                }
            }

            model.AvailablePhongBan = activePhongBans; // Gán danh sách đã xử lý

            // Lấy danh sách Roles (chỉ lấy Role Active)
            model.AvailableRoles = await _roleManager.Roles
                                            .Where(r => r.TrangThai == "A")
                                            .OrderBy(r => r.Name)
                                            .Select(r => new SelectListItem 
                                            { 
                                                Value = r.Name, 
                                                Text = r.Name 
                                            })
                                            .ToListAsync();
            // CurrentRoleNames đã được gán ở Edit GET action
        }
    }
}
