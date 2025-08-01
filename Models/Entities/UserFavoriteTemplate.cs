using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CTOM.Models.Entities
{
    public class UserFavoriteTemplate
    {
        [Key]
        public int FavoriteID { get; set; }

        [Required]
        [StringLength(450)]
        public required string UserID { get; set; }

        [Required]
        public int TemplateID { get; set; }

        public DateTime AddedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("TemplateID")]
        public virtual Templates? Template { get; set; }
    }
}
