using System;
using System.IO;
using System.Threading.Tasks;
using CTOM.Models.Settings;
using CTOM.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CTOM.Services
{
    /// <summary>
    /// Service xử lý việc lưu trữ và truy xuất file template
    /// </summary>
    public class TemplateStorageService : ITemplateStorageService
    {
        private readonly TemplateSettings _settings;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TemplateStorageService> _logger;

        public TemplateStorageService(
            IOptions<TemplateSettings> settings,
            IWebHostEnvironment environment,
            ILogger<TemplateStorageService> logger)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Đảm bảo thư mục gốc tồn tại
            var rootPath = GetRootPath();
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
                _logger.LogInformation("Created template root directory at {RootPath}", rootPath);
            }
        }

        public async Task<string> SaveTemplateAsync(IFormFile file, string userName, int businessOperationId, int? templateId = null)
        {
            ArgumentNullException.ThrowIfNull(file);
            if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("User name cannot be empty", nameof(userName));
            if (businessOperationId <= 0) throw new ArgumentException("Business Operation ID must be greater than 0", nameof(businessOperationId));

            // Validate file extension
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_settings.AllowedExtensions.Contains(fileExtension))
            {
                throw new InvalidOperationException($"Invalid file extension. Allowed extensions: {string.Join(", ", _settings.AllowedExtensions)}");
            }

            // Validate file size
            if (file.Length > _settings.MaxFileSizeBytes)
            {
                throw new InvalidOperationException($"File size exceeds the maximum allowed size of {_settings.MaxFileSizeMB}MB");
            }

            // Tạo đường dẫn tương đối và đầy đủ
            var relativePath = GetRelativePath(userName, businessOperationId, templateId, fileExtension);
            var fullPath = GetFullPath(relativePath);
            var directoryPath = Path.GetDirectoryName(fullPath);

            if (string.IsNullOrEmpty(directoryPath))
            {
                throw new InvalidOperationException("Không thể xác định thư mục đích từ đường dẫn: " + relativePath);
            }

            // Tạo thư mục nếu chưa tồn tại
            if (!Directory.Exists(directoryPath))
            {
                _ = Directory.CreateDirectory(directoryPath);
                _logger.LogInformation("Created directory: {DirectoryPath}", directoryPath);
            }

            // Lưu file
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("Saved template file to {FilePath}", fullPath);
            return relativePath; // Trả về đường dẫn tương đối từ RootPath
        }

        /// <summary>
        /// (MỚI) Triển khai phương thức lưu file từ byte array.
        /// </summary>
        public async Task<string> SaveFileAsync(byte[] fileContent, string relativePath)
        {
            if (fileContent == null || fileContent.Length == 0)
            {
                throw new ArgumentException("Nội dung file không được rỗng.", nameof(fileContent));
            }
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException("Đường dẫn tương đối không được rỗng.", nameof(relativePath));
            }

            var fullPath = GetFullPath(relativePath);
            var directoryPath = Path.GetDirectoryName(fullPath);

            if (string.IsNullOrEmpty(directoryPath))
            {
                throw new InvalidOperationException("Không thể xác định thư mục đích từ đường dẫn: " + relativePath);
            }

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                _logger.LogInformation("Đã tạo thư mục: {DirectoryPath}", directoryPath);
            }

            await File.WriteAllBytesAsync(fullPath, fileContent);
            _logger.LogInformation("Đã lưu file vào: {FilePath}", fullPath);

            return relativePath;
        }

        public bool DeleteTemplate(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            try
            {
                // Nếu là đường dẫn tương đối, chuyển đổi thành đường dẫn đầy đủ
                var fullPath = Path.IsPathRooted(filePath)
                    ? filePath
                    : GetFullPath(filePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("Deleted template file: {FilePath}", fullPath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template file: {FilePath}", filePath);
                return false;
            }
        }

        //OriginalDocxFilePath: {RootPath}/{UserName}/{BusinessOperationID}/{TemplateId}_original.docx
        //MappedDocxFilePath: {RootPath}/{UserName}/{BusinessOperationID}/{TemplateId}_mapped.docx
        public string GetFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be empty", nameof(path));

            var rootPath = GetRootPath();

            // Nếu đã là đường dẫn đầy đủ
            if (Path.IsPathRooted(path))
            {
                var normalizedPath = Path.GetFullPath(path);

                // Kiểm tra nếu đường dẫn đã nằm trong thư mục gốc
                if (normalizedPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                {
                    return normalizedPath;
                }

                throw new InvalidOperationException("Access to the specified path is not allowed.");
            }

            // Xử lý đường dẫn tương đối
            var safePath = path.Replace('\\', Path.DirectorySeparatorChar)
                             .Replace('/', Path.DirectorySeparatorChar)
                             .TrimStart(Path.DirectorySeparatorChar);

            // Lấy tên thư mục gốc từ rootPath
            var rootDirName = Path.GetFileName(rootPath.TrimEnd(Path.DirectorySeparatorChar));

            // Nếu đường dẫn đã bắt đầu bằng rootPath, bỏ qua
            if (safePath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                safePath = safePath.Substring(rootPath.Length).TrimStart(Path.DirectorySeparatorChar);
            }
            // Nếu đường dẫn bắt đầu bằng tên thư mục gốc, bỏ qua
            else if (!string.IsNullOrEmpty(rootDirName) &&
                    safePath.StartsWith(rootDirName, StringComparison.OrdinalIgnoreCase))
            {
                safePath = safePath.Substring(rootDirName.Length).TrimStart(Path.DirectorySeparatorChar);
            }

            var fullPath = Path.Combine(rootPath, safePath);
            var normalizedFullPath = Path.GetFullPath(fullPath);

            // Đảm bảo đường dẫn nằm trong thư mục gốc
            if (!normalizedFullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Access to the specified path is not allowed.");
            }

            return normalizedFullPath;
        }

        public bool FileExists(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            try
            {
                // Nếu là đường dẫn tương đối, chuyển đổi thành đường dẫn đầy đủ
                var fullPath = Path.IsPathRooted(filePath)
                    ? filePath
                    : GetFullPath(filePath);
                return File.Exists(fullPath);
            }
            catch
            {
                return false;
            }
        }

        private string GetRootPath()
        {
            // Nếu là đường dẫn tuyệt đối, sử dụng luôn
            if (Path.IsPathRooted(_settings.RootPath))
            {
                return _settings.RootPath;
            }

            // Ngược lại, kết hợp với thư mục gốc của ứng dụng
            return Path.Combine(_environment.ContentRootPath, _settings.RootPath);
        }

        private static string GetRelativePath(string userName, int businessOperationId, int? templateId, string fileExtension)
        {
            // Sử dụng tên người dùng thay vì ID để dễ nhận biết
            var safeUserName = Path.GetInvalidFileNameChars()
                .Aggregate(userName, (current, c) => current.Replace(c, '_'));

            if (templateId.HasValue)
            {
                // Định dạng: {UserName}/{BusinessOperationID}/{TemplateId}_original.docx
                var fileName = $"{templateId}_original{fileExtension}";
                return Path.Combine(safeUserName, businessOperationId.ToString(), fileName).Replace('\\', '/');
            }
            else
            {
                // Đối với file tạm, sử dụng định dạng khác
                var fileName = $"temp_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetRandomFileName()}{fileExtension}";
                return Path.Combine("Temp", safeUserName, businessOperationId.ToString(), fileName).Replace('\\', '/');
            }
        }

        // --- PHẦN BỔ SUNG: TRIỂN KHAI PHƯƠNG THỨC MỚI ---
        public async Task<byte[]?> GetFileBytesAsync(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                _logger.LogWarning("Yêu cầu đọc file với đường dẫn rỗng.");
                return null;
            }

            var fullPath = GetFullPath(relativePath);
            if (!System.IO.File.Exists(fullPath))
            {
                _logger.LogWarning("Yêu cầu đọc file không tồn tại: {FilePath}", fullPath);
                return null;
            }

            try
            {
                // Sử dụng FileShare.ReadWrite để cho phép các tiến trình khác đọc và ghi file
                // trong khi chúng ta đang đọc, giảm thiểu tối đa xung đột.
                using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "Lỗi IO khi đọc file (file có thể đang bị khóa): {FilePath}", fullPath);
                return null; // Trả về null để nơi gọi có thể xử lý lỗi
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định khi đọc file: {FilePath}", fullPath);
                return null;
            }
        }
    }
}
