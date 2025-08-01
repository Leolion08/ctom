using CTOM.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CTOM.Services;

/// <summary>
/// Runtime helper lấy thông tin người dùng hiện tại từ <see cref="HttpContext"/>/Claims.
/// </summary>
public sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public string UserName => Principal?.Identity?.IsAuthenticated == true
        ? Principal!.Identity!.Name ?? string.Empty
        : string.Empty;

    public string? MaPhong => Principal?.FindFirst("MaPhong")?.Value;

    public IEnumerable<Claim>? Claims => Principal?.Claims;

    public bool IsInRole(string roleName) => Principal?.IsInRole(roleName) ?? false;
}
