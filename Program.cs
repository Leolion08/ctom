/*
--------------------------------------------------------------------------------
 PHÂN TÍCH VÀ ĐỀ XUẤT TỐI ƯU HÓA - Bởi Gemini (AI)
--------------------------------------------------------------------------------
Chào bạn,
Dưới đây là các phân tích và đề xuất của tôi cho file Program.cs này.
Mục tiêu là làm cho file cấu hình trở nên sạch sẽ, hiệu quả và dễ bảo trì hơn.

1. Cấu trúc lại luồng khởi động:
   - Tôi đã sắp xếp lại toàn bộ file theo một luồng logic chuẩn của ASP.NET Core:
     1. Khởi tạo Builder
     2. Đăng ký Cấu hình (Configuration)
     3. Đăng ký Dịch vụ (Dependency Injection) -> Được chia thành các nhóm nhỏ hơn (DB, Identity, MVC, Services...)
     4. Xây dựng và Cấu hình HTTP Request Pipeline (Middleware) -> Sắp xếp middleware theo đúng thứ tự quan trọng.
     5. Các tác vụ sau khi Build (Data Seeding).
     6. Chạy ứng dụng.
   - Các comment phân nhóm đã được thêm vào để làm rõ từng khu vực.

2. Loại bỏ các Cấu hình Lặp lại (Đã thực hiện trong code bên dưới):
   - `AddControllersWithViews()`: Được gọi nhiều lần. Tôi đã gộp thành một lần gọi duy nhất và xâu chuỗi các cấu hình liên quan (`.AddRazorRuntimeCompilation()`, `.AddJsonOptions(...)`).
   - `AddLogging()`: Được gọi 2 lần. Đã gộp thành một khối cấu hình duy nhất.
   - `Configure<TemplateSettings>(...)` và `AddScoped<ITemplateStorageService, ...>`: Bị lặp lại. Đã giữ lại một lần duy nhất.

3. Đề xuất Tối ưu hóa (Những điểm bạn có thể xem xét thay đổi):

   - ĐỀ XUẤT 1: Cấu hình ExcelImportConfig
     - HIỆN TẠI:
       var excelImportConfig = new ExcelImportConfig();
       builder.Configuration.Bind(excelImportConfig);
       builder.Services.Configure<ExcelImportConfig>(builder.Configuration);
     - PHÂN TÍCH: Dòng `builder.Configuration.Bind(excelImportConfig);` không cần thiết và không sử dụng đối tượng `excelImportConfig` sau đó. Dòng `builder.Services.Configure<ExcelImportConfig>(builder.Configuration);` đã đủ để đăng ký và bind toàn bộ section cấu hình vào `ExcelImportConfig`.
     - ĐỀ XUẤT: Xóa 2 dòng đầu, chỉ giữ lại `builder.Services.Configure<ExcelImportConfig>(builder.Configuration);`. Điều này làm code gọn hơn và tuân thủ đúng chuẩn của .NET Core.

   - ĐỀ XUẤT 2: Cấu hình Static Files cho thư mục Template
     - HIỆN TẠI:
       builder.Services.AddSingleton(staticFileOptions);
       ...
       app.UseStaticFiles(staticFileOptions);
     - PHÂN TÍCH: Việc đăng ký `StaticFileOptions` như một Singleton là không theo chuẩn và không cần thiết. Các options này chỉ cần được tạo và truyền trực tiếp vào middleware `app.UseStaticFiles()`.
     - ĐỀ XUẤT: Xóa dòng `builder.Services.AddSingleton(staticFileOptions);`. Logic tạo đối tượng `staticFileOptions` nên được đặt ngay trước khi gọi `app.UseStaticFiles(staticFileOptions)` trong phần cấu hình pipeline để tăng tính rõ ràng. (Tôi đã giữ lại cấu trúc hiện tại của bạn trong code bên dưới nhưng đây là điểm nên tối ưu).

   - ĐỀ XUẤT 3: Cấu hình JSON Serialization
     - HIỆN TẠI: Có nhiều đoạn cấu hình `AddJsonOptions` và một biến `jsonOptions` được tạo nhưng dùng lặp lại.
     - PHÂN TÍCH: Có thể gộp tất cả các cấu hình JSON vào một nơi duy nhất khi gọi `AddControllersWithViews` và `AddRazorPages` để tránh trùng lặp và dễ quản lý. Việc bật reflection-based serialization (`DefaultJsonTypeInfoResolver`) có thể cần thiết cho .NET 7+, nhưng cần đảm bảo nó không gây ảnh hưởng hiệu năng nếu không thực sự cần thiết.
     - ĐỀ XUẤT: Gộp thành một lần cấu hình duy nhất trong `AddControllersWithViews`.

   - ĐỀ XUẤT 4: Pragma Warning
     - HIỆN TẠI: Có dòng `#pragma warning restore MVC1000, IL2026, IL3050` nhưng không có `#pragma warning disable` tương ứng ở trước.
     - ĐỀ XUẤT: Nếu không có lý do đặc biệt để giữ lại, nên xóa dòng này để tránh gây khó hiểu.

--------------------------------------------------------------------------------
*/

