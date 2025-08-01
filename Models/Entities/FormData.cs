using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTOM.Models.Entities;

/// <summary>
/// Lưu dữ liệu form động do người dùng nhập. Thay thế cho ReportDataTransaction.
/// </summary>
[Table("FormDatas")]
public class FormData
{
    [Key]
    //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long FormDataID { get; set; }

    [Required]
    public int TemplateID { get; set; }

    [StringLength(20)]
    public string? SoCif { get; set; }

    [Required]
    [StringLength(256)]
    public string CreatedByUserName { get; set; } = string.Empty;

    [Required]
    public DateTime CreationTimestamp { get; set; } = DateTime.Now;

    public DateTime? LastModificationTimestamp { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    [StringLength(20)]
    public string? CreatedDepartmentID { get; set; }

    /// <summary>
    /// JSON lưu giá trị các trường form (key: fieldName, value: fieldValue)
    /// </summary>
    public string FormDataJson { get; set; } = string.Empty;
    public string? Status { get; set; } = string.Empty;

    // Navigation
    [ForeignKey("TemplateID")]
    public virtual Templates? Template { get; set; }

    [ForeignKey("CreatedByUserName")]
    public virtual ApplicationUser? CreatedByUser { get; set; }

    [ForeignKey("CreatedDepartmentID")]
    public virtual PhongBan? CreatedDepartment { get; set; }
}
