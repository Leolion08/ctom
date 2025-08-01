using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using CTOM.Models.Enums;

namespace CTOM.ViewModels.Template
{
    public class TemplateViewModel
    {
        public int TemplateId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên template")]
        [StringLength(255, ErrorMessage = "Tên template không được vượt quá 255 ký tự")]
        [Display(Name = "Tên template")]
        public string TemplateName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn nghiệp vụ")]
        [Display(Name = "Nghiệp vụ")]
        public int? BusinessOperationId { get; set; }

        [Display(Name = "Tên nghiệp vụ")]
        public string? BusinessOperationName { get; set; }

        [Display(Name = "Tên file gốc")]
        public string? OriginalUploadFileName { get; set; }

        [Display(Name = "Người tạo")]
        public string? CreatedByUserName { get; set; }

        [Display(Name = "Phòng ban")]
        public string? CreatedDepartmentId { get; set; }

        [Display(Name = "Tên phòng ban")]
        public string? CreatedDepartmentName { get; set; }

        [Display(Name = "Ngày tạo")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        [DataType(DataType.DateTime)]
        public DateTime CreationTimestamp { get; set; } = DateTime.Now;

        [Display(Name = "Trạng thái")]
        //[Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        public string Status { get; set; } = "Draft";

        [Display(Name = "Mô tả")]
        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string? Description { get; set; }

        [Display(Name = "Khả dụng")]
        public bool IsActive { get; set; } = true;

        // Các trường dùng cho xử lý file
        //[Required(ErrorMessage = "Vui lòng chọn file template")]
        [Display(Name = "File template")]
        public IFormFile? TemplateFile { get; set; }

        [Display(Name = "Đường dẫn file")]
        public string? FilePath { get; set; }

        [Display(Name = "Tên file lưu trữ")]
        public string? StoredFileName { get; set; }


        [Display(Name = "Kích thước file")]
        public long? FileSize { get; set; }

        [Display(Name = "Loại nội dung")]
        public string? ContentType { get; set; }

        // Danh sách nghiệp vụ cho dropdown
        [Display(Name = "Danh sách nghiệp vụ")]
        public SelectList? BusinessOperations { get; set; }
    }

    /// <summary>
    /// Model cho việc cập nhật nhanh thông tin template
    /// </summary>
    public class QuickUpdateTemplateModel
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