using CTOM.Data;
using CTOM.Models.Config;
using CTOM.Models.Entities;
using CTOM.Models.Settings;
using CTOM.Services;
using CTOM.Services.Interfaces;
using CTOM.Services.Identity; // custom claims factory
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http.Features;

// =============================================================================
// 1. KHỞI TẠO BUILDER
// =============================================================================
var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// 2. ĐĂNG KÝ CẤU HÌNH (CONFIGURATION)
// =============================================================================

// --- Cấu hình cho file import Excel ---
var configPath = Path.Combine(builder.Environment.ContentRootPath, "Configurations", "cifKhdnConfig.json");
if (!System.IO.File.Exists(configPath))
{
    throw new FileNotFoundException($"Không tìm thấy file cấu hình import tại: {configPath}");
}
// Thêm file config vào hệ thống, tự động nạp lại khi có thay đổi
builder.Configuration.AddJsonFile(path: configPath, optional: false, reloadOnChange: true);


// =============================================================================
// 3. ĐĂNG KÝ DỊCH VỤ (DEPENDENCY INJECTION)
// =============================================================================

// --- 3.1. Cấu hình Database (DbContext) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString,
        sqlServerOptions => sqlServerOptions.CommandTimeout(180)));

// --- 3.2. Cấu hình Identity (Người dùng và Vai trò) ---
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    // Bỏ các ràng buộc mật khẩu để linh hoạt
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 1;

    // Cấu hình User
    options.User.RequireUniqueEmail = false; // Tắt yêu cầu email duy nhất
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cấu hình cookie xác thực
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Error/AccessDenied";
    options.ReturnUrlParameter = "returnUrl";
    options.Cookie.HttpOnly = true;
    //options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Luôn dùng cookie an toàn (Hoạt động với https?)
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// --- 3.3. Cấu hình MVC, Razor Pages, Controllers và JSON ---
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressAsyncSuffixInActionNames = true; // Không yêu cầu hậu tố 'Async' trong tên action
})
.AddRazorRuntimeCompilation() // Cho phép tự động biên dịch lại view khi có thay đổi (hữu ích trong development)
.AddJsonOptions(options =>
{
    // Cấu hình chung cho JSON serialization
    ConfigureJsonOptions(options.JsonSerializerOptions);
});

builder.Services.AddRazorPages(); // Thêm dịch vụ cho Razor Pages

// Cấu hình JSON cho các usage thủ công (service, logger, DB...)
builder.Services.Configure<JsonSerializerOptions>(ConfigureJsonOptions);

// --- 3.4. Cấu hình Dịch vụ của Ứng dụng (Application Services & Settings) ---
// Đăng ký các đối tượng settings từ appsettings.json
builder.Services.Configure<TemplateSettings>(builder.Configuration.GetSection("TemplateSettings"));
builder.Services.Configure<ExcelImportConfig>(builder.Configuration);

// Đăng ký các dịch vụ tùy chỉnh
builder.Services.AddScoped<ITemplateStorageService, TemplateStorageService>();
// Đăng ký các service truy cập người dùng và FormData
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<IFormDataService, FormDataService>();
builder.Services.AddScoped<ExcelImportService>();
//builder.Services.AddScoped<HtmlToDocxService>(); //Khong su dung
//builder.Services.AddScoped<DocxPlaceholderMappingService>();
builder.Services.AddScoped<DocxToStructuredHtmlService>();
builder.Services.AddScoped<DocxPlaceholderInsertionService>();
// Register custom claims factory so MaPhong & TenUser are always present
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, AppClaimsPrincipalFactory>();
builder.Services.AddScoped<DataSeeder>(); // Đăng ký DataSeeder

// --- 3.5. Cấu hình Các Dịch vụ Framework Khác ---
// Cấu hình localization (đa ngôn ngữ)
var supportedCultures = new[] { "vi-VN", "en-US" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
// Áp dụng văn hóa mặc định cho luồng xử lý
var cultureInfo = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Cấu hình Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    //options.Cookie.SecurePolicy = CookieSecurePolicy.Always; //Hoạt động với https
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Cấu hình HttpContextAccessor để truy cập HttpContext từ các service
builder.Services.AddHttpContextAccessor();

// Cấu hình giới hạn kích thước file upload
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2097152; // 2MB
});

// // Cấu hình Data Protection để lưu trữ key ra file system
// // Giúp giải quyết vấn đề mất session/login khi IIS App Pool recycle
// builder.Services.AddDataProtection()
//     .PersistKeysToFileSystem(new DirectoryInfo(@"C:\Keys\CTOM-Keys"))
//     .SetApplicationName("CTOM");

