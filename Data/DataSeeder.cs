using CTOM.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CTOM.Data
{
    public class DataSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DataSeeder> _logger;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;

        public DataSeeder(ApplicationDbContext context, ILogger<DataSeeder> logger, RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, IConfiguration configuration, IHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _roleManager = roleManager;
            _userManager = userManager;
            _configuration = configuration;
            _environment = environment;
        }

        public async Task SeedAsync()
        {
            _logger.LogInformation("Bắt đầu quá trình khởi tạo dữ liệu.");
            try
            {
                // Seed domain data first
                await SeedFromJsonAsync<PhongBan>("PhongBan.json");
                await SeedFromJsonAsync<BusinessOperation>("BusinessOperations.json");
                await SeedFromJsonAsync<AvailableCifField>("AvailableCifFields.json");

                // Seed Identity data from JSON files
                await SeedIdentityFromJsonAsync();

                _logger.LogInformation("Quá trình khởi tạo dữ liệu hoàn tất.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Đã xảy ra lỗi trong quá trình khởi tạo dữ liệu.");
                throw;
            }
        }

        private async Task SeedIdentityFromJsonAsync()
        {
            if (await _userManager.Users.AnyAsync())
            {
                _logger.LogInformation("Bảng Users đã có dữ liệu. Bỏ qua seeding dữ liệu Identity.");
                return;
            }

            _logger.LogInformation("Bắt đầu khởi tạo dữ liệu Identity từ file JSON.");

            var defaultPassword = _configuration.GetValue<string>("SeederSettings:DefaultPasswordForSeededUsers");
            if (string.IsNullOrEmpty(defaultPassword))
            {
                _logger.LogError("Mật khẩu mặc định cho người dùng chưa được cấu hình trong 'SeederSettings:DefaultPasswordForSeededUsers'.");
                return;
            }

            // Load data from JSONs
            var roles = await LoadJsonAsync<List<JsonRole>>("NhomQuyen.json");
            var users = await LoadJsonAsync<List<JsonUser>>("NguoiSuDung.json");
            var userRoles = await LoadJsonAsync<List<JsonUserRole>>("NguoiSuDung_NhomQuyen.json");

            if (roles == null || users == null || userRoles == null) return;

            // Create maps from old IDs to names
            var oldRoleIdToNameMap = roles.ToDictionary(r => r.Id, r => r.Name);
            var oldUserIdToNameMap = users.ToDictionary(u => u.Id, u => u.UserName);

            // 1. Seed Roles
            _logger.LogInformation("Đang tạo {Count} vai trò...", roles.Count);
            foreach (var roleDto in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleDto.Name))
                {
                    var newRole = new ApplicationRole(roleDto.Name)
                    {
                        TenNhomDayDu = roleDto.TenNhomDayDu,
                        TrangThai = roleDto.TrangThai
                    };
                    await _roleManager.CreateAsync(newRole);
                }
            }

            // 2. Seed Users
            _logger.LogInformation("Đang tạo {Count} người dùng với mật khẩu mặc định...", users.Count);
            foreach (var userDto in users)
            {
                if (await _userManager.FindByNameAsync(userDto.UserName) == null)
                {
                    var newUser = new ApplicationUser
                    {
                        UserName = userDto.UserName,
                        TenUser = userDto.TenUser,
                        MaPhong = userDto.MaPhong,
                        TrangThai = userDto.TrangThai,
                        EmailConfirmed = true // Assume confirmed for seeded users
                    };
                    await _userManager.CreateAsync(newUser, defaultPassword);
                }
            }

            // 3. Seed User-Role relationships
            _logger.LogInformation("Đang gán {Count} lượt quyền cho người dùng...", userRoles.Count);
            foreach (var userRoleDto in userRoles)
            {
                if (!oldUserIdToNameMap.TryGetValue(userRoleDto.UserId, out var userName) ||
                    !oldRoleIdToNameMap.TryGetValue(userRoleDto.RoleId, out var roleName))
                {
                    _logger.LogWarning("Bỏ qua gán quyền không hợp lệ: UserId={UserId}, RoleId={RoleId}", userRoleDto.UserId, userRoleDto.RoleId);
                    continue;
                }

                var user = await _userManager.FindByNameAsync(userName);
                if (user != null && !await _userManager.IsInRoleAsync(user, roleName))
                {
                    await _userManager.AddToRoleAsync(user, roleName);
                }
            }

            _logger.LogInformation("Hoàn tất khởi tạo dữ liệu Identity.");
        }

        private async Task<T?> LoadJsonAsync<T>(string fileName) where T : class
        {
            var filePath = Path.Combine(_environment.ContentRootPath, "Data", "SeedData", fileName);
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Không tìm thấy file seed: {FilePath}", filePath);
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task SeedFromJsonAsync<TEntity>(string fileName) where TEntity : class
        {
            var entitiesInJson = await LoadJsonAsync<List<TEntity>>(fileName);
            if (entitiesInJson == null || !entitiesInJson.Any())
            {
                return; // Không có dữ liệu trong file JSON để seed
            }

            var dbSet = _context.Set<TEntity>();
            var primaryKeyProperty = _context.Model.FindEntityType(typeof(TEntity))!.FindPrimaryKey()!.Properties.First();

            var existingEntities = await dbSet.ToListAsync();
            var entitiesToAdd = new List<TEntity>();

            foreach (var entityInJson in entitiesInJson)
            {
                var pkValue = entityInJson.GetType().GetProperty(primaryKeyProperty.Name)!.GetValue(entityInJson);
                var isExisting = existingEntities.Any(e =>
                {
                    var existingPkValue = e.GetType().GetProperty(primaryKeyProperty.Name)!.GetValue(e);
                    return existingPkValue!.Equals(pkValue);
                });

                if (!isExisting)
                {
                    entitiesToAdd.Add(entityInJson);
                }
            }

            if (entitiesToAdd.Any())
            {
                await dbSet.AddRangeAsync(entitiesToAdd);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Đã seed {Count} bản ghi mới vào bảng {TableName}.", entitiesToAdd.Count, typeof(TEntity).Name);
            }
            else
            {
                _logger.LogInformation("Bảng {TableName} đã có đầy đủ dữ liệu từ file seed. Không có gì để thêm mới.", typeof(TEntity).Name);
            }
        }
    }

    // Helper classes for deserializing JSON data
    internal class JsonRole
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string TenNhomDayDu { get; set; } = "";
        public string TrangThai { get; set; } = "A";
    }

    internal class JsonUser
    {
        public string Id { get; set; } = "";
        public string UserName { get; set; } = "";
        public string TenUser { get; set; } = "";
        public string MaPhong { get; set; } = "";
        public string TrangThai { get; set; } = "A";
    }

    internal class JsonUserRole
    {
        public string UserId { get; set; } = "";
        public string RoleId { get; set; } = "";
    }
}