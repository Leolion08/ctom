using CTOM.Data;
using CTOM.Models.Entities;
using CTOM.Services.Interfaces;
using CTOM.ViewModels.Form;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace CTOM.Services;

/// <summary>
/// Thực thi nghiệp vụ lưu <see cref="FormData"/> từ dữ liệu động người dùng nhập.
/// </summary>
public sealed class FormDataService(
    ApplicationDbContext dbContext,
    ICurrentUserAccessor currentUserAccessor,
    ILogger<FormDataService> logger,
    IOptions<JsonSerializerOptions> jsonOptionsAccessor) : IFormDataService
{
    private readonly ApplicationDbContext _db = dbContext;
    private readonly ICurrentUserAccessor _user = currentUserAccessor;
    private readonly ILogger<FormDataService> _logger = logger;
    private readonly JsonSerializerOptions _jsonOptions = jsonOptionsAccessor.Value;

    public async Task<List<SelectListItem>> GetAvailableTemplateOptionsAsync()
    {
        var userName = _user.UserName;
        var maPhong = _user.MaPhong;

        var templates = await _db.Templates
            .Include(t => t.BusinessOperation)
            .AsNoTracking()
            .Where(t =>
                t.IsActive &&
                t.BusinessOperation != null &&
                t.BusinessOperation.IsActive &&
                (
                    t.CreatedByUserName == userName ||
                    (maPhong != null && t.CreatedDepartmentID == maPhong)
                )
            )
            .Select(t => new
            {
                t.TemplateID,
                t.TemplateName,
                t.BusinessOperation!.OperationName
            })
            .OrderBy(t => t.OperationName)
            .ThenBy(t => t.TemplateName)
            .ThenBy(t => t.TemplateID)
            .ToListAsync();

        return templates
            .Select(t => new SelectListItem
            {
                Value = t.TemplateID.ToString(),
                Text = $"{t.OperationName} > {t.TemplateName} (ID: {t.TemplateID})"
            })
            .ToList();
    }


    public async Task<long> CreateAsync(FormCreateStep2VM vm)
    {
        // basic validation – đảm bảo TemplateId hợp lệ
        if (vm.TemplateId <= 0)
            throw new ArgumentException($"TemplateId {vm.TemplateId} is invalid", nameof(vm));

        // Kiểm tra Template tồn tại
        if (!await _db.Templates.AsNoTracking().AnyAsync(t => t.TemplateID == vm.TemplateId))
            throw new InvalidOperationException($"Template {vm.TemplateId} không tồn tại.");

        // Serialize form values với _jsonOptions (có bật phản chiếu)
        var json = JsonSerializer.Serialize(vm.FormValues ?? [], _jsonOptions);

        var entity = new FormData
        {
            TemplateID = vm.TemplateId,
            Note = vm.Note,
            FormDataJson = json,
            SoCif = vm.FormValues?.GetValueOrDefault("SoCif"),
            CreatedByUserName = _user.UserName,
            CreatedDepartmentID = _user.MaPhong,
            CreationTimestamp = DateTime.Now,
            LastModificationTimestamp = DateTime.Now,
            Status = "Draft"
        };

        await _db.FormDatas.AddAsync(entity);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Đã tạo FormData {Id} bởi {User}", entity.FormDataID, _user.UserName);
        return entity.FormDataID;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(FormEditViewModel vm)
    {
        // Basic validation
        if (vm.FormDataID <= 0)
            throw new ArgumentException($"FormDataID {vm.FormDataID} is invalid", nameof(vm));

        var entity = await _db.FormDatas.FindAsync(vm.FormDataID);
        if (entity is null)
            throw new InvalidOperationException($"FormData {vm.FormDataID} không tồn tại.");

        // Serialize form values with configured options
        var json = JsonSerializer.Serialize(vm.FormValues ?? [], _jsonOptions);

        if (vm.Note is not null)
            entity.Note = vm.Note;
        entity.FormDataJson = json;
        entity.SoCif = vm.FormValues?.GetValueOrDefault("SoCif");
        entity.LastModificationTimestamp = DateTime.Now;
        entity.CreatedByUserName = _user.UserName;

        _db.Update(entity);
        var affected = await _db.SaveChangesAsync();
        _logger.LogInformation("Đã cập nhật FormData {Id}, rows {Rows}", entity.FormDataID, affected);
    }
}
