using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CTOM.Models.Entities
{
    public class ApplicationRole : IdentityRole // PK Id kiểu string (GUID)
    {
        [Required] // Nên NOT NULL theo seed data
        [StringLength(1, MinimumLength = 1)]
        public string TrangThai { get; set; } = "A"; // Từ NhomQuyen.docx

        [StringLength(50)]
        public string? TenNhomDayDu { get; set; } // Tương ứng TenNhom từ NhomQuyen.docx

        // Constructors
        public ApplicationRole() : base() { }
        public ApplicationRole(string roleName) : base(roleName) { } // roleName sẽ là MaNhom ('ADMIN', 'HTTD')
    }
}