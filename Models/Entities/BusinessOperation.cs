using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTOM.Models.Entities
{
    /// <summary>
    /// Định nghĩa danh mục các nghiệp vụ của ngân hàng để phân loại template.
    /// Hỗ trợ cấu trúc cây 2 cấp (cha-con)
    /// </summary>
    public class BusinessOperation
    {
        [Key]
        [Display(Name = "Mã nghiệp vụ")]
        public int OperationID { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại khách hàng")]
        [StringLength(2)]
        [Display(Name = "Loại khách hàng")]
        public string CustomerType { get; set; } = "DN"; // DN: Doanh nghiệp, CN: Cá nhân

        [Required(ErrorMessage = "Vui lòng nhập tên nghiệp vụ")]
        [StringLength(100, ErrorMessage = "Tên nghiệp vụ không vượt quá 100 ký tự")]
        [Display(Name = "Tên nghiệp vụ")]
        public required string OperationName { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không vượt quá 500 ký tự")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Nhóm cha")]
        public int? ParentOperationID { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Ngày cập nhật")]
        public DateTime? ModifiedDate { get; set; }

        [StringLength(50)]
        [Display(Name = "Người tạo")]
        public string? CreatedBy { get; set; }

        [StringLength(50)]
        [Display(Name = "Người cập nhật")]
        public string? ModifiedBy { get; set; }

        // Navigation properties
        [ForeignKey("ParentOperationID")]
        [Display(Name = "Nhóm nghiệp vụ")]
        public virtual BusinessOperation? ParentOperation { get; set; }

        [InverseProperty("ParentOperation")]
        public virtual ICollection<BusinessOperation>? ChildOperations { get; set; }

        // Sẽ bổ sung lại sau khi có model Template
    }
}
