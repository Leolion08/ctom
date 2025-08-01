using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTOM.Models.Entities
{
    public class TemplateSharingRule
    {
        [Key]
        public int SharingRuleID { get; set; }

        [Required]
        public int TemplateID { get; set; }

        [Required]
        [StringLength(50)]
        public required string SharingType { get; set; } // 'User', 'Role', 'Department', 'AllUsers'

        [StringLength(450)]
        public string? TargetID { get; set; } // UserID, RoleID, or DepartmentCode

        [StringLength(100)]
        public string? TargetName { get; set; } // For display purposes

        [StringLength(50)]
        public string? PermissionLevel { get; set; } // 'Read', 'Edit', 'FullControl'


        // Navigation property
        [ForeignKey("TemplateID")]
        public virtual Templates? Template { get; set; }
    }
}
