using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using System.Web;
using CTOM.Data;
using CTOM.Models;
using CTOM.Models.DTOs;
using CTOM.Models.Entities;
using CTOM.Models.Enums;
using CTOM.Models.Responses;
using CTOM.Models.Settings;
//using CTOM.ViewModels;
using CTOM.Services;
using CTOM.Services.Interfaces;
using CTOM.ViewModels.Template;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
// Sử dụng Newtonsoft.Json thay vì System.Text.Json để tránh xung đột
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CTOM.Controllers
{
    [Authorize(Roles = "ADMIN,HTTD")]
    [Route("[controller]")]
    public class TemplateController : Controller
    {
        private readonly ILogger<TemplateController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ICurrentUserAccessor _currentUser;
        private readonly ITemplateStorageService _templateStorageService;
        private readonly TemplateSettings _templateSettings;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HtmlToDocxService _htmlToDocxService;
        //private readonly DocxPlaceholderMappingService _docxPlaceholderMappingService;
        private readonly DocxToStructuredHtmlService _docxToHtmlService;

        public TemplateController(
            ILogger<TemplateController> logger,
            ApplicationDbContext context,
            IWebHostEnvironment hostingEnvironment,
            ITemplateStorageService templateStorageService,
            ICurrentUserAccessor currentUserAccessor,
            IOptions<TemplateSettings> templateSettings,
            UserManager<ApplicationUser> userManager,
            HtmlToDocxService htmlToDocxService,
            //DocxPlaceholderMappingService docxPlaceholderMappingService
            DocxToStructuredHtmlService docxToHtmlService
            )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            _templateStorageService = templateStorageService ?? throw new ArgumentNullException(nameof(templateStorageService));
            _currentUser = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
            _templateSettings = templateSettings?.Value ?? throw new ArgumentNullException(nameof(templateSettings));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _htmlToDocxService = htmlToDocxService ?? throw new ArgumentNullException(nameof(htmlToDocxService));
            //_docxPlaceholderMappingService = docxPlaceholderMappingService ?? throw new ArgumentNullException(nameof(docxPlaceholderMappingService));
            _docxToHtmlService = docxToHtmlService ?? throw new ArgumentNullException(nameof(docxToHtmlService));
        }

        // --- LOẠI BỎ PHƯƠNG THỨC HELPER ReadFileSafelyAsync ---
        // Logic này đã được chuyển vào ITemplateStorageService

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {


            // Lấy danh sách nghiệp vụ cho dropdown lọc
            ViewBag.BusinessOperations = _context.BusinessOperations
                .OrderBy(bo => bo.OperationName)
                .Select(bo => new SelectListItem(bo.OperationName, bo.OperationID.ToString()))
                .ToList();

            // Thêm mục mặc định
            ViewBag.BusinessOperations.Insert(0, new SelectListItem("Tất cả nghiệp vụ", ""));

            return View();
        }

        /// <summary>
        /// Lấy dữ liệu cho DataTables với logic chuẩn (lọc, sắp xếp, phân trang phía server).
        /// Đây là hàm mẫu để áp dụng cho các trang khác.
        /// </summary>
        [HttpPost("GetDataTable")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetDataTable([FromForm] DataTablesRequest dtRequest, [FromForm(Name = "q")] string? customSearch)
        {
            try
            {
                // --- 1. KHỞI TẠO TRUY VẤN CƠ SỞ (BASE QUERY) ---
                // Bắt đầu với IQueryable để EF Core có thể xây dựng và tối ưu hóa câu lệnh SQL cuối cùng.
                // Luôn dùng AsNoTracking() cho các truy vấn chỉ đọc để tăng hiệu năng.
                var query = _context.Templates
                    .Include(t => t.BusinessOperation)
                    .Include(t => t.CreatedByUser)
                    .AsNoTracking()
                    .AsQueryable();

                // --- 2. LỌC THEO QUYỀN HẠN (AUTHORIZATION FILTERING) ---
                var currentUser = await _userManager.GetUserAsync(User);
                var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "ADMIN");
                if (!isAdmin && currentUser != null)
                {
                    query = query.Where(t =>
                        t.CreatedByUserName == currentUser.UserName ||
                        t.CreatedDepartmentID == currentUser.MaPhong);
                }

                // --- 3. ĐẾM TỔNG SỐ BẢN GHI (TOTAL RECORDS COUNT) ---
                // Đếm tổng số bản ghi *trước khi* áp dụng bộ lọc tìm kiếm.
                int recordsTotal = await query.CountAsync();

                // --- 4. ÁP DỤNG BỘ LỌC TÌM KIẾM (SEARCH/FILTERING) ---
                if (!string.IsNullOrWhiteSpace(customSearch))
                {
                    var filter = customSearch.Trim();
                    // Dùng Collate để tìm kiếm không phân biệt chữ hoa/thường trong SQL Server.
                    query = query.Where(t =>
                        (t.TemplateName != null && EF.Functions.Like(EF.Functions.Collate(t.TemplateName, "SQL_Latin1_General_CP1_CI_AI"), $"%{filter}%")) ||
                        (t.CreatedByUser != null && t.CreatedByUser.UserName != null && EF.Functions.Like(EF.Functions.Collate(t.CreatedByUser.UserName, "SQL_Latin1_General_CP1_CI_AI"), $"%{filter}%"))
                    );
                }

                // --- 5. ĐẾM SỐ BẢN GHI SAU KHI LỌC (FILTERED RECORDS COUNT) ---
                // Đếm số bản ghi *sau khi* đã áp dụng bộ lọc.
                int recordsFiltered = await query.CountAsync();

                // --- 6. XỬ LÝ SẮP XẾP (LOGIC CHUẨN VỚI TỐI ƯU MAPPING) ---
                var order = dtRequest.Order.FirstOrDefault();
                if (order != null && order.Column < dtRequest.Columns.Count)
                {
                    var clientSortColumn = dtRequest.Columns[order.Column].Name;
                    var sortDirection = order.Dir;

                    // "Bộ từ điển" ánh xạ tên cột từ client sang đường dẫn của Entity Framework.
                    // camelCase to PascalCase
                    var columnMapping = new Dictionary<string, string>
                    {
                        { "templateId", "TemplateID" },
                        { "businessOperationName", "BusinessOperation.OperationName" },
                        { "templateName", "TemplateName" },
                        { "isActive", "IsActive" },
                        { "createdByUserName", "CreatedByUserName" },
                        { "lastModificationTimestamp", "LastModificationTimestamp" },
                        { "status", "Status" }
                    };

                    if (!string.IsNullOrEmpty(clientSortColumn) && columnMapping.TryGetValue(clientSortColumn, out var serverSortColumn))
                    {
                        query = query.OrderBy($"{serverSortColumn} {sortDirection}");
                    }
                    else
                    {
                        query = query.OrderByDescending(t => t.LastModificationTimestamp);
                    }
                }
                else
                {
                    query = query.OrderByDescending(t => t.LastModificationTimestamp);
                }

                // --- 7. PHÂN TRANG (PAGINATION) ---
                // Áp dụng Skip() và Take() để chỉ lấy dữ liệu cho trang hiện tại.
                var pagedData = await query
                    .Skip(dtRequest.Start)
                    .Take(dtRequest.Length)
                    // --- 8. CHUYỂN ĐỔI DỮ LIỆU (PROJECTION) ---
                    // Dùng .Select() để tạo đối tượng mới, chỉ lấy các trường cần thiết.
                    .Select(t => new
                    {
                        templateId = t.TemplateID,
                        templateName = t.TemplateName,
                        businessOperationName = t.BusinessOperation != null ? t.BusinessOperation.OperationName : "",
                        status = t.Status,
                        createdByUserName = t.CreatedByUser != null ? t.CreatedByUser.UserName : (t.CreatedByUserName ?? "System"),
                        lastModificationTimestamp = t.LastModificationTimestamp,
                        isActive = t.IsActive
                    })
                    .ToListAsync();

                // --- 9. ĐÓNG GÓI PHẢN HỒI (RESPONSE FORMATTING) ---
                // Trả về đối tượng DataTablesResponse<T> chuẩn.
                var response = new DataTablesResponse<object>
                {
                    Draw = dtRequest.Draw,
                    RecordsTotal = recordsTotal,
                    RecordsFiltered = recordsFiltered,
                    Data = pagedData.Cast<object>().ToList()
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải dữ liệu template cho DataTables.");
                return Json(new DataTablesResponse<object>
                {
                    Draw = dtRequest.Draw,
                    Error = "Có lỗi xảy ra phía máy chủ."
                });
            }
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            try
            {
                await LoadDropdowns();

                // Truyền cấu hình sang view
                ViewData["MaxFileSizeMB"] = _templateSettings.MaxFileSizeMB;
                ViewData["MaxFileSizeBytes"] = _templateSettings.MaxFileSizeBytes;

                return View(new TemplateViewModel
                {
                    Status = TemplateStatus.Draft.ToString() // Mặc định là bản nháp
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải trang tạo mới template");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang tạo mới template. Vui lòng thử lại sau.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10MB max request size
        public async Task<IActionResult> Create(TemplateViewModel model)
        {
            try
            {
                // Kiểm tra ModelState
                if (!ModelState.IsValid)
                {
                    await LoadDropdowns();
                    return View(model);
                }

                // Kiểm tra file upload
                var file = Request.Form.Files["templateFile"];
                if (file == null || file.Length == 0)
                {
                    ModelState.AddModelError("", "Vui lòng chọn file template");
                    await LoadDropdowns();
                    return View(model);
                }

                // Validate file extension
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_templateSettings.AllowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("", "Chỉ chấp nhận file .docx");
                    await LoadDropdowns();
                    return View(model);
                }

                // Validate file size
                if (file.Length > _templateSettings.MaxFileSizeBytes)
                {
                    ModelState.AddModelError("", $"Kích thước file không được vượt quá {_templateSettings.MaxFileSizeMB}MB");
                    await LoadDropdowns();
                    return View(model);
                }

                // Lưu file tạm thởi để kiểm tra nội dung
                string? tempFilePath = null;
                try
                {
                    tempFilePath = Path.GetTempFileName();
                    await using var stream = new FileStream(tempFilePath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    // TODO: Thêm kiểm tra nội dung file nếu cần
                    // Ví dụ: Kiểm tra có phải là file .docx hợp lệ không
                }
                finally
                {
                    // Xóa file tạm
                    if (tempFilePath != null && System.IO.File.Exists(tempFilePath))
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                }

                // Lấy thông tin người dùng hiện tại
                var currentUser = await _userManager.GetUserAsync(User);
                var currentUserName = currentUser?.UserName ?? "System";
                var now = DateTime.Now;

                // Tạo đối tượng template mới
                var template = new Templates
                {
                    TemplateName = model.TemplateName.Trim(),
                    BusinessOperationID = model.BusinessOperationId,
                    Description = model.Description?.Trim(),
                    //Status = "Draft", //Mac định là bản nháp (Draft, Mapped)
                    Status = TemplateStatus.Draft.ToString(),
                    OriginalUploadFileName = file.FileName,
                    CreatedByUserName = currentUserName,
                    CreatedDepartmentID = currentUser?.MaPhong,
                    CreationTimestamp = now,
                    LastModifiedByUserName = currentUserName,
                    LastModificationTimestamp = now,
                    IsActive = false,
                    SharingType = "Private" // Mặc định là Private
                };

                // Lưu vào database để lấy ID
                _context.Templates.Add(template);
                await _context.SaveChangesAsync();

                // Kiểm tra BusinessOperationId không null trước khi lưu file
                if (!model.BusinessOperationId.HasValue)
                {
                    ModelState.AddModelError(string.Empty, "Vui lòng chọn nghiệp vụ");
                    // Lấy lại danh sách nghiệp vụ để hiển thị lại form
                    var businessOperations = await _context.BusinessOperations
                        .Where(bo => bo.ParentOperationID == null)
                        .Include(bo => bo.ChildOperations)
                        .OrderBy(bo => bo.OperationName)
                        .ToListAsync();

                    model.BusinessOperations = new SelectList(businessOperations, "BusinessOperationID", "OperationName");
                    return View(model);
                }

                // Lưu file template và nhận đường dẫn đầy đủ
                var savedFilePath = await _templateStorageService.SaveTemplateAsync(
                    file,
                    currentUserName,
                    model.BusinessOperationId.Value,
                    template.TemplateID);

                // Lưu đường dẫn đầy đủ vào database
                template.OriginalDocxFilePath = savedFilePath;
                //template.MappedDocxFilePath = $"/templates/{userId}/{template.BusinessOperationID}/{template.TemplateID}_original.docx";
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo mới template thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Lỗi cơ sở dữ liệu khi tạo mới template");
                ModelState.AddModelError("", "Có lỗi xảy ra khi lưu dữ liệu. Vui lòng kiểm tra lại thông tin.");
                await LoadDropdowns();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo mới template");
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo mới template. Vui lòng thử lại sau.");
                await LoadDropdowns();
                return View(model);
            }
        }

        // SỬA ĐỔI: Truyền MaxTableNestingLevel khi chuyển đổi DOCX sang HTML.
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var template = await _context.Templates
                .Include(t => t.BusinessOperation)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TemplateID == id);

            if (template is null)
            {
                return NotFound($"Không tìm thấy template có ID: {id}");
            }

            // Quyết định file sẽ hiển thị và convert sang HTML
            string? htmlContent = null;
            bool hasFile = false;

            try
            {
                string? relativeFilePath = null;
                if (template.Status?.Equals("Mapped", StringComparison.OrdinalIgnoreCase) == true &&
                    !string.IsNullOrEmpty(template.MappedDocxFilePath))
                {
                    relativeFilePath = template.MappedDocxFilePath;
                }
                else if (!string.IsNullOrEmpty(template.OriginalDocxFilePath))
                {
                    relativeFilePath = template.OriginalDocxFilePath;
                }

                if (!string.IsNullOrEmpty(relativeFilePath))
                {
                    // SỬA ĐỔI: Gọi service để đọc file an toàn
                    var docxBytes = await _templateStorageService.GetFileBytesAsync(relativeFilePath);

                    if (docxBytes != null)
                    {
                        htmlContent = _docxToHtmlService.ConvertToHtml(docxBytes, _templateSettings.MaxTableNestingLevel, isViewMode: true);
                        hasFile = true;
                        _logger.LogInformation("Successfully converted DOCX to HTML for template {TemplateId}", id);
                    }
                    else
                    {
                        _logger.LogWarning("Không thể đọc file template: {FilePath}", relativeFilePath);
                        htmlContent = "<p class='text-red-500 font-bold'>Lỗi: Không thể đọc file template. File có thể không tồn tại hoặc đang bị khóa.</p>";
                        hasFile = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting DOCX to HTML for template {TemplateId}", id);
                // htmlContent = null;
                htmlContent = "<p class='text-red-500 font-bold'>Lỗi nghiêm trọng khi chuyển đổi file sang HTML.</p>";
            }

            // Chỉ người tạo template mới được phép ánh xạ
            var isOwner = string.Equals(template.CreatedByUserName, _currentUser.UserName, StringComparison.OrdinalIgnoreCase);
            var canMapping = isOwner && !string.Equals(template.Status, "Mapped", StringComparison.OrdinalIgnoreCase);

            var viewModel = new TemplateDetailsVM
            {
                TemplateId = template.TemplateID,
                BusinessOperationName = template.BusinessOperation?.OperationName,
                TemplateName = template.TemplateName,
                Description = template.Description,
                Status = template.Status ?? "Draft",
                IsActive = template.IsActive,
                HtmlContent = htmlContent,
                HasFile = hasFile,
                CreatedByUserName = template.CreatedByUserName,
                CreatedDepartmentID = template.CreatedDepartmentID,
                CreationTimestamp = template.CreationTimestamp,
                LastModificationTimestamp = template.LastModificationTimestamp,
                LastModifiedByUserName = template.LastModifiedByUserName,
                CanMapping = canMapping
            };

            return View(viewModel);
        }


        [HttpPost("Delete/{id}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // SỬA ĐỔI: Sử dụng phương thức DeleteTemplate của service thay vì System.IO.File.Delete
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var template = await _context.Templates
                    .Include(t => t.TemplateFields)
                    .Include(t => t.TemplateSharingRules)
                    .Include(t => t.UserFavoriteTemplates)
                    .Include(t => t.FormDatas)
                    .FirstOrDefaultAsync(t => t.TemplateID == id);

                if (template == null) return Json(ApiResponse.Fail("Không tìm thấy template cần xóa."));
                if (string.Equals(template.Status, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    return Json(ApiResponse.Fail($"Không thể xóa template '{template.TemplateName}' đang ở trạng thái 'Active'. Vui lòng chuyển trạng thái trước khi xóa."));
                }

                var templateName = template.TemplateName;
                var templateId = template.TemplateID;
                var originalRelativePath = template.OriginalDocxFilePath;
                var mappedRelativePath = template.MappedDocxFilePath;

                if (template.TemplateFields?.Count > 0) _context.TemplateFields.RemoveRange(template.TemplateFields);
                if (template.TemplateSharingRules?.Count > 0) _context.TemplateSharingRules.RemoveRange(template.TemplateSharingRules);
                if (template.UserFavoriteTemplates?.Count > 0) _context.UserFavoriteTemplates.RemoveRange(template.UserFavoriteTemplates);
                if (template.FormDatas?.Count > 0) _context.FormDatas.RemoveRange(template.FormDatas);

                _context.Templates.Remove(template);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Xóa file vật lý sau khi transaction thành công
                if (!string.IsNullOrEmpty(originalRelativePath)) _templateStorageService.DeleteTemplate(originalRelativePath);
                if (!string.IsNullOrEmpty(mappedRelativePath)) _templateStorageService.DeleteTemplate(mappedRelativePath);

                _logger.LogInformation("Đã xóa template: {TemplateId} - {TemplateName}", templateId, templateName);
                TempData["SuccessMessage"] = $"Đã xóa template: {templateName}";

                return Json(ApiResponse.Ok($"Đã xóa thành công template '{templateName}'", new { id = templateId, name = templateName }));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xóa template: {TemplateId}", id);
                return Json(ApiResponse.Fail($"Đã xảy ra lỗi khi xóa template: {ex.Message}"));
            }
        }

        /// <summary>
        /// Lấy file template để hiển thị trong chế độ xem
        /// </summary>
        [HttpGet("GetTemplateFile/{id}/{type?}")]
        public async Task<IActionResult> GetTemplateFile(int id, string? type = "original")
        {
            try
            {
                var template = await _context.Templates.AsNoTracking().FirstOrDefaultAsync(t => t.TemplateID == id);
                if (template is null) return NotFound();

                var relativePath = type?.Equals("mapped", StringComparison.OrdinalIgnoreCase) == true
                    ? template.MappedDocxFilePath
                    : template.OriginalDocxFilePath;

                if (string.IsNullOrWhiteSpace(relativePath)) return NotFound("Template không có file.");

                // SỬA ĐỔI: Gọi service để đọc file an toàn
                var fileBytes = await _templateStorageService.GetFileBytesAsync(relativePath);
                if (fileBytes == null)
                {
                    return NotFound("Không tìm thấy hoặc không thể đọc file template.");
                }

                var fileName = Path.GetFileName(relativePath);
                Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileName}\"");
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy file template ID: {TemplateId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Đã xảy ra lỗi khi tải file template.");
            }
        }

        #region Private Helper Methods

        private async Task<bool> TemplateExists(int id)
        {
            return await _context.Templates.AnyAsync(e => e.TemplateID == id);
        }

        private async Task LoadDropdowns()
        {
            // Lấy danh sách nghiệp vụ đã được phân cấp
            var query = _context.BusinessOperations.AsQueryable();

            // Kiểm tra quyền của người dùng
            var isAdmin = User.IsInRole("ADMIN");
            var isHttd = User.IsInRole("HTTD");
            // Dự phòng cho HTTD_CN: var isHttdCn = User.IsInRole("HTTD_CN");

            // Lọc theo quyền
            if (!isAdmin){
                if (isHttd)
                {
                    // HTTD chỉ xem được nghiệp vụ DN
                    query = query.Where(b => b.CustomerType == "DN");
                }
                // Bỏ comment khi có thêm role HTTD_CN
                // else if (isHttdCn)
                // {
                //     // HTTD_CN chỉ xem được nghiệp vụ CN
                //     query = query.Where(b => b.CustomerType == "CN");
                // }
            }
            // Nếu là ADMIN hoặc không có role phù hợp, hiển thị tất cả

            var businessOperations = await query
                .OrderBy(b => b.OperationID)
                .Select(b => new
                {
                    b.OperationID,
                    b.OperationName,
                    b.ParentOperationID,
                    b.Description,
                    b.CustomerType
                })
                .ToListAsync();

            // Tạo danh sách phân cấp
            var result = new List<SelectListItem>();

            // Lấy danh sách nghiệp vụ cấp cha (ParentOperationID = null)
            var parentOperations = businessOperations
                .Where(b => b.ParentOperationID == null)
                .OrderBy(b => b.OperationID)
                .ToList();

            if (parentOperations.Count == 0 && businessOperations.Count > 0)
            {
                // Nếu không có parent nào nhưng vẫn có dữ liệu, hiển thị tất cả các mục
                foreach (var item in businessOperations.OrderBy(b => b.OperationID))
                {
                    result.Add(new SelectListItem
                    {
                        Text = item.OperationName,
                        Value = item.OperationID.ToString()
                    });
                }
            }
            else
            {
                // Hiển thị theo cấu trúc phân cấp
                foreach (var parent in parentOperations)
                {
                    // Thêm nghiệp vụ cấp cha với disabled
                    result.Add(new SelectListItem
                    {
                        Text = parent.OperationName,
                        Value = "",
                        Disabled = true
                    });

                    // Lấy danh sách nghiệp vụ cấp con
                    var childOperations = businessOperations
                        .Where(b => b.ParentOperationID == parent.OperationID)
                        .OrderBy(b => b.OperationID);

                    foreach (var child in childOperations)
                    {
                        // Thêm nghiệp vụ cấp con với dấu + và thụt lề
                        result.Add(new SelectListItem
                        {
                            Text = $"+ {child.OperationName} ({child.OperationID})",
                            Value = child.OperationID.ToString()
                        });
                    }
                }
            }

            ViewBag.BusinessOperations = result;
        }

        #endregion

        #region Mapping

        private bool CanCurrentUserMapping(Templates template)
        {
            if (template == null) return false;
            var isOwner = string.Equals(template.CreatedByUserName, _currentUser.UserName, StringComparison.OrdinalIgnoreCase);
            return isOwner && !string.Equals(template.Status, "Mapped", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Yêu cầu GET bất hợp lệ sẽ chuyển hướng kèm thông báo lỗi.
        /// </summary>
        [HttpGet("Mapping/{id}")]
        public IActionResult MappingGet(int id)
        {
            TempData["ErrorMessage"] = "Bạn không có quyền truy cập hoặc phương thức không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Hiển thị trang mapping cho template, đã được viết lại theo Giải pháp 2.
        /// </summary>
        [ValidateAntiForgeryToken]
        [HttpPost("Mapping/{id}")]
        // SỬA ĐỔI: Truyền MaxTableNestingLevel khi chuyển đổi DOCX sang HTML.
        public async Task<IActionResult> Mapping(int id)
        {
            try
            {
                _logger.LogInformation("Loading mapping page for template ID: {TemplateId}", id);
                var template = await _context.Templates.AsNoTracking().FirstOrDefaultAsync(t => t.TemplateID == id);
                if (template == null)
                {
                    TempData["ErrorMessage"] = $"Không tìm thấy template ID: {id}";
                    return RedirectToAction(nameof(Index));
                }

                if (!CanCurrentUserMapping(template))
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền ánh xạ template này hoặc template đã được ánh xạ.";
                    return RedirectToAction(nameof(Index));
                }

                // SỬA ĐỔI: Gọi service để đọc file an toàn
                var docxBytes = await _templateStorageService.GetFileBytesAsync(template.OriginalDocxFilePath);
                if (docxBytes == null)
                {
                    _logger.LogError("Template file not found or cannot be read: {RelativePath}", template.OriginalDocxFilePath);
                    TempData["ErrorMessage"] = "Không tìm thấy hoặc không thể đọc file template. Vui lòng liên hệ admin.";
                    return RedirectToAction(nameof(Index));
                }

                // *** THAY ĐỔI CỐT LÕI: GỌI SERVICE ĐỂ PHÂN TÍCH DOCX ***
                // Chuyển đổi DOCX sang HTML có nhúng metadata cấu trúc.
                //string structuredHtml = _docxToHtmlService.ConvertToHtml(docxBytes, isViewMode: false);
                // SỬA ĐỔI: Truyền giá trị từ settings.
                string structuredHtml = _docxToHtmlService.ConvertToHtml(docxBytes, _templateSettings.MaxTableNestingLevel, isViewMode: false);

                var availableFields = await GetAvailableFieldsAsync(); // Hàm của bạn

                var fieldViewModels = availableFields.Select(f => new FieldViewModel
                    {
                        Name = f.Name,
                        DisplayName = f.DisplayName,
                        DataType = f.FieldType ?? "TEXT", // Map FieldType vào DataType
                        DataSourceType = f.DataSourceType ?? "CIF",
                        DisplayOrder = f.DisplayOrder ?? 0

                    }).ToList();

                var viewModel = new TemplateMappingViewModel
                {
                    TemplateId = template.TemplateID,
                    TemplateName = template.TemplateName,
                    AvailableFields = fieldViewModels,
                    StructuredHtmlContent = structuredHtml // Truyền HTML đã được xử lý xuống View
                };

                // Truyền cấu hình MaxTableNestingLevel xuống View
                ViewData["MaxTableNestingLevel"] = _templateSettings.MaxTableNestingLevel;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading template mapping page for template ID: {TemplateId}", id);
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi khi tải template (ID: {id}). Vui lòng liên hệ với admin.";
                return RedirectToAction(nameof(Index));
                //return StatusCode(500, "Đã xảy ra lỗi khi tải trang mapping");
            }
        }

        /// <summary>
        /// API lưu thông tin mapping, đã được viết lại theo Giải pháp 2.
        /// </summary>
        [HttpPost("SaveMapping")]
        [ValidateAntiForgeryToken] // Nên có khi hoàn thiện
        // SỬA ĐỔI: Truyền MaxTableNestingLevel khi chuyển đổi DOCX sang HTML.
        public async Task<IActionResult> SaveMapping([FromBody] SaveMappingRequest request)
        {
            _logger.LogInformation("Starting to save mapping for template ID: {TemplateId}", request?.TemplateId);
            if (request == null || request.Fields.Count == 0)
            {
                _logger.LogWarning("Invalid mapping data received (null or no fields).");
                return BadRequest(new { success = false, message = "Không có dữ liệu mapping hợp lệ." });
            }

            // DEBUG: Log mapping data chi tiết
            _logger.LogInformation("Received mapping data: {FieldCount} fields", request.Fields.Count);
            foreach (var field in request.Fields)
            {
                _logger.LogInformation("Field: {FieldName}, Positions: {PositionCount}", field.FieldName, field.Positions.Count);
                foreach (var position in field.Positions)
                {
                    _logger.LogInformation("  - ElementId: {ElementId}, DocxPath: {DocxPath}, CharOffset: {CharOffset}, IsInNestedTable: {IsInNestedTable}",
                        position.ElementId, position.DocxPath, position.CharOffset, position.IsInNestedTable);
                }
            }

            // Validate theo MaxTableNestingLevel từ cấu hình
            // NestedDepth = 1 (bảng cha), NestedDepth = 2 (bảng con lồng 1 cấp), v.v.
            // MaxTableNestingLevel = 0: chỉ cho phép bảng cha (NestedDepth = 1)
            // MaxTableNestingLevel = 1: cho phép bảng cha + bảng con lồng 1 cấp (NestedDepth <= 2)
            var maxAllowedDepth = _templateSettings.MaxTableNestingLevel + 1; // +1 vì NestedDepth bắt đầu từ 1

            var invalidNestedPositions = request.Fields
                .SelectMany(f => f.Positions.Where(p => p.NestedDepth > maxAllowedDepth))
                .ToList();

            if (invalidNestedPositions.Count > 0)
            {
                var fieldNames = request.Fields
                    .Where(f => f.Positions.Any(p => p.NestedDepth > maxAllowedDepth))
                    .Select(f => f.FieldName)
                    .ToList();

                _logger.LogWarning("Attempted to map fields into tables nested too deeply: {FieldNames}, MaxAllowedDepth: {MaxDepth}",
                    string.Join(", ", fieldNames), maxAllowedDepth);

                var nestingLevelText = _templateSettings.MaxTableNestingLevel == 0 ? "bảng cha" :
                              _templateSettings.MaxTableNestingLevel == 1 ? "bảng cha và bảng con lồng 1 cấp" :
                              $"bảng lồng tối đa {_templateSettings.MaxTableNestingLevel} cấp";

                return BadRequest(new {
                    success = false,
                    message = $"Không thể mapping vào bảng lồng quá sâu. Hệ thống chỉ cho phép mapping vào {nestingLevelText}. Các trường sau vi phạm quy tắc: {string.Join(", ", fieldNames)}."
                });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation("Transaction started.");

            try
            {
                var template = await _context.Templates.Include(t => t.TemplateFields).FirstOrDefaultAsync(t => t.TemplateID == request.TemplateId);
                if (template == null)
                {
                    _logger.LogWarning("Template with ID {TemplateId} not found during save.", request.TemplateId);
                    return NotFound(new { success = false, message = "Không tìm thấy template." });
                }

                // SỬA ĐỔI: Gọi service để đọc file an toàn
                var docxBytes = await _templateStorageService.GetFileBytesAsync(template.OriginalDocxFilePath);
                if (docxBytes == null)
                {
                     return StatusCode(500, new { success = false, message = "Không thể đọc file template gốc." });
                }
                byte[] mappedDocxBytes;

                using (MemoryStream stream = new MemoryStream())
                {
                    await stream.WriteAsync(docxBytes);
                    using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(stream, true))
                    {
                        var body = wordDoc.MainDocumentPart?.Document?.Body;
                        if (body == null)
                        {
                            _logger.LogError("Không thể đọc nội dung tài liệu: MainDocumentPart hoặc Document hoặc Body là null");
                            return BadRequest(new { success = false, message = "Không thể đọc nội dung tài liệu" });
                        }
                        // *** THÊM DÒNG NÀY: Lấy style mặc định của văn bản ***
                        var defaultRunProperties = GetDefaultRunProperties(wordDoc.MainDocumentPart);

                        // *** LOGIC MỚI: SỬ DỤNG HTML TỪ DocxToStructuredHtmlService ĐỂ LẤY ELEMENT MAPPING ***
                        // Tạo HTML từ DOCX và parse để lấy element ID mapping chính xác
                        _logger.LogInformation("Creating element ID mapping using DocxToStructuredHtmlService HTML output...");

                        // Tạo HTML từ DOCX để lấy các element ID
                        //var htmlContent = _docxToHtmlService.ConvertToHtml(docxBytes, isViewMode: false);
                        // SỬA ĐỔI: Truyền giá trị từ settings.
                        var htmlContent = _docxToHtmlService.ConvertToHtml(docxBytes, _templateSettings.MaxTableNestingLevel, isViewMode: false);

                        // Parse HTML để lấy tất cả element ID và map với DOCX elements
                        var elementIdMap = CreateElementIdMapFromHtml(htmlContent, body);

                        _logger.LogInformation("Created {ElementCount} element mappings from HTML", elementIdMap.Count);

                        // Log một số ID để debug
                        var sampleIds = elementIdMap.Keys.Take(20).ToList();
                        _logger.LogInformation("Sample element IDs: {SampleIds}", string.Join(", ", sampleIds));

                        // Sắp xếp các vị trí cần chèn theo thứ tự ngược để không làm ảnh hưởng đến vị trí sau
                        var sortedInsertions = request.Fields
                            .SelectMany(f => f.Positions.Select(p => new { f.FieldName, Position = p }))
                            .Where(item => !string.IsNullOrEmpty(item.Position.ElementId)) // Chỉ xử lý positions có ElementId
                            .OrderByDescending(item => item.Position.CharOffset) // Sắp xếp theo offset để chèn từ cuối lên đầu
                            .ToList();

                        _logger.LogInformation("Processing {Count} placeholder insertions using ElementId mapping.", sortedInsertions.Count);

                        foreach (var item in sortedInsertions)
                        {
                            var pos = item.Position;
                            _logger.LogInformation("Processing field: {FieldName} with ElementId: {ElementId}", item.FieldName, pos.ElementId);

                            // Tìm element dựa trên ElementId
                            if (!elementIdMap.TryGetValue(pos.ElementId!, out var targetElement))
                            {
                                _logger.LogWarning("Cannot find element with ID: {ElementId} in elementIdMap. Available IDs: {AvailableIds}",
                                    pos.ElementId, string.Join(", ", elementIdMap.Keys.Take(10)));
                                continue;
                            }

                            _logger.LogInformation("Found target element: {ElementType} for ElementId: {ElementId}",
                                targetElement.GetType().Name, pos.ElementId);

                            // *** LOGIC MỚI: XỬ LÝ MERGE TEXT TỪ NHIỀU RUN ĐỂ MAPPING CHÍNH XÁC ***
                            //var insertResult = InsertPlaceholderWithMergedText(targetElement, pos.CharOffset, item.FieldName);
                            // *** SỬA DÒNG NÀY: Truyền style mặc định vào hàm chèn placeholder ***
                            var insertResult = InsertPlaceholderWithMergedText(targetElement, pos.CharOffset, item.FieldName, defaultRunProperties);

                            if (insertResult.Success)
                            {
                                _logger.LogInformation("Successfully inserted placeholder '{FieldName}' at position {Position}. Result: '{ResultText}'",
                                    item.FieldName, insertResult.InsertPosition, insertResult.ResultText);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to insert placeholder '{FieldName}': {ErrorMessage}",
                                    item.FieldName, insertResult.ErrorMessage);
                            }
                        }
                        wordDoc?.MainDocumentPart?.Document?.Save();
                    }
                    mappedDocxBytes = stream.ToArray();
                }

                // Lưu file đã được mapped
                //var fullOriginalPath = _templateStorageService.GetFullPath(template.OriginalDocxFilePath);
                var mappedFileName = Path.GetFileName(template.OriginalDocxFilePath)?.Replace("_original", "_mapped") ?? "";
                var mappedRelativePath = Path.Combine(Path.GetDirectoryName(template.OriginalDocxFilePath) ?? string.Empty, mappedFileName);
                var fullMappedPath = _templateStorageService.GetFullPath(mappedRelativePath);

                // Tạo thư mục nếu chưa tồn tại
                var directory = Path.GetDirectoryName(fullMappedPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Lưu file đã được mapped
                //await System.IO.File.WriteAllBytesAsync(fullMappedPath, mappedDocxBytes);
                // SỬA ĐỔI: Dùng service để lưu file thay vì System.IO
                await _templateStorageService.SaveFileAsync(mappedDocxBytes, mappedRelativePath);
                _logger.LogInformation("Mapped document saved to: {MappedPath}", fullMappedPath);

                // Cập nhật thông tin template
                template.MappedDocxFilePath = mappedRelativePath;
                template.Status = "Mapped";
                template.IsActive = true;  // Kích hoạt template
                template.LastModificationTimestamp = DateTime.Now;

                // Xóa các field cũ và thêm các field mới
                if (template.TemplateFields?.Count > 0)
                {
                    _context.TemplateFields.RemoveRange(template.TemplateFields);
                }

                foreach (var field in request.Fields)
                {
                    var templateField = new TemplateField
                    {
                        TemplateID = request.TemplateId,
                        FieldName = field.FieldName,
                        DisplayName = field.DisplayName,
                        DataType = field.DataType,
                        IsRequired = field.IsRequired,
                        DefaultValue = field.DefaultValue,
                        DisplayOrder = field.DisplayOrder,
                        Description = field.Description,
                        DataSourceType = field.DataSourceType,
                        CalculationFormula = field.CalculationFormula
                    };
                    _context.TemplateFields.Add(templateField);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully saved mapping and committed transaction for template ID: {TemplateId}", request.TemplateId);

                // 7. Trả về response thành công để client chuyển hướng
                TempData["SuccessMessage"] = $"Lưu ánh xạ thành công template ID: {template.TemplateID} - {template.TemplateName}";
                return Json(new { success = true, message = "Lưu ánh xạ thành công.",redirectUrl = Url.Action(nameof(Index)) });

                //return Ok(new { success = true, message = "Lưu mapping thành công."/*, redirectUrl = Url.Action(nameof(Index))*/ });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving mapping for template ID: {TemplateId}", request.TemplateId);
                return StatusCode(500, new { success = false, message = $"Đã xảy ra lỗi nghiêm trọng: {ex.Message}" });
            }
        }

        /// <summary>
        /// Lấy danh sách các trường có sẵn để mapping dưới dạng AvailableField
        /// </summary>
        private async Task<List<AvailableField>> GetAvailableFieldsAsync()
        {
            try
            {
                _logger?.LogInformation("Đang lấy danh sách các trường CIF có sẵn từ database");

                // Lấy danh sách các trường CIF từ database
                var cifFields = await _context.AvailableCifFields
                    .Where(f => f.IsActive)
                    .OrderBy(f => f.DisplayOrder)
                    .Select(f => new AvailableField
                    {
                        Name = f.FieldName,
                        DisplayName = f.DisplayName ?? f.FieldName,
                        FieldType = f.DataType ?? "TEXT",
                        //Description = f.Description,
                        DataSourceType = f.FieldTagPrefix
                    })
                    .ToListAsync();

                //// Kết hợp danh sách trường CIF và trường hệ thống
                //var allFields = cifFields.Concat(systemFields).ToList();
                var allFields = cifFields;

                return allFields;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Lỗi khi lấy danh sách trường CIF từ database");

                // Trả về danh sách mặc định nếu có lỗi
                return new List<AvailableField>
                {
                    new() { Name = "SoCif", DisplayName = "Số CIF", FieldType = "TEXT", DataSourceType = "CIF" },
                    new() { Name = "TenCif", DisplayName = "Tên khách hàng", FieldType = "TEXT", DataSourceType = "CIF" }
                };
            }
        }

        #endregion

        #region Quick Update Template

        /// <summary>
        /// Lấy thông tin template theo ID (dùng cho modal sửa nhanh)
        /// </summary>
        [HttpGet("GetTemplate/{id}")]
        public async Task<IActionResult> GetTemplateById(int id)
        {
            try
            {
                var template = await _context.Templates
                    .Include(t => t.BusinessOperation)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TemplateID == id);

                if (template == null)
                {
                    return Json(ApiResponse.Fail("Không tìm thấy template cần xem"));
                }

                return Json(ApiResponse.Ok("Lấy thông tin template thành công", new
                {
                    TemplateId = template.TemplateID,
                    BusinessOperationId = template.BusinessOperationID,
                    BusinessOperationName = template.BusinessOperation?.OperationName ?? string.Empty,
                    TemplateName = template.TemplateName ?? string.Empty,
                    Description = template.Description ?? string.Empty,
                    IsActive = template.IsActive
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin template ID: {TemplateId}", id);
                return Json(ApiResponse.Fail("Đã xảy ra lỗi khi lấy thông tin template"));
            }
        }

        /// <summary>
        /// Cập nhật nhanh thông tin cơ bản của template
        /// </summary>
        [HttpPost("QuickUpdate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickUpdate([FromBody] QuickUpdateTemplateModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToDictionary(k => "ValidationError", v => new[] { v });
                    return Json(ApiResponse.Fail("Dữ liệu không hợp lệ", errors));
                }

                var template = await _context.Templates.FindAsync(model.TemplateId);
                if (template == null)
                {
                    return Json(ApiResponse.Fail("Không tìm thấy template cần cập nhật"));
                }

                // Cập nhật thông tin người sửa cuối cùng
                var currentUser = await _userManager.GetUserAsync(User);
                var currentUserName = currentUser?.UserName ?? "System";
                var now = DateTime.Now;

                // Cập nhật các trường được phép sửa
                template.TemplateName = model.TemplateName;
                template.Description = model.Description;
                template.IsActive = model.IsActive;

                template.LastModificationTimestamp = now;
                template.LastModifiedByUserName = currentUserName;

                _context.Templates.Update(template);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đã cập nhật nhanh thông tin template ID: {TemplateId}", model.TemplateId);

                return Json(ApiResponse.Ok($"Đã cập nhật nhanh thông tin template ID: {model.TemplateId}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật nhanh thông tin template");
                return Json(ApiResponse.Fail("Đã xảy ra lỗi khi cập nhật thông tin template"));
            }
        }

        #endregion

        #region Mapped Docx Info

        private static readonly Regex PlaceholderRegex = new(@"<<(?<name>[A-Za-z0-9_]+)>>", RegexOptions.Compiled);

        /// <summary>
        /// Trả về thông tin file mapped docx: tồn tại hay không, HTML preview và danh sách placeholders
        /// </summary>
        [HttpGet("GetMappedInfo/{id}")]
        public async Task<IActionResult> GetMappedInfo(int id)
        {
            try
            {
                var template = await _context.Templates.AsNoTracking().FirstOrDefaultAsync(t => t.TemplateID == id);
                if (template == null)
                    return Json(ApiResponse.Fail("Không tìm thấy template"));

                if (string.IsNullOrWhiteSpace(template.MappedDocxFilePath))
                    return Json(ApiResponse.Ok("Template chưa có file mapped", new { exists = false }));

                // SỬA ĐỔI: Gọi service để đọc file an toàn
                byte[]? bytes = await _templateStorageService.GetFileBytesAsync(template.MappedDocxFilePath);
                if (bytes == null)
                {
                    return Json(ApiResponse.Ok("File mapped không tồn tại hoặc không thể đọc", new { exists = false }));
                }
                //var html = _docxToHtmlService.ConvertToHtml(bytes, isViewMode: false);
                var html = _docxToHtmlService.ConvertToHtml(bytes, _templateSettings.MaxTableNestingLevel, isViewMode: false);
                var placeholders = PlaceholderRegex.Matches(html).Select(m => m.Groups["name"].Value).Distinct().ToList();

                var fileUrl = Url.Action(nameof(GetTemplateFile), "Template", new { id, type = "mapped" });

                return Json(ApiResponse.Ok("Lấy thông tin mapped template thành công", new
                {
                    exists = true,
                    placeholders,
                    html,
                    fileUrl
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin mapped docx của template {TemplateId}", id);
                return Json(ApiResponse.Fail("Đã xảy ra lỗi khi lấy thông tin mapped template"));
            }
        }

        /// <summary>
        /// Kết quả chèn placeholder
        /// </summary>
        private class PlaceholderInsertResult
        {
            public bool Success { get; set; }
            public int InsertPosition { get; set; }
            public string ResultText { get; set; } = string.Empty;
            public string ErrorMessage { get; set; } = string.Empty;
        }

        /// <summary>
        /// Thông tin Run và text để merge
        /// </summary>
        private class RunTextInfo
        {
            public Run Run { get; set; } = null!;
            public Text TextElement { get; set; } = null!;
            public string Content { get; set; } = string.Empty;
            public int StartOffset { get; set; }
            public int EndOffset { get; set; }
        }

        /// <summary>
        /// Chèn placeholder với thuật toán merge text từ nhiều Run
        /// </summary>
        private PlaceholderInsertResult InsertPlaceholderWithMergedText(OpenXmlElement targetElement, int charOffset, string fieldName, RunProperties? defaultRunProperties)
        {
            try
            {
                // Xử lý Paragraph - merge text từ tất cả Run
                if (targetElement is Paragraph paragraph)
                {
                    return InsertInParagraphWithMerge(paragraph, charOffset, fieldName, defaultRunProperties);
                }

                // Xử lý Run đơn lẻ
                if (targetElement is Run run)
                {
                    // Run đơn lẻ đã có style, không cần áp dụng style mặc định
                    return InsertInSingleRun(run, charOffset, fieldName);
                }

                return new PlaceholderInsertResult
                {
                    Success = false,
                    ErrorMessage = $"Unsupported element type: {targetElement.GetType().Name}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting placeholder in element");
                return new PlaceholderInsertResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Chèn placeholder vào Paragraph bằng cách merge text từ tất cả Run
        /// </summary>
        private PlaceholderInsertResult InsertInParagraphWithMerge(Paragraph paragraph, int charOffset, string fieldName, RunProperties? defaultRunProperties)
        {
            // Bước 1: Thu thập tất cả Run và text
            var runInfos = CollectRunTextInfo(paragraph);
            if (runInfos.Count == 0)
            {
                // Tạo Run mới nếu paragraph trống
                var newRun = new Run();

                // *** THAY ĐỔI QUAN TRỌNG: Áp dụng style mặc định cho Run mới ***
                if (defaultRunProperties != null)
                {
                    newRun.PrependChild((RunProperties)defaultRunProperties.CloneNode(true));
                }

                var newTextElement = new Text($"<<{fieldName}>>");
                newRun.Append(newTextElement);
                paragraph.Append(newRun);

                return new PlaceholderInsertResult
                {
                    Success = true,
                    InsertPosition = 0,
                    ResultText = $"<<{fieldName}>>"
                };
            }

            // Bước 2: Merge text để tính toán vị trí chính xác
            var mergedText = string.Join("", runInfos.Select(r => r.Content));
            var safeOffset = Math.Min(charOffset, mergedText.Length);

            _logger.LogDebug("Merged text: '{MergedText}', Target offset: {Offset}", mergedText, safeOffset);

            // Bước 3: Tìm Run chứa vị trí cần chèn
            var targetRunInfo = FindTargetRunInfo(runInfos, safeOffset);
            if (targetRunInfo == null)
            {
                return new PlaceholderInsertResult
                {
                    Success = false,
                    ErrorMessage = "Cannot find target run for insertion"
                };
            }

            // Bước 4: Tính toán vị trí chèn trong Run cụ thể
            var relativeOffset = safeOffset - targetRunInfo.StartOffset;
            var insertPosition = Math.Min(relativeOffset, targetRunInfo.Content.Length);

            // Bước 5: Chèn placeholder bằng cách tạo Run riêng biệt để tránh bị tách rời
            var originalText = targetRunInfo.TextElement.Text;
            var placeholderText = $"<<{fieldName}>>";

            // Tách text thành 3 phần: trước, placeholder, sau
            var beforeText = originalText.Substring(0, insertPosition);
            var afterText = originalText.Substring(insertPosition);

            // Cập nhật text element hiện tại chỉ chứa phần trước
            targetRunInfo.TextElement.Text = beforeText;

            // Tạo Run mới chỉ chứa placeholder
            var placeholderRun = new Run();

            // Áp dụng style từ run gốc hoặc style mặc định
            var sourceRunProperties = targetRunInfo.Run.GetFirstChild<RunProperties>();
            if (sourceRunProperties != null)
            {
                placeholderRun.PrependChild((RunProperties)sourceRunProperties.CloneNode(true));
            }
            else if (defaultRunProperties != null)
            {
                placeholderRun.PrependChild((RunProperties)defaultRunProperties.CloneNode(true));
            }

            var placeholderTextElement = new Text(placeholderText);
            placeholderRun.Append(placeholderTextElement);

            // Chèn placeholder run sau run hiện tại
            targetRunInfo.Run.InsertAfterSelf(placeholderRun);

            // Nếu có text sau, tạo run mới cho phần sau
            if (!string.IsNullOrEmpty(afterText))
            {
                var afterRun = new Run();

                // Áp dụng style tương tự
                if (sourceRunProperties != null)
                {
                    afterRun.PrependChild((RunProperties)sourceRunProperties.CloneNode(true));
                }
                else if (defaultRunProperties != null)
                {
                    afterRun.PrependChild((RunProperties)defaultRunProperties.CloneNode(true));
                }

                var afterTextElement = new Text(afterText);
                afterRun.Append(afterTextElement);

                // Chèn after run sau placeholder run
                placeholderRun.InsertAfterSelf(afterRun);
            }

            _logger.LogInformation("Successfully created dedicated run for placeholder: {PlaceholderText}", placeholderText);

            return new PlaceholderInsertResult
            {
                Success = true,
                InsertPosition = safeOffset,
                ResultText = $"{beforeText}{placeholderText}{afterText}"
            };
        }

        /// <summary>
        /// Thu thập thông tin tất cả Run và text trong Paragraph
        /// </summary>
        private static List<RunTextInfo> CollectRunTextInfo(Paragraph paragraph)
        {
            var runInfos = new List<RunTextInfo>();
            int currentOffset = 0;

            foreach (var run in paragraph.Elements<Run>())
            {
                var textElement = run.Elements<Text>().FirstOrDefault();
                if (textElement != null)
                {
                    var content = textElement.Text ?? "";
                    runInfos.Add(new RunTextInfo
                    {
                        Run = run,
                        TextElement = textElement,
                        Content = content,
                        StartOffset = currentOffset,
                        EndOffset = currentOffset + content.Length
                    });
                    currentOffset += content.Length;
                }
            }

            return runInfos;
        }

        /// <summary>
        /// Tìm Run chứa vị trí cần chèn
        /// </summary>
        private static RunTextInfo? FindTargetRunInfo(List<RunTextInfo> runInfos, int targetOffset)
        {
            foreach (var runInfo in runInfos)
            {
                if (targetOffset >= runInfo.StartOffset && targetOffset <= runInfo.EndOffset)
                {
                    return runInfo;
                }
            }

            // Fallback: Run cuối cùng
            return runInfos.LastOrDefault();
        }

        /// <summary>
        /// Chèn placeholder vào Run đơn lẻ
        /// </summary>
        private static PlaceholderInsertResult InsertInSingleRun(Run run, int charOffset, string fieldName)
        {
            var textElement = run.Elements<Text>().FirstOrDefault();
            if (textElement == null)
            {
                // Tạo Text mới nếu Run trống
                var newTextElement = new Text($"<<{fieldName}>>");
                run.Append(newTextElement);

                return new PlaceholderInsertResult
                {
                    Success = true,
                    InsertPosition = 0,
                    ResultText = $"<<{fieldName}>>"
                };
            }

            var originalText = textElement.Text ?? "";
            var safeOffset = Math.Min(charOffset, originalText.Length);
            var newTextContent = originalText.Insert(safeOffset, $"<<{fieldName}>>");
            textElement.Text = newTextContent;

            return new PlaceholderInsertResult
            {
                Success = true,
                InsertPosition = safeOffset,
                ResultText = newTextContent
            };
        }

        /// <summary>
        /// Tìm vị trí chính xác để chèn placeholder - đồng bộ với frontend
        /// </summary>
        private int FindAccurateInsertPosition(string docxText, int frontendOffset, OpenXmlElement targetElement)
        {
            if (string.IsNullOrEmpty(docxText)) return 0;

            // Bước 1: Đảm bảo offset nằm trong giới hạn an toàn
            var safeOffset = Math.Min(frontendOffset, docxText.Length);

            _logger.LogDebug("Analyzing text for accurate position. Text: '{Text}', Frontend offset: {FrontendOffset}, Safe offset: {SafeOffset}",
                docxText, frontendOffset, safeOffset);

            // Bước 2: Kiểm tra xem vị trí frontend có hợp lý không
            if (IsValidInsertPosition(docxText, safeOffset))
            {
                _logger.LogDebug("Frontend offset {Offset} is valid, using it directly", safeOffset);
                return safeOffset;
            }

            // Bước 3: Tìm vị trí tốt nhất gần vị trí frontend
            var adjustedPosition = FindNearestValidPosition(docxText, safeOffset);

            _logger.LogDebug("Adjusted position from {OriginalOffset} to {AdjustedOffset}", safeOffset, adjustedPosition);
            return adjustedPosition;
        }

        /// <summary>
        /// Kiểm tra xem vị trí có hợp lý để chèn placeholder không
        /// </summary>
        private static bool IsValidInsertPosition(string text, int position)
        {
            if (position <= 0 || position >= text.Length) return position == 0 || position == text.Length;

            // Không chèn giữa các ký tự chữ (tránh tách từ)
            var prevChar = text[position - 1];
            var nextChar = text[position];

            // Nếu cả 2 bên đều là chữ/số thì không hợp lý
            if (char.IsLetterOrDigit(prevChar) && char.IsLetterOrDigit(nextChar))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tìm vị trí hợp lý gần nhất
        /// </summary>
        private static int FindNearestValidPosition(string text, int targetPosition)
        {
            // Tìm về phía sau trước
            for (int i = targetPosition; i <= text.Length; i++)
            {
                if (IsValidInsertPosition(text, i))
                {
                    return i;
                }
            }

            // Tìm về phía trước
            for (int i = targetPosition - 1; i >= 0; i--)
            {
                if (IsValidInsertPosition(text, i))
                {
                    return i;
                }
            }

            // Fallback: cuối text
            return text.Length;
        }

        /// <summary>
        /// Tạo element ID mapping bằng cách parse HTML từ DocxToStructuredHtmlService
        /// </summary>
        private Dictionary<string, OpenXmlElement> CreateElementIdMapFromHtml(string htmlContent, Body body)
        {
            var elementIdMap = new Dictionary<string, OpenXmlElement>();

            try
            {
                // Sử dụng regex để lấy tất cả data-element-id từ HTML
                var elementIdRegex = new Regex(@"data-element-id=""([^""]+)""", RegexOptions.Compiled);
                var matches = elementIdRegex.Matches(htmlContent);

                if (matches.Count == 0) return elementIdMap;

                // Tạo danh sách tất cả DOCX elements theo thứ tự
                var allParagraphs = body.Descendants<Paragraph>().ToList();
                var allRuns = body.Descendants<Run>().ToList();
                var allTables = body.Descendants<Table>().ToList();
                var allTableCells = body.Descendants<TableCell>().ToList();

                int pIndex = 0, rIndex = 0, tIndex = 0, tcIndex = 0;

                // Lấy danh sách các element ID theo thứ tự xuất hiện trong HTML
                var elementIds = matches.Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();

                foreach (var elementId in elementIds)
                {
                    if (string.IsNullOrEmpty(elementId)) continue;

                    OpenXmlElement? docxElement = null;

                    // Map theo loại element
                    if (elementId.StartsWith("p-") && pIndex < allParagraphs.Count)
                    {
                        docxElement = allParagraphs[pIndex++];
                    }
                    else if (elementId.StartsWith("r-") && rIndex < allRuns.Count)
                    {
                        docxElement = allRuns[rIndex++];
                    }
                    else if (elementId.StartsWith("t-") && tIndex < allTables.Count)
                    {
                        docxElement = allTables[tIndex++];
                    }
                    else if (elementId.StartsWith("tc-") && tcIndex < allTableCells.Count)
                    {
                        docxElement = allTableCells[tcIndex++];
                    }

                    if (docxElement != null)
                    {
                        elementIdMap[elementId] = docxElement;
                    }
                }

                _logger.LogDebug("Mapped {ParagraphCount} paragraphs, {RunCount} runs, {TableCount} tables, {CellCount} cells",
                    pIndex, rIndex, tIndex, tcIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating element ID map from HTML");
            }

            return elementIdMap;
        }

        /// <summary>
        /// Lấy RunProperties mặc định từ document's styles part để áp dụng cho placeholder.
        /// </summary>
        private static RunProperties? GetDefaultRunProperties(MainDocumentPart mainPart)
        {
            // Ưu tiên lấy style từ DocDefaults
            var rPrDefaultContainer = mainPart.StyleDefinitionsPart?.Styles?.DocDefaults?.RunPropertiesDefault;
            if (rPrDefaultContainer != null)
            {
                var defaultRunProps = rPrDefaultContainer.GetFirstChild<RunProperties>();
                if (defaultRunProps != null)
                {
                    // Clone để tránh thay đổi style gốc
                    return (RunProperties)defaultRunProps.CloneNode(true);
                }
            }

            // Nếu không có, thử lấy từ style "Normal"
            var normalStyle = mainPart.StyleDefinitionsPart?.Styles?.Elements<Style>()
                .FirstOrDefault(s => s.StyleId == "Normal" && s.Type == StyleValues.Paragraph);
            var rPrNormal = normalStyle?.StyleRunProperties;
            if (rPrNormal != null)
            {
                // *** FIX: Tạo một RunProperties mới từ OuterXml của StyleRunProperties. ***
                // Đây là cách chuyển đổi chính xác giữa hai kiểu đối tượng liên quan nhưng khác biệt này.
                return new RunProperties(rPrNormal.CloneNode(true).OuterXml);
            }

            // Nếu không tìm thấy, trả về null
            return null;
        }

        #endregion
    }
}