// Tạo đường dẫn đến thư mục 'Keys' bên trong thư mục gốc của ứng dụng
var keysPath = Path.Combine(builder.Environment.ContentRootPath, "Keys");

// Cấu hình Data Protection để lưu trữ key vào đường dẫn tương đối đó
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("CTOM");

// Cấu hình Logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddSimpleConsole(options =>
    {
        options.IncludeScopes = true;
        options.SingleLine = true;
        options.TimestampFormat = "[HH:mm:ss] ";
    });
    // Lọc bớt các log không cần thiết từ hệ thống
    //loggingBuilder.AddFilter("Microsoft", LogLevel.Warning);
    //loggingBuilder.AddFilter("System", LogLevel.Warning);
    // Cho phép hiển thị tất cả log từ ứng dụng của chúng ta
    loggingBuilder.AddFilter("CTOM", LogLevel.Information);
});


// =============================================================================
// 4. XÂY DỰNG VÀ CẤU HÌNH HTTP REQUEST PIPELINE (MIDDLEWARE)
// =============================================================================
var app = builder.Build();

// =============================================================================
// 4.1. Cấu hình Middleware theo Môi trường (Development vs. Production)
// =============================================================================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// =============================================================================
// 4.2. Cấu hình các Middleware Chung (Thứ tự rất quan trọng)
// =============================================================================

// Lấy tên ứng dụng từ cấu hình để đặt PathBase. 
// PHẢI đặt trước các middleware khác như StaticFiles, Routing, Authentication.
var appName = app.Configuration.GetValue<string>("AppName");
if (!string.IsNullOrEmpty(appName) && appName != "/")
{
    app.UsePathBase($"/{appName.Trim('/')}");
}

// Chuyển hướng HTTP sang HTTPS
app.UseHttpsRedirection();

// Sử dụng localization đã cấu hình
app.UseRequestLocalization(localizationOptions);

// Middleware phục vụ các file tĩnh từ thư mục wwwroot
app.UseStaticFiles();

// Middleware phục vụ các file tĩnh từ thư mục Template (cấu hình riêng)
var templateSettings = app.Configuration.GetSection("TemplateSettings").Get<TemplateSettings>();
var templateFolder = !string.IsNullOrEmpty(templateSettings?.RootPath) ? templateSettings.RootPath : "TemplatesData";
var templateRootPath = Path.Combine(app.Environment.ContentRootPath, templateFolder);
if (!Directory.Exists(templateRootPath))
{
    Directory.CreateDirectory(templateRootPath);
}
var staticFileOptions = new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(templateRootPath),
    RequestPath = $"/{templateFolder.Trim('/')}",
    ServeUnknownFileTypes = true // Cho phép phục vụ các kiểu file không xác định
};
if (app.Environment.IsDevelopment())
{
    // Tắt cache trong môi trường development để dễ debug
    staticFileOptions.OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "no-cache, no-store";
        ctx.Context.Response.Headers.Expires = "-1";
    };
}
app.UseStaticFiles(staticFileOptions);


// Bật cơ chế định tuyến (routing)
app.UseRouting();

// Sử dụng Session
app.UseSession();

// Bật xác thực (Authentication) - Phải đứng trước Authorization
app.UseAuthentication();

// Bật phân quyền (Authorization)
app.UseAuthorization();


// --- 4.3. Cấu hình Điểm cuối (Endpoint Mapping) ---
app.MapControllerRoute(
    name: "khachhangdn",
    pattern: "KhachHangDN/{action=Index}/{id?}",
    defaults: new { controller = "KhachHangDN" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.MapControllers(); // Map các API controllers


// =============================================================================
// 5. CÁC TÁC VỤ SAU KHI BUILD (DATA SEEDING)
// =============================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        if ((await context.Database.GetPendingMigrationsAsync()).Any())
        {
            await context.Database.MigrateAsync(); // Đảm bảo DB được tạo và migrate
        }

        var seeder = services.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync(); // "Gieo" dữ liệu
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi trong quá trình khởi tạo hoặc migrate database.");
    }
}


// =============================================================================
// 6. CHẠY ỨNG DỤNG
// =============================================================================
app.Run();


// =============================================================================
// 7. CẤU HÌNH JSON (local function)
// =============================================================================

static void ConfigureJsonOptions(JsonSerializerOptions options)
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.ReferenceHandler = ReferenceHandler.IgnoreCycles; // Bỏ qua vòng lặp tham chiếu
    options.NumberHandling = JsonNumberHandling.AllowReadingFromString; // Cho phép đọc số từ chuỗi
    options.TypeInfoResolver = new DefaultJsonTypeInfoResolver(); // Cho phép reflection, Cần thiết cho .NET 7+
}
