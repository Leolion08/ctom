using CTOM.Data;
using CTOM.Models.Entities;
using CTOM.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTOM.Controllers.Api;

/// <summary>
/// Lookup corporate customer by CIF.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
[Produces("application/json")]
public sealed class CifController(ApplicationDbContext db) : Controller
{
    /// <summary>
    /// Get customer info by CIF number.
    /// </summary>
    /// <param name="id">CIF number (SoCif).</param>
    /// <returns>Customer data or 404.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        var cif = id.Trim();
        KhachHangDN? customer = await db.KhachHangDNs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SoCif == cif);

        if (customer is null){
            return Json(ApiResponse.NotExisted($"Không tìm thấy CIF {cif} trong kho dữ liệu."));
        } else {
            return Json(ApiResponse<KhachHangDN>.Existed($"Kho dữ liệu có tồn tại CIF {cif}", customer));
        }
    }
}
