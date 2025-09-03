using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography; // [THÊM MỚI] Dùng cho việc hash
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
        //private readonly HtmlToDocxService _htmlToDocxService;
        //private readonly DocxPlaceholderMappingService _docxPlaceholderMappingService;
        private readonly DocxToStructuredHtmlService _docxToHtmlService;
        // [THÊM MỚI] Khai báo service chèn placeholder
        private readonly DocxPlaceholderInsertionService _docxPlaceholderService;

        public TemplateController(
            ILogger<TemplateController> logger,
            ApplicationDbContext context,
            IWebHostEnvironment hostingEnvironment,
            ITemplateStorageService templateStorageService,
            ICurrentUserAccessor currentUserAccessor,
            IOptions<TemplateSettings> templateSettings,
            UserManager<ApplicationUser> userManager,
            //HtmlToDocxService htmlToDocxService,
            //DocxPlaceholderMappingService docxPlaceholderMappingService
            DocxToStructuredHtmlService docxToHtmlService,
            DocxPlaceholderInsertionService docxPlaceholderService
            )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            _templateStorageService = templateStorageService ?? throw new ArgumentNullException(nameof(templateStorageService));
            _currentUser = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
            _templateSettings = templateSettings?.Value ?? throw new ArgumentNullException(nameof(templateSettings));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            //_htmlToDocxService = htmlToDocxService ?? throw new ArgumentNullException(nameof(htmlToDocxService));
            //_docxPlaceholderMappingService = docxPlaceholderMappingService ?? throw new ArgumentNullException(nameof(docxPlaceholderMappingService));
            _docxToHtmlService = docxToHtmlService ?? throw new ArgumentNullException(nameof(docxToHtmlService));
            _docxPlaceholderService = docxPlaceholderService ?? throw new ArgumentNullException(nameof(docxPlaceholderService));
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
                        htmlContent = _docxToHtmlService.ConvertToHtml(docxBytes, isViewMode: true);
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
            //var canMapping = isOwner && !string.Equals(template.Status, "Mapped", StringComparison.OrdinalIgnoreCase);
            var canMapping = isOwner; // Cho phép người tạo template ánh xạ (bao gồm cả template đã ánh xạ)

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
            //return isOwner && !string.Equals(template.Status, "Mapped", StringComparison.OrdinalIgnoreCase);

            // SỬA ĐỔI THEO PRD: Bỏ kiểm tra status "Mapped" để cho phép chỉnh sửa lại.
            return isOwner;
        }

        // [SỬA ĐỔI] Hợp nhất logic của MappingGet và Mapping (POST) vào một phương thức GET duy nhất
        // để đảm bảo logic tải trang mapping luôn nhất quán.
        //[HttpGet("Mapping/{id}")]
        [ValidateAntiForgeryToken]
        [HttpPost("Mapping/{id}")]
        public async Task<IActionResult> Mapping(int id)
        {
            var template = await _context.Templates
                .Include(t => t.TemplateFields) // Eager load các field đã có
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TemplateID == id);

            if (template == null) return NotFound();
            if (!CanCurrentUserMapping(template))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền ánh xạ template này.";
                return RedirectToAction(nameof(Index));
            }

            // [SỬA ĐỔI] Luôn tải file gốc để đảm bảo tính nhất quán khi re-mapping.
            // Việc chèn placeholder sẽ được thực hiện lại từ đầu dựa trên "dấu vân tay" đã lưu.
            var relativePathToLoad = template.OriginalDocxFilePath;
            if (string.IsNullOrEmpty(relativePathToLoad))
            {
                TempData["ErrorMessage"] = "Không tìm thấy tệp tin gốc của template.";
                return RedirectToAction(nameof(Index));
            }

            var docxBytes = await _templateStorageService.GetFileBytesAsync(relativePathToLoad);
            if (docxBytes == null)
            {
                TempData["ErrorMessage"] = "Không thể đọc tệp tin gốc của template.";
                return RedirectToAction(nameof(Index));
            }

            // Chuyển đổi DOCX sang HTML, có nhúng hash vào các paragraph
            string htmlContent = _docxToHtmlService.ConvertToHtml(docxBytes, isViewMode: false);

            // [NÂNG CẤP] Lấy TẤT CẢ các fingerprint từ các trường đã lưu và gộp lại thành một mảng duy nhất.
            var mappedFields = await _context.TemplateFields
                .Where(f => f.TemplateID == id && !string.IsNullOrEmpty(f.MappingPositionsJson))
                .AsNoTracking()
                .ToListAsync();

            var allFingerprints = new List<FieldPositionFingerprint>();
            foreach (var field in mappedFields)
            {
                try
                {
                    // Deserialize mảng fingerprint của từng trường
                    var fingerprintsForField = JsonConvert.DeserializeObject<List<FieldPositionFingerprint>>(field.MappingPositionsJson);
                    if (fingerprintsForField != null)
                    {
                        // Gộp vào danh sách chung
                        allFingerprints.AddRange(fingerprintsForField);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi deserialize MappingPositionsJson cho TemplateFieldID {TemplateFieldID}", field.TemplateFieldID);
                }
            }

            // // [SỬA ĐỔI] Lấy dữ liệu mapping đã lưu từ cột JSON và tạo DTO
            // var mappedFieldsData = template.TemplateFields?
            //     .Where(f => !string.IsNullOrEmpty(f.MappingPositionsJson))
            //     .Select(f => new FieldMappingInfo
            //     {
            //         FieldName = f.FieldName,
            //         DisplayName = f.DisplayName,
            //         DataType = f.DataType,
            //         IsRequired = f.IsRequired,
            //         DataSourceType = f.DataSourceType,
            //         DisplayOrder = f.DisplayOrder,
            //         // ... các thuộc tính khác của field
            //         Positions = JsonConvert.DeserializeObject<List<FieldPositionFingerprint>>(f.MappingPositionsJson ?? "[]") ?? new List<FieldPositionFingerprint>()
            //     })
            //     .ToList() ?? new List<FieldMappingInfo>();

            // Lấy danh sách các trường có sẵn
            var availableCifFields = await GetAvailableCifFieldsAsync();
            var savedTemplateFields = template.TemplateFields?
                .Select(f => new AvailableField
                {
                    Name = f.FieldName,
                    DisplayName = f.DisplayName ?? f.FieldName,
                    FieldType = f.DataType ?? "TEXT",
                    DataSourceType = f.DataSourceType ?? "FORM",
                    DisplayOrder = f.DisplayOrder
                }).ToList() ?? new List<AvailableField>();
            
            var allAvailableFields = availableCifFields
                .Concat(savedTemplateFields)
                .GroupBy(f => f.Name)
                .Select(g => new FieldViewModel
                {
                    Name = g.Key,
                    DisplayName = g.First().DisplayName ?? g.Key,
                    DataType = g.First().FieldType ?? "TEXT",
                    DataSourceType = g.First().DataSourceType ?? "CIF",
                    DisplayOrder = g.First().DisplayOrder ?? 0
                })
                .OrderBy(f => f.DisplayOrder)
                .ThenBy(f => f.DisplayName ?? f.Name)
                .ToList();

            var viewModel = new TemplateMappingViewModel
            {
                TemplateId = template.TemplateID,
                TemplateName = template.TemplateName,
                StructuredHtmlContent = htmlContent,
                AvailableFields = allAvailableFields.Select(f => new FieldViewModel
                {
                    Name = f.Name,
                    DisplayName = f.DisplayName,
                    DataType = f.DataType ?? "TEXT",
                    DataSourceType = f.DataSourceType ?? "INPUT",
                    DisplayOrder = f.DisplayOrder 
                }).OrderBy(f => f.DisplayOrder).ThenBy(f => f.Name).ToList(),
                // [NÂNG CẤP] Truyền mảng fingerprint đã gộp xuống client
                MappedFieldsJson = JsonConvert.SerializeObject(allFingerprints, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                })
                
                //MappedFieldsJson = JsonConvert.SerializeObject(allFingerprints)
            };

            return View("Mapping", viewModel);
        }       

        /// <summary>
        /// [NÂNG CẤP] Action nhận và xử lý dữ liệu mapping từ client.
        /// Tích hợp đầy đủ với TemplateStorageService và kiến trúc "Dấu vân tay".
        /// </summary>
        [HttpPost("SaveMapping")]
        [ValidateAntiForgeryToken] // Bỏ đi khi dùng API-style với [FromBody]
        public async Task<IActionResult> SaveMapping([FromBody] SaveMappingRequest request)
        {
            //_logger.LogInformation("Bắt đầu lưu mapping (Hybrid Fingerprinting) cho template ID: {TemplateId}", request?.TemplateId);
            // if (request == null || request.Fields.Count == 0)
            // {
            //     return Json(new { success = false, message = "Không có dữ liệu mapping hợp lệ." });
            // }
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ.", errors });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var template = await _context.Templates.Include(t => t.TemplateFields).FirstOrDefaultAsync(t => t.TemplateID == request.TemplateId);
                if (template == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy template." });
                }

                // 1. Tạo file mapped.docx bằng cách chèn placeholder vào file origin.docx
                var originalDocxBytes = await _templateStorageService.GetFileBytesAsync(template.OriginalDocxFilePath);
                if (originalDocxBytes == null)
                {
                    return StatusCode(500, new { success = false, message = "Không thể đọc file template gốc." });
                }

                // byte[] mappedDocxBytes;
                // using (var memoryStream = new MemoryStream())
                // {
                //     await memoryStream.WriteAsync(originalDocxBytes);
                //     using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(memoryStream, true))
                //     {
                //         InsertPlaceholdersByFingerprint(wordDoc, request.Fields);
                //     }
                //     mappedDocxBytes = memoryStream.ToArray();
                // }

                // Gọi service mới để chèn placeholders
                var mappedDocxBytes = _docxPlaceholderService.InsertPlaceholders(originalDocxBytes, request.Fingerprints);

                // Lưu file mapped.docx
                var mappedFileName = Path.GetFileName(template.OriginalDocxFilePath)?.Replace("_original", "_mapped") ?? $"{template.TemplateID}_mapped.docx";
                var mappedRelativePath = Path.Combine(Path.GetDirectoryName(template.OriginalDocxFilePath) ?? string.Empty, mappedFileName).Replace('\\', '/');
                await _templateStorageService.SaveFileAsync(mappedDocxBytes, mappedRelativePath);
                //template.MappedDocxFilePath = mappedRelativePath; // Cập nhật đường dẫn trong DB
                
                _logger.LogInformation("Đã lưu file mapped.docx vào: {mappedRelativePath}", mappedRelativePath);

                // --- BƯỚC 2: CẬP NHẬT DATABASE ---

                var availableCifFields = await GetAvailableCifFieldsAsync();
                
                var savedTemplateFields = template.TemplateFields?
                    .Select(f => new AvailableField
                    {
                        Name = f.FieldName,
                        DisplayName = f.DisplayName ?? f.FieldName,
                        FieldType = f.DataType ?? "TEXT",
                        DataSourceType = f.DataSourceType ?? "FORM",
                        DisplayOrder = f.DisplayOrder
                    }).ToList() ?? new List<AvailableField>();
                
                // var allAvailableFieldsLookup = availableCifFields
                //     .Concat(savedTemplateFields)
                //     .GroupBy(f => f.Name)
                //     .Select(g => g.First())
                //     .ToList();

                    var allAvailableFieldsLookup = availableCifFields
                    .Select(f => new { f.Name, f.DisplayName, f.FieldType, f.DataSourceType })
                    .Concat(savedTemplateFields.Select(f => new { f.Name, f.DisplayName, f.FieldType, f.DataSourceType }))
                    .GroupBy(f => f.Name)
                    .ToDictionary(g => g.Key, g => g.First());

                // Xóa tất cả các trường cũ của template này để đồng bộ lại
                var oldFields = _context.TemplateFields.Where(f => f.TemplateID == request.TemplateId);
                _context.TemplateFields.RemoveRange(oldFields);
                //await _context.SaveChangesAsync(); // Lưu thay đổi xóa trước khi thêm mới

                // // Log dữ liệu nhận được từ client
                // _logger.LogInformation("Dữ liệu nhận được từ client - Số trường: {FieldCount}", request.Fields?.Count);
                // if (request.Fields != null)
                // {
                //     foreach (var field in request.Fields)
                //     {
                //         _logger.LogInformation("Field: {Name}, DataType: {DataType}, DataSourceType: {DataSourceType}", 
                //             field.Name, field.DataType, field.DataSourceType);
                //     }
                // }

                // Nhóm các fingerprint theo tên trường
                var groupedFingerprints = request.Fingerprints.GroupBy(f => f.FieldName);

                foreach (var group in groupedFingerprints)
                {
                    var fieldName = group.Key;
                    var fieldFingerprints = group.ToList();

                    // Tra cứu thông tin trường từ danh sách đã tạo
                    var availableFieldInfo = allAvailableFieldsLookup.GetValueOrDefault(fieldName);
                    // _logger.LogInformation("Xử lý trường: {FieldName}", fieldName);
                    // _logger.LogInformation("Thông tin từ availableFieldInfo: {Info}", 
                    //     availableFieldInfo != null ? $"DisplayName={availableFieldInfo.DisplayName}, FieldType={availableFieldInfo.FieldType}, DataSourceType={availableFieldInfo.DataSourceType}" : "null");

                    // Lấy thông tin DataType và DataSourceType từ request nếu có, nếu không thì lấy từ availableFieldInfo
                    var fieldInfo = request.Fields?.FirstOrDefault(f => f.Name == fieldName);
                    // _logger.LogInformation("Thông tin từ fieldInfo: {Info}", 
                    //     fieldInfo != null ? $"DataType={fieldInfo.DataType}, DataSourceType={fieldInfo.DataSourceType}" : "null");
                    
                    var dataType = fieldInfo?.DataType ?? availableFieldInfo?.FieldType ?? "TEXT";
                    var dataSourceType = fieldInfo?.DataSourceType ?? availableFieldInfo?.DataSourceType ?? "FORM";
                    
                    // _logger.LogInformation("Giá trị cuối cùng - DataType: {DataType}, DataSourceType: {DataSourceType}", 
                    //     dataType, dataSourceType);
                    
                    var newField = new TemplateField
                    {
                        TemplateID = request.TemplateId,
                        FieldName = fieldName,
                        //DisplayName = availableFieldInfo?.DisplayName ?? fieldName,
                        // [SỬA LỖI] Ưu tiên DisplayName từ request của client, sau đó mới fallback về thông tin cũ hoặc FieldName.
                        DisplayName = fieldInfo?.DisplayName ?? availableFieldInfo?.DisplayName ?? fieldName,
                        DataType = dataType,
                        DataSourceType = dataSourceType,
                        MappingPositionsJson = JsonConvert.SerializeObject(fieldFingerprints),
                        // Cập nhật ngày cập nhật
                        // NgayCapNhat = DateTime.Now
                    };
                    
                    // _logger.LogInformation("Tạo mới TemplateField: {FieldName}, DataType: {DataType}, DataSourceType: {DataSourceType}", 
                    //     newField.FieldName, newField.DataType, newField.DataSourceType);
                    _context.TemplateFields.Add(newField);
                }

                // Cập nhật trạng thái template
                template.MappedDocxFilePath = mappedRelativePath;
                template.Status = TemplateStatus.Mapped.ToString();
                template.IsActive = true;
                template.LastModificationTimestamp = DateTime.Now;
                template.LastModifiedByUserName = _currentUser.UserName;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Lưu mapping và commit transaction thành công cho template ID: {TemplateId}", request.TemplateId);
                //TempData["SuccessMessage"] = $"Lưu ánh xạ thành công cho template '{template.TemplateName}'";
                //return Json(new { success = true, message = "Lưu ánh xạ thành công.", redirectUrl = Url.Action(nameof(Index)) });
                return Json(new { success = true, message = "Lưu ánh xạ thành công." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi lưu mapping cho template ID: {TemplateId}", request.TemplateId);
                return StatusCode(500, new { success = false, message = $"Đã xảy ra lỗi nghiêm trọng: {ex.Message}" });
            }
        }
      
        /// <summary>
        /// Lấy danh sách các trường CIF có sẵn để mapping
        /// </summary>
        private async Task<List<AvailableField>> GetAvailableCifFieldsAsync()
        {
            try
            {
                _logger?.LogInformation("Đang lấy danh sách các trường CIF có sẵn từ database");

                var cifFields = await _context.AvailableCifFields
                    .Where(f => f.IsActive)
                    .OrderBy(f => f.DisplayOrder)
                    .Select(f => new AvailableField
                    {
                        Name = f.FieldName,
                        DisplayName = f.DisplayName ?? f.FieldName,
                        FieldType = f.DataType ?? "TEXT",
                        DataSourceType = "CIF", // Gán cứng là CIF
                        DisplayOrder = f.DisplayOrder
                    })
                    .ToListAsync();

                return cifFields;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Lỗi khi lấy danh sách trường CIF từ database");
                return new List<AvailableField>(); // Trả về danh sách rỗng nếu có lỗi
            }
        }
        // === KẾT THÚC BỔ SUNG ===

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
                var html = _docxToHtmlService.ConvertToHtml(bytes, isViewMode: false);
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

        #endregion
    }
}
