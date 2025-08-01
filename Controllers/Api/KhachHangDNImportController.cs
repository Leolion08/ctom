using CTOM.Models.Responses;
using CTOM.Services;
using CTOM.Models.Config; // Thêm namespace cho FieldConfig
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // Thêm cho Dictionary
using System.IO;
using System.Threading.Tasks;

namespace CTOM.Controllers.Api;

/// <summary>
/// API Controller xử lý import dữ liệu Khách hàng doanh nghiệp từ Excel
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize] // Yêu cầu đăng nhập
[Produces("application/json")] // Đảm bảo phản hồi luôn có Content-Type: application/json
public class KhachHangDNImportController : ControllerBase
{
    private readonly ExcelImportService _importService;
    private readonly ILogger<KhachHangDNImportController> _logger;

    public KhachHangDNImportController(
        ExcelImportService importService,
        ILogger<KhachHangDNImportController> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    /// <summary>
    /// Import dữ liệu từ file Excel
    /// </summary>
    /// <param name="file">File Excel cần import</param>
    /// <returns>Kết quả import</returns>
    [HttpPost("")]
    [RequestSizeLimit(5 * 1024 * 1024)] // Giới hạn mặc định 5MB (sẽ được ghi đè bởi middleware)
    public async Task<ApiResponse<ImportResponse>> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return ApiResponse<ImportResponse>.Fail("Vui lòng chọn file để import");
        }

        // Bỏ validate kích thước file ở đây vì đã có trong ExcelImportService
        // Đồng thời đã có RequestSizeLimit ở cấp controller

        // Kiểm tra định dạng file
        var fileExt = Path.GetExtension(file.FileName).ToLowerInvariant();
        //if (fileExt != ".xlsx" && fileExt != ".xls")
        if (fileExt != ".xlsx")
        {
            return ApiResponse<ImportResponse>.Fail("Chỉ hỗ trợ file Excel (.xlsx)");
        }

        try
        {
            _logger.LogInformation("Bắt đầu import file: {FileName} ({FileSize} bytes)",
                file.FileName, file.Length);

            ImportResponse result;
            using (var stream = file.OpenReadStream())
            {
                result = await _importService.ImportFromExcel(
                    fileStream: stream,
                    //fileName: file.FileName,
                    sheetName: string.Empty,
                    hasHeader: true,
                    fieldConfigs: new Dictionary<string, Models.Config.FieldConfig>()
                );
            }

            // Log kết quả import
            if (result.HasErrors)
            {
                _logger.LogWarning("Import hoàn thành với {ErrorCount} lỗi: {SuccessCount}/{TotalRows} bản ghi",
                    result.ErrorCount, result.SuccessCount, result.TotalRows);
                return ApiResponse<ImportResponse>.Fail(result.Message, result);
            }

            _logger.LogInformation("Import thành công: {SuccessCount}/{TotalRows} bản ghi",
                result.SuccessCount, result.TotalRows);

            return ApiResponse<ImportResponse>.Ok(result, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi import file {FileName}", file.FileName);
            return ApiResponse<ImportResponse>.Fail("Có lỗi xảy ra khi import dữ liệu: " + ex.Message);
        }
    }
}
