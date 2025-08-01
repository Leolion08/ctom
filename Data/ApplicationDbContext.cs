using System.Reflection.Emit;
using CTOM.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CTOM.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
    {
        // DbSet dùng tên class tiếng Việt
        public DbSet<PhongBan> PhongBans { get; set; } = null!;
        public DbSet<Templates> Templates { get; set; } = null!;
        public required DbSet<KhachHangDN> KhachHangDNs { get; set; }

        // Template Management
        public DbSet<BusinessOperation> BusinessOperations { get; set; } = null!;
        public DbSet<AvailableCifField> AvailableCifFields { get; set; } = null!;
        public DbSet<TemplateField> TemplateFields { get; set; } = null!;
        public DbSet<TemplateSharingRule> TemplateSharingRules { get; set; } = null!;
        public DbSet<UserFavoriteTemplate> UserFavoriteTemplates { get; set; } = null!;
        public DbSet<FormData> FormDatas { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // // Ignore legacy entities removed from schema
            // builder.Ignore<ReportDataTransaction>();
            // builder.Ignore<ReportDataTransactionField>();

            // --- Ánh xạ Entities tới tên bảng tiếng Việt ---
            builder.Entity<ApplicationUser>().ToTable("NguoiSuDung");
            builder.Entity<ApplicationRole>().ToTable("NhomQuyen");
            builder.Entity<IdentityUserRole<string>>().ToTable("NguoiSuDung_NhomQuyen");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

            builder.Entity<PhongBan>().ToTable("PhongBan");
            builder.Entity<Templates>().ToTable("Templates");
            builder.Entity<KhachHangDN>().ToTable("KhachHangDN");

            // Cấu hình BusinessOperation
            builder.Entity<BusinessOperation>(entity =>
            {
                entity.ToTable("BusinessOperations");

                // Đặt tên cột theo chuẩn database
                entity.Property(e => e.OperationID).HasColumnName("BusinessOperationID");

                // Cấu hình quan hệ tự tham chiếu (cây 2 cấp)
                entity.HasOne(e => e.ParentOperation)
                    .WithMany(e => e.ChildOperations)
                    .HasForeignKey(e => e.ParentOperationID)
                    .OnDelete(DeleteBehavior.Restrict); // Không xóa cascade để tránh xóa nhầm

                // Cấu hình unique constraint cho OperationName trong cùng cấp và cùng CustomerType
                entity.HasIndex(e => new { e.OperationName, e.ParentOperationID, e.CustomerType })
                    .IsUnique();

                // Giá trị mặc định
                entity.Property(e => e.CustomerType).HasDefaultValue("DN");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            });

            // Cấu hình KhachHangDN
            builder.Entity<KhachHangDN>(entity =>
            {
                // Khóa ngoại UserThucHienId -> ApplicationUser.UserName
                entity.HasOne(kh => kh.UserThucHien)
                      .WithMany()
                      .HasForeignKey(kh => kh.UserThucHienId)
                      .HasPrincipalKey(u => u.UserName)
                      .OnDelete(DeleteBehavior.Restrict);

                // Khóa ngoại PhongThucHien -> PhongBan.MaPhong
                entity.HasOne(kh => kh.PhongBanThucHien)
                      .WithMany()
                      .HasForeignKey(kh => kh.PhongThucHien)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // --- Cấu hình chi tiết ---

            // PhongBan
            builder.Entity<PhongBan>(entity =>
            {
                entity.HasKey(e => e.MaPhong); // PK là MaPhong ('DTTT', 'KHDN1'...)
                entity.Property(e => e.MaPhong).HasMaxLength(20);
                entity.Property(e => e.MaPhongHR).HasMaxLength(20).IsRequired(false); // Nullable Y
                entity.Property(e => e.TenPhong).HasMaxLength(50).IsRequired(); // Nullable N
                entity.Property(e => e.TenVietTat).HasMaxLength(20).IsRequired(false); // Nullable Y
                entity.Property(e => e.TrangThai).HasMaxLength(1).IsFixedLength().IsRequired().HasDefaultValue("A"); // Nullable Y, Default A -> Ưu tiên NOT NULL
            });

            // ApplicationUser (NguoiSuDung)
            builder.Entity<ApplicationUser>(entity => {
                entity.Property(e => e.TenUser).HasMaxLength(50).IsRequired(); // Nullable N
                entity.Property(e => e.MaPhong).HasMaxLength(20).IsRequired(); // Nullable N, FK
                entity.Property(e => e.TrangThai).HasMaxLength(1).IsFixedLength().IsRequired().HasDefaultValue("A"); // Nullable Y, Default A -> Ưu tiên NOT NULL

                // --- FK ---
                entity.HasOne(e => e.PhongBan)
                      .WithMany(d => d.NguoiSuDungs)
                      .HasForeignKey(e => e.MaPhong)
                      .OnDelete(DeleteBehavior.Restrict); // Essential FK
            });

            // ApplicationRole (NhomQuyen)
            builder.Entity<ApplicationRole>(entity => {
                // Identity quản lý PK (Id), Name (dùng làm MaNhom)
                entity.Property(e => e.TenNhomDayDu).HasMaxLength(50).IsRequired(false); // Tên hiển thị, Nullable Y
                entity.Property(e => e.TrangThai).HasMaxLength(1).IsFixedLength().IsRequired().HasDefaultValue("A"); // Nullable Y, Default A -> Ưu tiên NOT NULL
            });

            // Templates
            builder.Entity<Templates>(entity =>
            {
                entity.HasKey(e => e.TemplateID); // PK Identity, NOT NULL

                // Cấu hình các thuộc tính
                entity.Property(e => e.TemplateName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.OriginalUploadFileName).HasMaxLength(255);
                entity.Property(e => e.OriginalDocxFilePath).IsRequired();
                entity.Property(e => e.MappedDocxFilePath).IsRequired();
                entity.Property(e => e.CreatedByUserName).HasMaxLength(256).IsRequired();
                entity.Property(e => e.CreatedDepartmentID).HasMaxLength(20);
                entity.Property(e => e.CreationTimestamp).IsRequired().HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.LastModifiedByUserName).HasMaxLength(256);
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
                entity.Property(e => e.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Draft");
                entity.Property(e => e.SharingType).HasMaxLength(50).IsRequired().HasDefaultValue("Private");
                entity.Property(e => e.Description).HasMaxLength(1000);

                // Cấu hình các quan hệ
                entity.HasOne(t => t.BusinessOperation)
                    .WithMany()
                    .HasForeignKey(t => t.BusinessOperationID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(t => t.CreatedByUserName)
                    .HasPrincipalKey(u => u.UserName)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.LastModifiedByUser)
                    .WithMany()
                    .HasForeignKey(t => t.LastModifiedByUserName)
                    .HasPrincipalKey(u => u.UserName)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.CreatedDepartment)
                    .WithMany()
                    .HasForeignKey(t => t.CreatedDepartmentID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Cấu hình BusinessOperations
            builder.Entity<BusinessOperation>(entity =>
            {
                entity.HasKey(e => e.OperationID);
                entity.Property(e => e.OperationName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            });

            // Cấu hình AvailableCifField
            builder.Entity<AvailableCifField>(entity =>
            {
                entity.HasKey(e => e.FieldID);
                entity.Property(e => e.FieldName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.DisplayName).HasMaxLength(100);
                entity.Property(e => e.DataType).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            });

            // Cấu hình TemplateField
            builder.Entity<TemplateField>(entity =>
            {
                entity.HasKey(e => e.TemplateFieldID);
                entity.Property(e => e.FieldName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.DisplayName).HasMaxLength(100);
                entity.Property(e => e.DataType).HasMaxLength(50);
                entity.Property(e => e.DefaultValue).HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.HasOne(tf => tf.Template)
                    .WithMany(t => t.TemplateFields)
                    .HasForeignKey(tf => tf.TemplateID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình TemplateSharingRule
            builder.Entity<TemplateSharingRule>(entity =>
            {
                entity.HasKey(e => e.SharingRuleID);
                entity.Property(e => e.SharingType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.TargetID).HasMaxLength(450);
                entity.Property(e => e.TargetName).HasMaxLength(100);
                entity.Property(e => e.PermissionLevel).HasMaxLength(50);

                // Sửa lại tên thuộc tính navigation từ SharingRules thành TemplateSharingRules
                entity.HasOne(tsr => tsr.Template)
                    .WithMany(t => t.TemplateSharingRules)
                    .HasForeignKey(tsr => tsr.TemplateID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình UserFavoriteTemplate
            builder.Entity<UserFavoriteTemplate>(entity =>
            {
                entity.HasKey(e => e.FavoriteID);
                entity.HasOne(uft => uft.User)
                    .WithMany()
                    .HasForeignKey(uft => uft.UserID)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(uft => uft.Template)
                    .WithMany()
                    .HasForeignKey(uft => uft.TemplateID)
                    .OnDelete(DeleteBehavior.Cascade);
            });





            // Cấu hình FormData (FormDatas)
            builder.Entity<FormData>(entity =>
            {
                entity.HasKey(e => e.FormDataID);

                entity.Property(e => e.SoCif).HasMaxLength(20);
                entity.Property(e => e.CreatedByUserName).HasMaxLength(256).IsRequired();
                entity.Property(e => e.FormDataJson);
                entity.Property(e => e.Note).HasMaxLength(1000);
                entity.Property(e => e.CreatedDepartmentID).HasMaxLength(20);

                // Quan hệ với Template
                entity.HasOne(fd => fd.Template)
                      .WithMany(t => t.FormDatas)
                      .HasForeignKey(fd => fd.TemplateID)
                      .OnDelete(DeleteBehavior.Restrict);

                // Quan hệ với người tạo (ApplicationUser)
                entity.HasOne(fd => fd.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(fd => fd.CreatedByUserName)
                      .HasPrincipalKey(u => u.UserName)
                      .OnDelete(DeleteBehavior.Restrict);

                // Quan hệ với Phòng ban tạo (PhongBan)
                entity.HasOne(fd => fd.CreatedDepartment)
                      .WithMany()
                      .HasForeignKey(fd => fd.CreatedDepartmentID)
                      .HasPrincipalKey(p => p.MaPhong)
                      .OnDelete(DeleteBehavior.Restrict);

                // Indexes
                entity.HasIndex(e => e.TemplateID);
                entity.HasIndex(e => e.CreatedByUserName);
                entity.HasIndex(e => e.SoCif);
                entity.HasIndex(e => e.CreatedDepartmentID);
            });

            // === CẤU HÌNH CHO KHACHHANGDN ===
            builder.Entity<KhachHangDN>(entity =>
            {
                // Index Unique cho SoCif
                entity.HasIndex(e => e.SoCif, "IX_KhachHangDN_SoCIF").IsUnique();

                // Default Value cho NgayCapNhatDuLieu
                entity.Property(e => e.NgayCapNhatDuLieu)
                      .HasDefaultValueSql("GETDATE()");
                      //.ValueGeneratedOnAddOrUpdate(); // Nếu có,  EF sẽ không đưa NgayCapNhatDuLieu vào mệnh đề SET trong câu lệnh UPDATE mà nó tạo ra

                // Chỉ định kiểu cột Date/DateTime
                entity.Property(e => e.NgayCapGiayChungNhanDKKD).HasColumnType("date");
                entity.Property(e => e.NgaySinhDaiDienCongTy).HasColumnType("date");
                entity.Property(e => e.NgayCapGiayToTuyThanDaiDienDN).HasColumnType("date");
                entity.Property(e => e.NgayHetHanGiayToTuyThanDaiDienDN).HasColumnType("date");
                entity.Property(e => e.NgaySinhKeToanTruong).HasColumnType("date");
                entity.Property(e => e.NgayCapGiayToTuyThanKeToanTruong).HasColumnType("date");
                entity.Property(e => e.NgayHetHanGiayToTuyThanKeToanTruong).HasColumnType("date");

                // Khóa ngoại UserThucHienId -> ApplicationUser.UserName
                entity.HasOne(kh => kh.UserThucHien)
                      .WithMany()
                      .HasForeignKey(kh => kh.UserThucHienId)
                      .HasPrincipalKey(u => u.UserName)
                      .OnDelete(DeleteBehavior.Restrict);

                // Khóa ngoại PhongThucHien -> PhongBan.MaPhong
                entity.HasOne(kh => kh.PhongBanThucHien)
                      .WithMany()
                      .HasForeignKey(kh => kh.PhongThucHien)
                      .HasPrincipalKey(p => p.MaPhong)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            // ===============================

            // --- Seed Data cho Roles và Departments bằng HasData ---
            SeedRolesAndDepartments(builder);
        }

        // Phương thức riêng để seed Roles và Departments
        private static void SeedRolesAndDepartments(ModelBuilder builder)
        {
            // Seed Roles (dùng MaNhom làm Name)
            string adminRoleId = "d1b5952a-2162-46c7-b29e-1a2a68922c14";
            string httdRoleId = "e2b6952b-3163-47c8-b39f-2b3b78923d15";
            string vantinRoleId = "f3b7953c-4164-48d9-b49a-3c4c89924e16";

            builder.Entity<ApplicationRole>().HasData(
                new ApplicationRole { Id = adminRoleId, Name = "ADMIN", NormalizedName = "ADMIN", TenNhomDayDu = "Quản trị chương trình", TrangThai = "A" },
                new ApplicationRole { Id = httdRoleId, Name = "HTTD", NormalizedName = "HTTD", TenNhomDayDu = "Hỗ trợ tín dụng", TrangThai = "A" },
                new ApplicationRole { Id = vantinRoleId, Name = "VANTIN", NormalizedName = "VANTIN", TenNhomDayDu = "Vấn tin báo cáo", TrangThai = "A" }
            );

            // Seed Departments (dùng MaPhong từ seed data làm PK)
            builder.Entity<PhongBan>().HasData(
               new PhongBan { MaPhong = "DTTT", MaPhongHR = "BIDV160A108", TenPhong = "P. Đào tạo và Quản lý thông tin", TenVietTat = "ĐT&QLTT", TrangThai = "A" },
               new PhongBan { MaPhong = "KHDN1", MaPhongHR = "BIDV160A205", TenPhong = "P. Khách hàng Doanh nghiệp 1", TenVietTat = "KHDN 1", TrangThai = "A" },
               new PhongBan { MaPhong = "KHDN2", MaPhongHR = "BIDV160A206", TenPhong = "P. Khách hàng Doanh nghiệp 2", TenVietTat = "KHDN 2", TrangThai = "A" }
            );
        }
    }
}
