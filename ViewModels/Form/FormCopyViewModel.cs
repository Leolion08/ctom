namespace CTOM.ViewModels.Form
{
    /// <summary>
    /// ViewModel cho trang Sao chép giao dịch.
    /// Chứa dữ liệu ban đầu được sao chép từ một giao dịch đã có.
    /// </summary>
    public class FormCopyViewModel
    {
        public int TemplateId { get; set; }

        public string? TemplateName { get; set; }

        public string? Note { get; set; }

        // Dữ liệu JSON của các trường động để điền vào form
        public string? FormDataJson { get; set; }

        // THUỘC TÍNH MỚI: Chứa HTML được render từ server
        public string? HtmlContent { get; set; }

        // THUỘC TÍNH MỚI: Cờ xác định có file preview hay không
        public bool HasFile { get; set; }
    }
}
