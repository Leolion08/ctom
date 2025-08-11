using CTOM.Models.Settings;
using Microsoft.AspNetCore.Http;

namespace CTOM.Services.Interfaces
{
    /// <summary>
    /// Service xử lý việc lưu trữ và truy xuất file template
    /// </summary>
    public interface ITemplateStorageService
    {
        /// <summary>
        /// Lưu file template vào thư mục được chỉ định
        /// </summary>
        /// <param name="file">File template cần lưu</param>
        /// <param name="userName">Tên đăng nhập của người dùng</param>
        /// <param name="businessOperationId">ID của nghiệp vụ</param>
        /// <param name="templateId">ID của template (nếu có)</param>
        /// <returns>Đường dẫn tương đối đến file đã lưu</returns>
        Task<string> SaveTemplateAsync(IFormFile file, string userName, int businessOperationId, int? templateId = null);

        /// <summary>
        /// (MỚI) Lưu nội dung file từ một mảng byte vào một đường dẫn tương đối.
        /// Hàm này dùng để lưu file đã được xử lý (ví dụ: file mapped).
        /// </summary>
        /// <param name="fileContent">Nội dung file dưới dạng byte array.</param>
        /// <param name="relativePath">Đường dẫn tương đối nơi file sẽ được lưu.</param>
        /// <returns>Đường dẫn tương đối đến file đã lưu.</returns>
        Task<string> SaveFileAsync(byte[] fileContent, string relativePath);

        /// <summary>
        /// Xóa file template
        /// </summary>
        /// <param name="relativePath">Đường dẫn tương đối đến file cần xóa</param>
        /// <returns>True nếu xóa thành công, ngược lại false</returns>
        bool DeleteTemplate(string relativePath);

        /// <summary>
        /// Lấy đường dẫn đầy đủ đến file template
        /// </summary>
        /// <param name="relativePath">Đường dẫn tương đối đến file</param>
        /// <returns>Đường dẫn đầy đủ đến file</returns>
        string GetFullPath(string relativePath);

        /// <summary>
        /// Kiểm tra file có tồn tại không
        /// </summary>
        /// <param name="relativePath">Đường dẫn tương đối đến file</param>
        /// <returns>True nếu file tồn tại, ngược lại false</returns>
        bool FileExists(string relativePath);

        /// <summary>
        /// (MỚI) Đọc nội dung file một cách an toàn dựa trên đường dẫn tương đối.
        /// Phương thức này thay thế cho logic đọc file trực tiếp trong Controller để tránh lỗi file lock.
        /// </summary>
        /// <param name="relativePath">Đường dẫn tương đối của file.</param>
        /// <returns>Nội dung file dưới dạng byte array, hoặc null nếu có lỗi.</returns>
        Task<byte[]?> GetFileBytesAsync(string relativePath);

        /// <summary>
        /// (MỚI) Lưu file DOCX đã được điền placeholder (mapped).
        /// Phương thức này sẽ tự quản lý đường dẫn lưu file dựa trên ID của template.
        /// </summary>
        /// <param name="templateId">ID của template.</param>
        /// <param name="fileContent">Nội dung file dưới dạng byte array.</param>
        /// <param name="userName">Tên người dùng của chủ sở hữu template.</param>
        /// <param name="businessOperationId">ID của nghiệp vụ.</param>
        /// <returns>Đường dẫn tương đối đến file đã lưu.</returns>
        Task<string> SaveMappedFileAsync(int templateId, byte[] fileContent, string userName, int businessOperationId);
    }
}
