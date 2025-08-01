using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CTOM.ViewModels.Form
{
    /// <summary>
    /// ViewModel cho trang Edit, được cập nhật để chứa HTML preview.
    /// </summary>
    public record FormEditViewModel
    {
        public long FormDataID { get; init; }

        public int TemplateId { get; init; }

        public string? TemplateName { get; init; }

        [Display(Name = "Ghi chú")]
        public string? Note { get; init; }

        // Dùng để binding dữ liệu từ form động khi submit
        public Dictionary<string, string?> FormValues { get; init; } = new();

        // Dùng để truyền dữ liệu JSON ban đầu cho view (khi render GET)
        public string? FormDataJson { get; init; }     // chuỗi JSON thô

        // THUỘC TÍNH MỚI: Chứa HTML được render từ server
        public string? HtmlContent { get; init; }

        // THUỘC TÍNH MỚI: Cờ xác định có file preview hay không
        public bool HasFile { get; init; }
    }
}
