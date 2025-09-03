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
using System.Threading.Tasks;
using CTOM.Models.Entities;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

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
        {
            LogInvalidModelState("CreateStep1 POST");
            return ValidationProblem(ModelState);
        }

        try
        {
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

            fields = fields.OrderBy(f => f.FieldName != "SoCif").ToList();

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
            // Ghi log chi tiết ModelState khi không hợp lệ
            LogInvalidModelState("Create POST");

            TempData["ErrorMessage"] = "Dữ liệu chưa hợp lệ, vui lòng kiểm tra lại.";
            return RedirectToAction(nameof(Create));
        }

        try
        {
            // Chuẩn hóa FormValues về dictionary rỗng nếu null để tránh lỗi downstream
            var normalizedVm = vm with { FormValues = vm.FormValues ?? new Dictionary<string, string?>() };
            var id = await _formDataService.CreateAsync(normalizedVm);
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
            TempData["ErrorMessage"] = "Không tìm thấy template liên kết với giao dịch này.";
            return RedirectToAction(nameof(Index));
        }

        var (htmlContent, hasFile) = await GenerateHtmlPreviewAsync(template);

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
            HtmlContent = htmlContent,
            HasFile = hasFile
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
     * PHẦN THAY ĐỔI: Cập nhật Action Details để sinh HTML preview đã trộn dữ liệu
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

        // THAY ĐỔI: Gọi hàm sinh HTML ĐÃ ĐƯỢC TRỘN DỮ LIỆU thay vì HTML gốc
        var mergedHtmlContent = await GenerateMergedHtmlAsync(formData);
        var hasFile = !string.IsNullOrEmpty(mergedHtmlContent);

        string fullTemplateName = "Không xác định";
        var operationName = template.BusinessOperation?.OperationName ?? "N/A";
        fullTemplateName = $"{operationName} > {template.TemplateName} (ID: {template.TemplateID})";

        var vm = new FormEditViewModel
        {
            FormDataID = formData.FormDataID,
            TemplateId = formData.TemplateID,
            Note = formData.Note,
            TemplateName = fullTemplateName,
            FormDataJson = formData.FormDataJson,
            HtmlContent = mergedHtmlContent, // Gán nội dung HTML đã trộn
            HasFile = hasFile
        };
        
        if (template.LastModificationTimestamp.HasValue && 
            template.LastModificationTimestamp.Value > formData.CreationTimestamp)
        {
            ViewData["ShowTemplateVersionWarning"] = true;
        }

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

        if (!ModelState.IsValid)
        {
            LogInvalidModelState($"Edit POST (FormDataID={id})");
            var errorDetails = ModelState
                .Where(ms => ms.Value is { Errors.Count: > 0 })
                .Select(ms => new
                {
                    field = ms.Key,
                    value = Request.HasFormContentType ? Request.Form[ms.Key].ToString() : null,
                    errors = ms.Value!.Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .ToArray()
                })
                .ToList();

            return Json(new { success = false, message = "Dữ liệu không hợp lệ.", errors = errorDetails });
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

        // === THAY ĐỔI: Gọi hàm sinh HTML đã được TRỘN DỮ LIỆU ===
        // Thay vì gọi GenerateHtmlPreviewAsync, chúng ta gọi GenerateMergedHtmlAsync
        // để lấy HTML có sẵn dữ liệu của giao dịch gốc.
        var mergedHtmlContent = await GenerateMergedHtmlAsync(originalForm);
        var hasFile = !string.IsNullOrEmpty(mergedHtmlContent);

        string fullTemplateName = "Không xác định";
        var operationName = template.BusinessOperation?.OperationName ?? "N/A";
        fullTemplateName = $"{operationName} > {template.TemplateName} (ID: {template.TemplateID})";

        var viewModel = new FormCopyViewModel
        {
            TemplateId = originalForm.TemplateID,
            TemplateName = fullTemplateName,
            Note = originalForm.Note,
            FormDataJson = originalForm.FormDataJson,
            HtmlContent = mergedHtmlContent, // Gán nội dung HTML đã trộn
            HasFile = hasFile
        };

        return View("Copy", viewModel);
    }

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
            
            // THAY ĐỔI: Gọi hàm helper mới để lấy HTML đã trộn
            var finalHtml = await GenerateMergedHtmlAsync(formData);

            if (string.IsNullOrEmpty(finalHtml))
            {
                 return Json(new { success = false, message = "Template không có file để xem trước hoặc có lỗi xảy ra." });
            }

            return Json(new { success = true, htmlContent = finalHtml });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo merged HTML preview cho FormData ID {FormDataId}", id);
            return Json(new { success = false, message = "Có lỗi xảy ra phía máy chủ." });
        }
    }

    /// <summary>
    /// Hàm helper để sinh HTML preview của template GỐC (chưa trộn dữ liệu).
    /// </summary>
    private async Task<(string? htmlContent, bool hasFile)> GenerateHtmlPreviewAsync(Templates template)
    {
        string? htmlContent = null;
        bool hasFile = false;

        try
        {
            string? relativeFilePath = template.MappedDocxFilePath;
            if (string.IsNullOrEmpty(relativeFilePath) || !_templateStorageService.FileExists(relativeFilePath))
            {
                relativeFilePath = template.OriginalDocxFilePath;
            }

            if (!string.IsNullOrEmpty(relativeFilePath))
            {
                var docxBytes = await _templateStorageService.GetFileBytesAsync(relativeFilePath);
                if (docxBytes != null)
                {
                    htmlContent = _docxToHtmlService.ConvertToHtml(docxBytes, isViewMode: true);
                    hasFile = true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting DOCX to HTML for template {TemplateId}", template.TemplateID);
        }

        return (htmlContent, hasFile);
    }
    
    /// <summary>
    /// BỔ SUNG: Hàm helper mới để sinh HTML đã được TRỘN DỮ LIỆU.
    /// </summary>
    private async Task<string?> GenerateMergedHtmlAsync(FormData formData)
    {
        if (formData?.Template == null) return null;

        // 1. Lấy HTML gốc của template.
        var (baseHtml, hasFile) = await GenerateHtmlPreviewAsync(formData.Template);
        if (!hasFile || string.IsNullOrEmpty(baseHtml))
        {
            return baseHtml; // Trả về null hoặc empty nếu template không có file.
        }

        // 2. Lấy dữ liệu đã lưu (parse an toàn). Nếu JSON không hợp lệ, fallback về {} để tránh lỗi.
        Dictionary<string, string?> savedData;
        try
        {
            savedData = JsonSerializer.Deserialize<Dictionary<string, string?>>(formData.FormDataJson ?? "{}", _jsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FormDataJson không hợp lệ cho FormDataID {FormDataId}. Fallback về rỗng", formData.FormDataID);
            savedData = new();
        }
        
        // 3. Lấy định nghĩa các trường để biết kiểu dữ liệu (phục vụ format).
        var fields = await _db.TemplateFields
            .AsNoTracking()
            .Where(f => f.TemplateID == formData.TemplateID)
            .ToListAsync();

        // 4. Thực hiện thay thế.
        //    Placeholder trong HTML (với isViewMode: true) có dạng &lt;&lt;FieldName&gt;&gt;
        var placeholderPattern = new Regex("&lt;&lt;([a-zA-Z0-9_]+)&gt;&gt;");
        string finalHtml = placeholderPattern.Replace(baseHtml, match =>
        {
            string fieldName = match.Groups[1].Value;
            if (savedData.TryGetValue(fieldName, out var value) && !string.IsNullOrEmpty(value))
            {
                string displayValue = FormatDisplayValue(value, fields.FirstOrDefault(f => f.FieldName == fieldName)?.DataType);
                // THAY ĐỔI: Thêm data-placeholder-for vào cả trường hợp có dữ liệu
                return $"<span class=\"highlighted-merge\" data-placeholder-for=\"{fieldName}\">{WebUtility.HtmlEncode(displayValue)}</span>";
            }
            else
            {
                // THAY ĐỔI: Thêm data-placeholder-for vào trường hợp rỗng
                return $"<span class=\"placeholder-span placeholder-empty\" data-placeholder-for=\"{fieldName}\">{match.Value}</span>";
            }
        });

        return finalHtml;
    }


    /// <summary>
    /// Hàm helper để định dạng giá trị hiển thị dựa trên kiểu dữ liệu.
    /// </summary>
    private static string FormatDisplayValue(string value, string? dataType)
    {
        if (string.IsNullOrEmpty(value)) return "";

        switch (dataType?.ToUpperInvariant())
        {
            case "NUMBER":
                if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                {
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

    /// <summary>
    /// Ghi log chi tiết các lỗi ModelState hiện tại. Dùng chung cho các action POST.
    /// </summary>
    private void LogInvalidModelState(string actionName)
    {
        try
        {
            var errorDetails = ModelState
                .Where(ms => ms.Value is { Errors.Count: > 0 })
                .Select(ms => new
                {
                    Field = ms.Key,
                    Value = Request.HasFormContentType ? Request.Form[ms.Key].ToString() : null,
                    Errors = ms.Value!.Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .ToArray()
                })
                .ToList();

            _logger.LogWarning("ModelState không hợp lệ tại {Action}. Details: {ErrorDetails}",
                actionName,
                JsonSerializer.Serialize(errorDetails, _jsonOptions));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể serialize chi tiết ModelState tại {Action}", actionName);
        }
    }
}
