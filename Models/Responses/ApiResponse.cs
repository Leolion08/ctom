using System.Collections.Generic;
using System.Text.Json.Serialization; // Sử dụng JsonPropertyName

namespace CTOM.Models.Responses
{
    /// <summary>
    /// Base response cho tất cả các API
    /// </summary>
    public class ApiResponse
    {
        //Tên thuộc tính viết thường (camelCase) cho tất cả các thuộc tính
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("exists")]
        public bool Exists { get; set; } // Thuộc tính này có vẻ đặc thù, giữ lại theo file gốc

        [JsonPropertyName("data")]
        public object? Data { get; set; } // Dữ liệu chung nếu không dùng generic

        [JsonPropertyName("errors")]
        public IDictionary<string, string[]>? Errors { get; set; } // Lỗi theo kiểu ModelState

        // Factory methods cho ApiResponse không generic
        public static ApiResponse Ok(string message = "Thành công", object? data = null)
            => new() { Success = true, Message = message, Data = data };

        // Phương thức Fail này nhận IDictionary cho errors, thường dùng cho lỗi validation từ ModelState
        public static ApiResponse Fail(string message, IDictionary<string, string[]>? errors = null)
            => new() { Success = false, Message = message, Errors = errors };

        public static ApiResponse Existed(string message = "Đã tồn tại", object? data = null)
            => new() { Success = true, Message = message, Exists = true, Data = data };

        public static ApiResponse NotExisted(string message = "Không tồn tại", object? data = null)
            => new() { Success = true, Message = message, Exists = false, Data = data };
    }

    /// <summary>
    /// Generic version của ApiResponse để chứa dữ liệu có kiểu cụ thể
    /// </summary>
    /// <typeparam name="T">Kiểu của thuộc tính Data</typeparam>
    public class ApiResponse<T> : ApiResponse
    {
        // 'new' keyword để ẩn thuộc tính Data của lớp base và cung cấp kiểu T cụ thể
        [JsonPropertyName("data")]
        public new T? Data { get; set; }

        // Factory methods cho ApiResponse<T>

        /// <summary>
        /// Tạo phản hồi thành công với dữ liệu kiểu T.
        /// </summary>
        public static ApiResponse<T> Ok(T data, string message = "Thành công")
            => new() { Success = true, Message = message, Data = data };

        /// <summary>
        /// Tạo phản hồi thành công không có dữ liệu cụ thể (Data sẽ là default của T).
        /// </summary>
        public static ApiResponse<T> Ok(string message = "Thành công")
             => new() { Success = true, Message = message, Data = default };


        /// <summary>
        /// Tạo phản hồi thất bại với dữ liệu chi tiết kiểu T (ví dụ: thông tin chi tiết về lỗi import).
        /// Thuộc tính 'Errors' (IDictionary) của lớp base vẫn có thể được sử dụng cho các lỗi validation chung.
        /// </summary>
        public static ApiResponse<T> Fail(string message, T? data, IDictionary<string, string[]>? validationErrors = null)
            => new() { Success = false, Message = message, Data = data, Errors = validationErrors };

        /// <summary>
        /// Tạo phản hồi thất bại chỉ với thông điệp và các lỗi validation (không có data cụ thể kiểu T).
        /// </summary>
        public static new ApiResponse<T> Fail(string message, IDictionary<string, string[]>? validationErrors = null) // 'new' để phân biệt
            => new() { Success = false, Message = message, Data = default, Errors = validationErrors };
    }
}
