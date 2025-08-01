using CTOM.Models.Entities;
using CTOM.Models.Responses;
using CTOM.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTOM.Controllers.Api;

/// <summary>
/// API trả về JSON dữ liệu của một bản ghi FormData dùng cho chức năng Merge DOCX client-side.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed partial class FormDataController(ILogger<FormDataController> logger, ApplicationDbContext db)
    : ControllerBase
{
    /// <summary>
    /// Lấy dữ liệu JSON của một bản ghi FormData.
    /// </summary>
    /// <param name="id">FormDataID</param>
    /// <returns>ApiResponse chứa object JSON (deserialize thành dynamic/object)</returns>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object?>>> Get(long id)
    {
        try
        {
            var json = await db.FormDatas
                               .AsNoTracking()
                               .Where(x => x.FormDataID == id)
                               .Select(x => x.FormDataJson)
                               .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(json))
            {
                return NotFound(ApiResponse<object>.Fail("Không tìm thấy dữ liệu."));
            }

            // Cố gắng deserialize JSON; nếu lỗi (ví dụ JSON không hợp lệ) thì trả về chuỗi thô để tránh 500.
            object dataObject;
            try
            {
                dataObject = System.Text.Json.JsonDocument.Parse(json).RootElement.Clone();
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                logger.LogWarning(jsonEx, "Invalid JSON stored in FormData {FormDataId}", id);
                dataObject = json; // Fallback: raw string
            }

            return Ok(ApiResponse<object>.Ok(dataObject, "Thành công"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get FormData {FormDataId} error", id);
            return StatusCode(500, ApiResponse<object>.Fail("Lỗi máy chủ"));
        }
    }
}
