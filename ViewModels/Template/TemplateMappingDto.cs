using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CTOM.ViewModels.Template
{
    /// <summary>
    /// DTO cho việc lưu trữ thông tin mapping của một trường
    /// </summary>
    public class MappedFieldDto
    {
        /// <summary>
        /// Tên trường
        /// </summary>
        [Required]
        public string FieldName { get; set; } = string.Empty;
        
        /// <summary>
        /// Tên hiển thị của trường
        /// </summary>
        [Required]
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>
        /// Loại dữ liệu của trường
        /// </summary>
        public string? DataType { get; set; }
        
        /// <summary>
        /// Có bắt buộc không
        /// </summary>
        public bool IsRequired { get; set; }
        
        /// <summary>
        /// Giá trị mặc định
        /// </summary>
        public string? DefaultValue { get; set; }
        
        /// <summary>
        /// Mô tả về trường
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Danh sách các vị trí của trường trong tài liệu
        /// </summary>
        public List<FieldPosition> Positions { get; set; } = new();
    }

    // Sử dụng class FieldPosition từ TemplateMappingViewModel

    /// <summary>
    /// DTO chứa toàn bộ thông tin mapping để lưu vào database
    /// </summary>
    public class MappingDataDto
    {
        /// <summary>
        /// Danh sách các trường đã được mapping
        /// </summary>
        public List<MappedFieldDto> MappedFields { get; set; } = new();
        
        /// <summary>
        /// Thời điểm thực hiện mapping
        /// </summary>
        public DateTime MappedAt { get; set; }
        
        /// <summary>
        /// Người dùng thực hiện mapping
        /// </summary>
        public string MappedBy { get; set; } = string.Empty;
        
        /// <summary>
        /// Phiên bản của cấu trúc mapping
        /// </summary>
        public string Version { get; set; } = "1.0";
    }
}
