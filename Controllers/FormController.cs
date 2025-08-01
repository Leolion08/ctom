using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using CTOM.Data;
using CTOM.Services;
using CTOM.Services.Interfaces;
using CTOM.ViewModels.Form;
using Microsoft.Extensions.Options;
using System.Text.Json;
using CTOM.Models.DTOs;
using CTOM.Models.Settings;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Dynamic.Core;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading.Tasks; // Đảm bảo có using này
using CTOM.Models.Entities; // Đảm bảo có using này
using System.Linq; // Đảm bảo có using này
using System; // Đảm bảo có using này
using System.Collections.Generic; // Đảm bảo có using này
using System.IO; // Đảm bảo có using này

namespace CTOM.Controllers;

/// <summary>
/// Controller hiển thị danh sách giao dịch nhập dữ liệu cho Form động (F03).
/// </summary>
[Authorize(Roles = "ADMIN,HTTD")]
[Route("[controller]")]
public sealed class FormController(
        ILogger<FormController> logger,
        IFormDataService formDataService,
        ApplicationDbContext dbContext,
        IOptions<JsonSerializerOptions> jsonOptionsAccessor,
        ICurrentUserAccessor currentUserAccessor,
        DocxToStructuredHtmlService docxToHtmlService,
        IOptions<TemplateSettings> templateSettings,
        ITemplateStorageService templateStorageService) : Controller
{
    private readonly ILogger<FormController> _logger = logger;
    private readonly IFormDataService _formDataService = formDataService;
    private readonly ApplicationDbContext _db = dbContext;
    private readonly JsonSerializerOptions _jsonOptions = jsonOptionsAccessor.Value;
    private readonly ICurrentUserAccessor _currentUser = currentUserAccessor;
    private readonly DocxToStructuredHtmlService _docxToHtmlService = docxToHtmlService;
    private readonly TemplateSettings _templateSettings = templateSettings.Value;
    private readonly ITemplateStorageService _templateStorageService = templateStorageService;

    /// <summary>
    /// GET /Form/Create – hiển thị bước 1 (chọn template + ghi chú)
    /// </summary>
    [HttpGet("Create")]
    public async Task<IActionResult> Create(int? templateId = null)
    {
        var templateOptions = await _formDataService.GetAvailableTemplateOptionsAsync();
        ViewData["TemplateOptions"] = templateOptions;

        // Nếu có templateId, tạo preview HTML
        if (templateId.HasValue)
        {
            var template = await _db.Templates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TemplateID == templateId && t.IsActive);

            if (template != null)
            {
                var (htmlContent, hasFile) = await GenerateHtmlPreviewAsync(template);
                var vm = new FormCreateVM
                {
                    TemplateId = template.TemplateID,
                    TemplateName = template.TemplateName,
                    HtmlContent = htmlContent,
                    HasFile = hasFile
                };
                return View(vm);
            }
        }

        return View(new FormCreateVM());
    }

    /// <summary>
    /// POST Step1 – nhận TemplateId + Note, trả về JSON danh sách trường.
    /// </summary>
    [HttpPost("Create/Step1")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStep1([FromForm] FormCreateVM vm)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            // Validate TemplateId tồn tại
            bool templateExists = await _db.Templates.AnyAsync(t => t.TemplateID == vm.TemplateId && t.IsActive);
            if (!templateExists)
                return BadRequest("Template không tồn tại hoặc đã bị khóa.");

            var entityFields = await _db.TemplateFields
                .AsNoTracking()
                .Where(f => f.TemplateID == vm.TemplateId)
                .OrderBy(f => f.DisplayOrder ?? int.MaxValue)
                .ToListAsync();

            var fields = entityFields.Select(f => new TemplateFieldDto(
                    f.FieldName,
                    string.IsNullOrWhiteSpace(f.DisplayName) ? f.FieldName : f.DisplayName,
                    (f.DataType ?? "TEXT").ToUpperInvariant(),
                    f.IsRequired,
                    f.DataSourceType)).ToList();

            // Ưu tiên SoCif đầu danh sách
            fields = fields.OrderBy(f => f.FieldName != "SoCif").ToList();

            // Chuyển hướng sang trang Create với templateId để hiển thị HTML preview
            return Json(new { success = true, fields, redirectUrl = Url.Action("Create", new { templateId = vm.TemplateId }) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateStep1");
            return StatusCode(500, "Có lỗi xảy ra");
        }
    }

    /// <summary>
    /// POST: Lấy danh sách trường của Template – dùng cho trang Edit.
    /// </summary>
    [HttpPost("GetTemplateFields/{templateId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetTemplateFields(int templateId)
    {
        return await GetTemplateFieldsInternal(templateId);
    }

    [HttpGet("GetTemplateFields/{templateId}")]
    public async Task<IActionResult> GetTemplateFieldsGet(int templateId) => await GetTemplateFieldsInternal(templateId);

    private async Task<IActionResult> GetTemplateFieldsInternal(int templateId)
    {
        try
        {
            bool templateExists = await _db.Templates.AnyAsync(t => t.TemplateID == templateId && t.IsActive);
            if (!templateExists)
                return Json(new { success = false, message = "Template không tồn tại hoặc đã bị khóa." });

            var entityFields = await _db.TemplateFields
                .AsNoTracking()
                .Where(f => f.TemplateID == templateId)
                .OrderBy(f => f.DisplayOrder ?? int.MaxValue)
                .ToListAsync();

            var fields = entityFields.Select(f => new {
                    fieldName = f.FieldName,
                    displayName = string.IsNullOrWhiteSpace(f.DisplayName) ? f.FieldName : f.DisplayName,
                    dataType = (f.DataType ?? "TEXT").ToUpperInvariant(),
                    isRequired = f.IsRequired,
                    dataSourceType = f.DataSourceType
                })
                .OrderBy(f => f.fieldName != "SoCif")
                .ToList();

            return Json(fields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetTemplateFields");
            return StatusCode(500, "Có lỗi xảy ra");
        }
    }

    /// <summary>
    /// POST Create – lưu FormData (step2 submit).
    /// </summary>
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FormCreateStep2VM vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu chưa hợp lệ, vui lòng kiểm tra lại.";
            return RedirectToAction(nameof(Create));
        }

        try
        {
            var id = await _formDataService.CreateAsync(vm);
            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return Json(new { success = true, id });
            }
            TempData["SuccessMessage"] = $"Giao dịch đã được tạo thành công (ID: {id}). Bây giờ bạn có thể chỉnh sửa và xem kết quả.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi lưu FormData");
            TempData["ErrorMessage"] = "Có lỗi khi lưu form, vui lòng thử lại.";
            return RedirectToAction(nameof(Create));
        }
    }

    // GET: /Form/Edit/5
    // SỬA LỖI: Bổ sung logic tạo HTML preview cho trang Edit
    [HttpGet("Edit/{id:long}")]
    public async Task<IActionResult> Edit(long id)
    {
        var formData = await _db.FormDatas
            .Include(f => f.Template)
                .ThenInclude(t => t!.BusinessOperation)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.FormDataID == id);

        if (formData == null)
        {
            return NotFound();
        }

        var template = formData.Template;
        if (template == null)
        {
            // Xử lý trường hợp template không tồn tại hoặc không được load
            TempData["ErrorMessage"] = "Không tìm thấy template liên kết với giao dịch này.";
            return RedirectToAction(nameof(Index));
        }

        // --- LOGIC MỚI ĐỂ TẠO HTML PREVIEW ---
        var (htmlContent, hasFile) = await GenerateHtmlPreviewAsync(template);
        // ------------------------------------

        string fullTemplateName = "Không xác định";
        var operationName = template.BusinessOperation?.OperationName ?? "N/A";
        fullTemplateName = $"{operationName} > {template.TemplateName} (ID: {template.TemplateID})";

        var viewModel = new FormEditViewModel
        {
            FormDataID = formData.FormDataID,
            TemplateId = formData.TemplateID,
            Note = formData.Note,
            TemplateName = fullTemplateName,
            FormDataJson = formData.FormDataJson,
            HtmlContent = htmlContent, // Truyền HTML sang View
            HasFile = hasFile          // Truyền cờ có file sang View
        };

        if (TempData["SuccessMessage"] is not null)
            ViewData["SuccessMessage"] = TempData["SuccessMessage"];

        return View(viewModel);
    }

    // GET: /Form/Index – danh sách form
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Danh sách form đã tạo";
        return View();
    }

    /// <summary>
    /// Lấy dữ liệu cho DataTables với logic chuẩn (lọc, sắp xếp, phân trang phía server).
    /// </summary>
    [HttpPost("GetDataTable")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetDataTable([FromForm] DataTablesRequest dtRequest, [FromForm(Name = "q")] string? customSearch)
    {
        try
        {
            var query = from f in _db.FormDatas.Include(f => f.Template).AsNoTracking()
                        join kh in _db.KhachHangDNs.AsNoTracking() on f.SoCif equals kh.SoCif into khGroup
                        from kh in khGroup.DefaultIfEmpty()
                        select new { f, kh };

            var currentUser = _currentUser.UserName.ToLowerInvariant();
            var currentDeptId = _currentUser.MaPhong;
            if (!string.IsNullOrWhiteSpace(currentUser) || !string.IsNullOrWhiteSpace(currentDeptId))
            {
                query = query.Where(x =>
                    (!string.IsNullOrWhiteSpace(currentUser) && x.f.CreatedByUserName.ToLower() == currentUser) ||
                    (!string.IsNullOrWhiteSpace(currentDeptId) && x.f.CreatedDepartmentID == currentDeptId));
            }

            int recordsTotal = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(customSearch))
            {
                var filter = customSearch.Trim();
                query = query.Where(x =>
                    (x.f.SoCif != null && EF.Functions.Like(x.f.SoCif, $"%{filter}%")) ||
                    (x.f.Template != null && x.f.Template.TemplateName != null && EF.Functions.Like(x.f.Template.TemplateName!, $"%{filter}%")));
            }

            int recordsFiltered = await query.CountAsync();

            var order = dtRequest.Order.FirstOrDefault();
            if (order != null && order.Column < dtRequest.Columns.Count)
            {
                var clientSortColumn = dtRequest.Columns[order.Column].Name;
                var sortDirection = order.Dir;
                var columnMapping = new Dictionary<string, string>
                {
                    { "formDataId", "f.FormDataID" },
                    { "templateName", "f.Template.TemplateName" },
                    { "note", "f.Note" },
                    { "soCif", "f.SoCif" },
                    { "createdDepartmentId", "f.CreatedDepartmentID" },
                    { "createdBy", "f.CreatedByUserName" },
                    { "updatedAt", "f.LastModificationTimestamp" }
                };

                if (!string.IsNullOrEmpty(clientSortColumn) && columnMapping.TryGetValue(clientSortColumn, out var serverSortColumn))
                {
                    query = query.OrderBy($"{serverSortColumn} {sortDirection}");
                }
                else
                {
                    query = query.OrderByDescending(x => x.f.LastModificationTimestamp ?? x.f.CreationTimestamp);
                }
            }
            else
            {
                query = query.OrderByDescending(x => x.f.LastModificationTimestamp ?? x.f.CreationTimestamp);
            }

            var pagedData = await query
                .Skip(dtRequest.Start)
                .Take(dtRequest.Length)
                .Select(x => new
                {
                    formDataId = x.f.FormDataID,
                    templateId = x.f.TemplateID,
                    templateName = x.f.Template != null ? x.f.Template.TemplateName : "(N/A)",
                    soCif = x.f.SoCif ?? string.Empty,
                    tenCif = x.kh != null ? x.kh.TenCif : null,
                    note = x.f.Note,
                    createdBy = x.f.CreatedByUserName,
                    createdDepartmentId = x.f.CreatedDepartmentID,
                    updatedAt = x.f.LastModificationTimestamp ?? x.f.CreationTimestamp,
                })
                .ToListAsync();

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
            _logger.LogError(ex, "Lỗi khi tải dữ liệu form cho DataTables.");
            return Json(new DataTablesResponse<object>
            {
                Draw = dtRequest.Draw,
                Error = "Có lỗi xảy ra phía máy chủ."
            });
        }
    }

    /**
     * PHẦN THAY ĐỔI: Cập nhật Action Details để sinh HTML preview
     */
    [HttpGet("Details/{id:long}")]
    public async Task<IActionResult> Details(long id)
    {
        var formData = await _db.FormDatas
            .Include(f => f.Template)
                .ThenInclude(t => t!.BusinessOperation)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.FormDataID == id);

        if (formData == null) return NotFound();

        var template = formData.Template;
        if (template == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy template liên kết với giao dịch này.";
            return RedirectToAction(nameof(Index));
        }

        // THAY ĐỔI: Gọi hàm sinh HTML
        var (htmlContent, hasFile) = await GenerateHtmlPreviewAsync(template);

        string fullTemplateName = "Không xác định";
        var operationName = template.BusinessOperation?.OperationName ?? "N/A";
        fullTemplateName = $"{operationName} > {template.TemplateName} (ID: {template.TemplateID})";

        // THAY ĐỔI: Truyền HTML vào ViewModel
        var vm = new FormEditViewModel
        {
            FormDataID = formData.FormDataID,
            TemplateId = formData.TemplateID,
            Note = formData.Note,
            TemplateName = fullTemplateName,
            FormDataJson = formData.FormDataJson,
            HtmlContent = htmlContent, // Gán nội dung HTML
            HasFile = hasFile          // Gán cờ có file
        };
        return View("Details", vm);
    }

    // POST: /Form/Edit/5
    [HttpPost("Edit/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, [FromForm] FormEditViewModel vm)
    {
        if (id != vm.FormDataID)
        {
            return Json(new { success = false, message = "ID không hợp lệ." });
        }

        try
        {
            await _formDataService.UpdateAsync(vm);
            return Json(new { success = true, message = "Cập nhật thành công!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật FormData ID {FormDataId}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    // POST: /Form/Delete/5
    [HttpPost("Delete/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            var form = await _db.FormDatas.FindAsync(id);
            if (form is null)
                return Json(new { success = false, message = "Không tìm thấy giao dịch cần xóa." });

            var currentUserName = _currentUser.UserName.ToLowerInvariant();
            if (form.CreatedByUserName?.ToLower() != currentUserName)
                return Json(new { success = false, message = "Bạn không có quyền xóa giao dịch này." });

            _db.FormDatas.Remove(form);
            await _db.SaveChangesAsync();
            _logger.LogInformation("User {User} deleted FormData {FormDataId}", currentUserName, id);
            return Json(new { success = true, message = "Đã xóa giao dịch thành công." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa FormData ID {FormDataId}", id);
            return Json(new { success = false, message = "Có lỗi xảy ra khi xóa giao dịch." });
        }
    }

    /// <summary>
    /// GET: /Form/GetHtmlPreview
    /// </summary>
    [HttpGet("GetHtmlPreview")]
    public async Task<IActionResult> GetHtmlPreview([FromQuery] int templateId)
    {
        try
        {
            var template = await _db.Templates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TemplateID == templateId && t.IsActive);

            if (template == null)
            {
                return Json(new { success = false, message = $"Không tìm thấy template có ID: {templateId}" });
            }

            var (htmlContent, hasFile) = await GenerateHtmlPreviewAsync(template);
            return Json(new { success = true, htmlContent, hasFile });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting DOCX to HTML for template {TemplateId}", templateId);
            return Json(new { success = false, message = $"Lỗi khi tạo HTML preview: {ex.Message}" });
        }
    }

    /// <summary>
    /// GET: /Form/Copy/5
    /// </summary>
    [HttpGet("Copy/{id:long}")]
    public async Task<IActionResult> Copy(long id)
    {
        var originalForm = await _db.FormDatas
            .Include(f => f.Template)
                .ThenInclude(t => t!.BusinessOperation)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.FormDataID == id);

        if (originalForm == null)
        {
            return NotFound();
        }

        var template = originalForm.Template;
        if (template == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy template liên kết với giao dịch này.";
            return RedirectToAction(nameof(Index));
        }

        // THAY ĐỔI: Gọi hàm sinh HTML preview
        var (htmlContent, hasFile) = await GenerateHtmlPreviewAsync(template);

        string fullTemplateName = "Không xác định";
        var operationName = template.BusinessOperation?.OperationName ?? "N/A";
        fullTemplateName = $"{operationName} > {template.TemplateName} (ID: {template.TemplateID})";

        // THAY ĐỔI: Truyền HTML vào ViewModel
        var viewModel = new FormCopyViewModel
        {
            TemplateId = originalForm.TemplateID,
            TemplateName = fullTemplateName,
            Note = originalForm.Note,
            FormDataJson = originalForm.FormDataJson,
            HtmlContent = htmlContent, // Gán nội dung HTML
            HasFile = hasFile          // Gán cờ có file
        };

        return View("Copy", viewModel);
    }

    /// <summary>
    /// ACTION MỚI: Trả về HTML của một tài liệu đã được trộn dữ liệu.
    /// Dùng cho chức năng "Xem tài liệu đã lưu" trong modal.
    /// </summary>
    /**
     * PHẦN THAY ĐỔI: Sửa lại hoàn toàn logic thay thế placeholder để khắc phục lỗi lồng thẻ span.
     * Logic mới đảm bảo việc thay thế được thực hiện một cách rõ ràng và chính xác.
     */
    [HttpGet("GetMergedHtmlPreview/{id:long}")]
    public async Task<IActionResult> GetMergedHtmlPreview(long id)
    {
        try
        {
            var formData = await _db.FormDatas
                .Include(f => f.Template)
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FormDataID == id);

            if (formData?.Template == null)
            {
                return Json(new { success = false, message = "Không tìm thấy giao dịch hoặc template tương ứng." });
            }

            // 1. Lấy HTML gốc với các placeholder đã được chuyển thành span tương tác
            var (baseHtml, hasFile) = await GenerateHtmlPreviewAsync(formData.Template);

            if (!hasFile || string.IsNullOrEmpty(baseHtml))
            {
                return Json(new { success = false, message = "Template không có file để xem trước." });
            }

            var savedData = JsonSerializer.Deserialize<Dictionary<string, string?>>(formData.FormDataJson ?? "{}", _jsonOptions) ?? new();
            var fields = await _db.TemplateFields
                .AsNoTracking()
                .Where(f => f.TemplateID == formData.TemplateID)
                .ToListAsync();

            string finalHtml = baseHtml;

            // 2. Tạo một biểu thức chính quy (Regex) để tìm tất cả các span placeholder trong một lần
            var allPlaceholdersPattern = new Regex("<span class=\"placeholder-span[^\"]*\" data-placeholder-for=\"([^\"]+)\">.*?</span>", RegexOptions.IgnoreCase);

            // 3. Sử dụng MatchEvaluator để xử lý logic thay thế cho mỗi placeholder tìm được
            finalHtml = allPlaceholdersPattern.Replace(finalHtml, match =>
            {
                // Lấy tên trường từ kết quả regex
                string fieldName = match.Groups[1].Value;

                if (savedData.TryGetValue(fieldName, out var value) && !string.IsNullOrEmpty(value))
                {
                    // Ý NGHĨA: Nếu có dữ liệu, thay thế TOÀN BỘ thẻ span cũ bằng một thẻ span mới
                    // với class "highlighted-merge" (nền xanh).
                    string displayValue = FormatDisplayValue(value, fields.FirstOrDefault(f => f.FieldName == fieldName)?.DataType);
                    return $"<span class=\"highlighted-merge\">{displayValue}</span>";
                }
                else
                {
                    // Ý NGHĨA: Nếu không có dữ liệu, thay thế TOÀN BỘ thẻ span cũ bằng một thẻ span mới
                    // với class "placeholder-span" (nền vàng) và nội dung là <<TênTrường>>.
                    return $"<span class=\"placeholder-span placeholder-empty\">{System.Net.WebUtility.HtmlEncode($"<<{fieldName}>>")}</span>";
                }
            });

            return Json(new { success = true, htmlContent = finalHtml });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo merged HTML preview cho FormData ID {FormDataId}", id);
            return Json(new { success = false, message = "Có lỗi xảy ra phía máy chủ." });
        }
    }

    /// <summary>
    /// Hàm helper để sinh HTML preview từ một template.
    /// ĐÂY LÀ PHẦN ĐƯỢC CẢI TIẾN ĐỂ ĐỌC FILE AN TOÀN.
    /// </summary>
    private async Task<(string? htmlContent, bool hasFile)> GenerateHtmlPreviewAsync(Templates template)
    {
        string? htmlContent = null;
        bool hasFile = false;

        try
        {
            string? relativeFilePath = null;
            if (template.Status?.Equals("Mapped", StringComparison.OrdinalIgnoreCase) == true && !string.IsNullOrEmpty(template.MappedDocxFilePath))
            {
                relativeFilePath = template.MappedDocxFilePath;
            }
            else if (!string.IsNullOrEmpty(template.OriginalDocxFilePath))
            {
                relativeFilePath = template.OriginalDocxFilePath;
            }

            if (!string.IsNullOrEmpty(relativeFilePath))
            {
                // SỬA ĐỔI: Thay thế lệnh đọc file không an toàn bằng cách gọi service đã được tối ưu.
                // Dòng code cũ: var docxBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                var docxBytes = await _templateStorageService.GetFileBytesAsync(relativeFilePath);

                if (docxBytes != null)
                {
                    htmlContent = _docxToHtmlService.ConvertToHtml(docxBytes, _templateSettings.MaxTableNestingLevel, isViewMode: true);
                    hasFile = true;
                    _logger.LogInformation("Successfully converted DOCX to HTML for template {TemplateId}", template.TemplateID);
                }
                else
                {
                    _logger.LogWarning("Không thể đọc file template: {FilePath}", relativeFilePath);
                    // Không cần gán htmlContent lỗi ở đây vì nơi gọi sẽ xử lý
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting DOCX to HTML for template {TemplateId}", template.TemplateID);
            // Không gán htmlContent lỗi ở đây, trả về null để nơi gọi xử lý
        }

        return (htmlContent, hasFile);
    }

    /// <summary>
    /// Hàm helper để định dạng giá trị hiển thị dựa trên kiểu dữ liệu.
    /// </summary>
    /**
     * THAY ĐỔI: Cập nhật hàm FormatDisplayValue để sử dụng văn hóa en-US
     * Giúp định dạng số theo kiểu 12,345.67
     */
    private static string FormatDisplayValue(string value, string? dataType)
    {
        if (string.IsNullOrEmpty(value)) return "";

        switch (dataType?.ToUpperInvariant())
        {
            case "NUMBER":
                // Dùng InvariantCulture để parse, đảm bảo '.' luôn là dấu thập phân
                if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                {
                    // // Dùng "en-US" để định dạng với ',' cho hàng nghìn và '.' cho thập phân
                    // return num.ToString("N", new CultureInfo("en-US")); //Mặc định N là N2

                    // Phân tách phần nguyên & phần thập phân bằng chính số gốc
                    // Hiển thị phần thập phân theo số lượng số thập phân trong số gốc
                    var parts = value.Split('.');
                    var decimalDigits = parts.Length == 2 ? parts[1].Length : 0;
                    var format = decimalDigits > 0 ? $"N{decimalDigits}" : "N0";

                    return num.ToString(format, new CultureInfo("en-US"));
                }
                return value;

            case "DATE":
                if (DateTime.TryParse(value, out var date))
                {
                    return date.ToString("dd/MM/yyyy");
                }
                return value;

            default:
                return value;
        }
    }

}
