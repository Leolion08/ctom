using CTOM.ViewModels.Form;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CTOM.Services.Interfaces;

/// <summary>
/// Cung cấp các thao tác nghiệp vụ với FormData.
/// </summary>
public interface IFormDataService
{
    /// <summary>
    /// Tạo mới một bản ghi <see cref="Models.Entities.FormData"/> từ dữ liệu người dùng nhập.
    /// </summary>
    /// <param name="vm">ViewModel bước 2 chứa dữ liệu động.</param>
    /// <returns>ID của FormData vừa tạo.</returns>
    Task<long> CreateAsync(FormCreateStep2VM vm);

    /// <summary>
    /// Cập nhật dữ liệu <see cref="Models.Entities.FormData"/> đã tồn tại.
    /// </summary>
    /// <param name="vm">ViewModel chứa dữ liệu chỉnh sửa.</param>
    Task UpdateAsync(FormEditViewModel vm);

    /// <summary>
    /// Lấy danh sách template có thể chọn.
    /// </summary>
    /// <returns>Danh sách template.</returns>
    Task<List<SelectListItem>> GetAvailableTemplateOptionsAsync();
}
