namespace CTOM.Models.Settings
{
    /// <summary>
    /// Cấu hình cho việc upload và lưu trữ template
    /// </summary>
    public class TemplateSettings
    {
        /// <summary>
        /// Thư mục gốc lưu trữ template
        /// Mặc định: "TemplatesData"
        /// </summary>
        public string RootPath { get; set; } = "TemplatesData";

        /// <summary>
        /// Dung lượng tối đa của file template (MB)
        /// Mặc định: 2MB
        /// </summary>
        public int MaxFileSizeMB { get; set; } = 2;

        /// <summary>
        /// Dung lượng tối đa của file template (tính bằng byte)
        /// </summary>
        public long MaxFileSizeBytes => MaxFileSizeMB * 1024 * 1024;

        /// <summary>
        /// Định dạng file được phép upload
        /// </summary>
        public string[] AllowedExtensions { get; set; } = new[] { ".docx" };

        /// <summary>
        /// Mức độ lồng bảng tối đa cho phép mapping
        /// 0 = chỉ bảng cha, 1 = bảng cha + bảng con lồng 1 cấp, 2 = cho phép lồng 2 cấp, v.v.
        /// Mặc định: 1 (cho phép bảng con lồng 1 cấp)
        /// </summary>
        public int MaxTableNestingLevel { get; set; } = 1; //Không sử dụng
    }
}
