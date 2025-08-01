using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CTOM.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchemaWithUserNameReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NhomQuyen",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TrangThai = table.Column<string>(type: "nchar(1)", fixedLength: true, maxLength: 1, nullable: false, defaultValue: "A"),
                    TenNhomDayDu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhomQuyen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhongBan",
                columns: table => new
                {
                    MaPhong = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaPhongHR = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TenPhong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenVietTat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TrangThai = table.Column<string>(type: "nchar(1)", fixedLength: true, maxLength: 1, nullable: false, defaultValue: "A")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhongBan", x => x.MaPhong);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    TemplateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.TemplateID);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_NhomQuyen_RoleId",
                        column: x => x.RoleId,
                        principalTable: "NhomQuyen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NguoiSuDung",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaPhong = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TrangThai = table.Column<string>(type: "nchar(1)", fixedLength: true, maxLength: 1, nullable: false, defaultValue: "A"),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiSuDung", x => x.Id);
                    table.UniqueConstraint("AK_NguoiSuDung_UserName", x => x.UserName);
                    table.ForeignKey(
                        name: "FK_NguoiSuDung_PhongBan_MaPhong",
                        column: x => x.MaPhong,
                        principalTable: "PhongBan",
                        principalColumn: "MaPhong",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KhachHangDN",
                columns: table => new
                {
                    SoCif = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenCif = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    XepHangTinDungNoiBo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LoaiHinhDN = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SoGiayChungNhanDKKD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NoiCapGiayChungNhanDKKD = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NgayCapGiayChungNhanDKKD = table.Column<DateTime>(type: "date", nullable: true),
                    TenTiengAnh = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DiaChiTrenDKKD = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DiaChiKinhDoanhHienTai = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LinhVucKinhDoanhChinh = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SoDienThoaiDN = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SoFaxCongTy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EmailCongTy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TenNguoiDaiDienTheoPhapLuat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChucVu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgaySinhDaiDienCongTy = table.Column<DateTime>(type: "date", nullable: true),
                    GioiTinhDaiDienCongTy = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    QuocTichDaiDienCongTy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SoGiayToTuyThanDaiDienCongTy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NgayCapGiayToTuyThanDaiDienDN = table.Column<DateTime>(type: "date", nullable: true),
                    NoiCapGiayToTuyThanDaiDienDN = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NgayHetHanGiayToTuyThanDaiDienDN = table.Column<DateTime>(type: "date", nullable: true),
                    DiaChiDaiDienCongTy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SoDienThoaiDaiDienCongTy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EmailDaiDienCongTy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VanBanUyQuyenSo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TenKeToanTruong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChucVuKeToanTruong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgaySinhKeToanTruong = table.Column<DateTime>(type: "date", nullable: true),
                    GioiTinhKeToanTruong = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    QuocTichKeToanTruong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SoGiayToTuyThanKeToanTruong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NgayCapGiayToTuyThanKeToanTruong = table.Column<DateTime>(type: "date", nullable: true),
                    NoiCapGiayToTuyThanKeToanTruong = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NgayHetHanGiayToTuyThanKeToanTruong = table.Column<DateTime>(type: "date", nullable: true),
                    SoDienThoaiKeToanTruong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EmailKeToanTruong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DiaChiKeToanTruong = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserThucHienId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PhongThucHien = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NgayCapNhatDuLieu = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhachHangDN", x => x.SoCif);
                    table.ForeignKey(
                        name: "FK_KhachHangDN_NguoiSuDung_UserThucHienId",
                        column: x => x.UserThucHienId,
                        principalTable: "NguoiSuDung",
                        principalColumn: "UserName",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KhachHangDN_PhongBan_PhongThucHien",
                        column: x => x.PhongThucHien,
                        principalTable: "PhongBan",
                        principalColumn: "MaPhong",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NguoiSuDung_NhomQuyen",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiSuDung_NhomQuyen", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_NguoiSuDung_NhomQuyen_NguoiSuDung_UserId",
                        column: x => x.UserId,
                        principalTable: "NguoiSuDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NguoiSuDung_NhomQuyen_NhomQuyen_RoleId",
                        column: x => x.RoleId,
                        principalTable: "NhomQuyen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_NguoiSuDung_UserId",
                        column: x => x.UserId,
                        principalTable: "NguoiSuDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_NguoiSuDung_UserId",
                        column: x => x.UserId,
                        principalTable: "NguoiSuDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_NguoiSuDung_UserId",
                        column: x => x.UserId,
                        principalTable: "NguoiSuDung",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "NhomQuyen",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName", "TenNhomDayDu", "TrangThai" },
                values: new object[,]
                {
                    { "d1b5952a-2162-46c7-b29e-1a2a68922c14", null, "ADMIN", "ADMIN", "Quản trị chương trình", "A" },
                    { "e2b6952b-3163-47c8-b39f-2b3b78923d15", null, "HTTD", "HTTD", "Hỗ trợ tín dụng", "A" },
                    { "f3b7953c-4164-48d9-b49a-3c4c89924e16", null, "VANTIN", "VANTIN", "Vấn tin báo cáo", "A" }
                });

            migrationBuilder.InsertData(
                table: "PhongBan",
                columns: new[] { "MaPhong", "MaPhongHR", "TenPhong", "TenVietTat", "TrangThai" },
                values: new object[,]
                {
                    { "DTTT", "BIDV160A108", "P. Đào tạo và Quản lý thông tin", "ĐT&QLTT", "A" },
                    { "KHDN1", "BIDV160A205", "P. Khách hàng Doanh nghiệp 1", "KHDN 1", "A" },
                    { "KHDN2", "BIDV160A206", "P. Khách hàng Doanh nghiệp 2", "KHDN 2", "A" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_KhachHangDN_PhongThucHien",
                table: "KhachHangDN",
                column: "PhongThucHien");

            migrationBuilder.CreateIndex(
                name: "IX_KhachHangDN_SoCIF",
                table: "KhachHangDN",
                column: "SoCif",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KhachHangDN_UserThucHienId",
                table: "KhachHangDN",
                column: "UserThucHienId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "NguoiSuDung",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiSuDung_MaPhong",
                table: "NguoiSuDung",
                column: "MaPhong");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "NguoiSuDung",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiSuDung_NhomQuyen_RoleId",
                table: "NguoiSuDung_NhomQuyen",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "NhomQuyen",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KhachHangDN");

            migrationBuilder.DropTable(
                name: "NguoiSuDung_NhomQuyen");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "Templates");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "NhomQuyen");

            migrationBuilder.DropTable(
                name: "NguoiSuDung");

            migrationBuilder.DropTable(
                name: "PhongBan");
        }
    }
}
