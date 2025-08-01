using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTOM.Models.Entities
{
    public class TemplateField
    {
        /// <summary>
        /// Khóa chính của bảng TemplateField
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TemplateFieldID { get; set; }

        /// <summary>
        /// Khóa ngoại tham chiếu đến bảng Templates
        /// </summary>
        [Required(ErrorMessage = "TemplateID là bắt buộc")]
        public int TemplateID { get; set; }

        /// <summary>
        /// Tên trường (không dấu, viết liền)
        /// </summary>
        [Required(ErrorMessage = "Tên trường là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên trường không được vượt quá 100 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên trường chỉ được chứa chữ cái, số và dấu gạch dưới")]
        public string FieldName { get; set; } = null!;

        /// <summary>
        /// Tên hiển thị của trường (có dấu, có khoảng trắng)
        /// </summary>
        [StringLength(100, ErrorMessage = "Tên hiển thị không được vượt quá 100 ký tự")]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Kiểu dữ liệu của trường (text, number, date, datetime, boolean, ...)
        /// </summary>
        [StringLength(50, ErrorMessage = "Kiểu dữ liệu không được vượt quá 50 ký tự")]
        public string? DataType { get; set; } = "text";

        /// <summary>
        /// Giá trị mặc định của trường
        /// </summary>
        [StringLength(500, ErrorMessage = "Giá trị mặc định không được vượt quá 500 ký tự")]
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Xác định xem trường có bắt buộc nhập hay không
        /// </summary>
        [Display(Name = "Bắt buộc")]
        public bool IsRequired { get; set; }

        /// <summary>
        /// Thứ tự hiển thị của trường
        /// </summary>
        [Display(Name = "Thứ tự")]
        public int? DisplayOrder { get; set; }

        /// <summary>
        /// Mô tả chi tiết về trường
        /// </summary>
        [StringLength(200, ErrorMessage = "Mô tả không được vượt quá 200 ký tự")]
        public string? Description { get; set; }

        /// <summary>
        /// Navigation property tham chiếu đến Template chứa trường này
        /// </summary>
        [ForeignKey(nameof(TemplateID))]
        [InverseProperty(nameof(Templates.TemplateFields))]
        public virtual Templates? Template { get; set; }

        /// <summary>
        /// Loại nguồn dữ liệu của trường ('CIF', 'INPUT', 'CALC')
        /// </summary>
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string? DataSourceType { get; set; }

        /// <summary>
        /// Công thức tính toán cho trường loại CALC
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? CalculationFormula { get; set; }
    }
}
