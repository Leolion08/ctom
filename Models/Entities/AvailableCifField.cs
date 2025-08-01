using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTOM.Models.Entities
{
    public class AvailableCifField
    {
        [Key]
        public int FieldID { get; set; }

        [Required]
        [StringLength(100)]
        public required string FieldName { get; set; }

        [StringLength(100)]
        public string? DisplayName { get; set; }

        [StringLength(50)]
        public string? DataType { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }
        
        [Required]
        public bool IsActive { get; set; } = true;

        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string FieldTagPrefix { get; set; } = "CIF";

        public int DisplayOrder { get; set; } = 0;
    }
}
