using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTOM.Models.Entities
{
    [Table("Templates")]
    public class Templates
    {
        [Key]
        public int TemplateID { get; set; }

        [Required]
        [StringLength(255)]
        public required string TemplateName { get; set; }

        public int? BusinessOperationID { get; set; }

        [StringLength(255)]
        public string? OriginalUploadFileName { get; set; }

        [Required]
        public string OriginalDocxFilePath { get; set; } = string.Empty;

        [Required]
        public string MappedDocxFilePath { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        public string CreatedByUserName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? CreatedDepartmentID { get; set; }

        [Required]
        public DateTime CreationTimestamp { get; set; } = DateTime.Now;

        [StringLength(256)]
        public string? LastModifiedByUserName { get; set; }


        public DateTime? LastModificationTimestamp { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        [StringLength(50)]
        public string? Status { get; set; } = "Draft";

        [Required]
        [StringLength(50)]
        public string SharingType { get; set; } = "Private";

        public string? Description { get; set; }

        // Navigation properties
        [ForeignKey("BusinessOperationID")]
        public virtual BusinessOperation? BusinessOperation { get; set; }

        [ForeignKey("CreatedByUserName")]
        public virtual ApplicationUser? CreatedByUser { get; set; }
        [ForeignKey("LastModifiedByUserName")]
        public virtual ApplicationUser? LastModifiedByUser { get; set; }
        [ForeignKey("CreatedDepartmentID")]
        public virtual PhongBan? CreatedDepartment { get; set; }

        // Collection navigation properties
        public virtual ICollection<TemplateField>? TemplateFields { get; set; }
        public virtual ICollection<TemplateSharingRule>? TemplateSharingRules { get; set; }
        public virtual ICollection<UserFavoriteTemplate>? UserFavoriteTemplates { get; set; }
        public virtual ICollection<FormData>? FormDatas { get; set; }

    }
}
