using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace CTOM.Migrations
{
    /// <inheritdoc />
    public partial class SeedBusinessOperations : Migration
    {
        /// <summary>
        /// Thêm dữ liệu mẫu cho bảng BusinessOperations
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bước 1: Thêm các nghiệp vụ cấp 1 (ParentOperationID = NULL)
            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT [dbo].[BusinessOperations] ON;

                -- Thêm các nghiệp vụ cấp 1
                INSERT INTO [BusinessOperations] 
                    ([BusinessOperationID], [OperationName], [Description], [ParentOperationID], [CustomerType], [IsActive], [CreatedDate], [CreatedBy])
                VALUES
                    (1, N'Tín dụng (DN)', N'Nghiệp vụ tín dụng cho khách hàng doanh nghiệp', NULL, 'DN', 1, GETDATE(), 'System'),
                    (2, N'Tiền gửi (DN)', N'Nghiệp vụ tiền gửi cho khách hàng doanh nghiệp', NULL, 'DN', 1, GETDATE(), 'System'),
                    (3, N'Dịch vụ (DN)', N'Nghiệp vụ dịch vụ cho khách hàng doanh nghiệp', NULL, 'DN', 1, GETDATE(), 'System'),
                    (4, N'Nghiệp vụ cá nhân (CN)', N'Nghiệp vụ cho khách hàng cá nhân', NULL, 'CN', 1, GETDATE(), 'System');

                SET IDENTITY_INSERT [dbo].[BusinessOperations] OFF;
            ");

            // Bước 2: Thêm các nghiệp vụ cấp 2 (sau khi đã có ID của cấp 1)
            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT [dbo].[BusinessOperations] ON;

                -- Dưới Tín dụng (DN) - ID cha = 1
                INSERT INTO [BusinessOperations] 
                    ([BusinessOperationID], [OperationName], [Description], [ParentOperationID], [CustomerType], [IsActive], [CreatedDate], [CreatedBy])
                VALUES
                    (5, N'Định giá TSBĐ', N'Định giá tài sản bảo đảm', 1, 'DN', 1, GETDATE(), 'System'),
                    (6, N'Thế chấp TSBĐ', N'Thế chấp tài sản bảo đảm', 1, 'DN', 1, GETDATE(), 'System'),
                    (7, N'Mượn thay thế TSBĐ', N'Mượn thay thế tài sản bảo đảm', 1, 'DN', 1, GETDATE(), 'System'),
                    (8, N'Rút TSBĐ', N'Rút tài sản bảo đảm', 1, 'DN', 1, GETDATE(), 'System'),
                    (9, N'Hợp đồng bảo đảm', N'Hợp đồng bảo đảm tín dụng', 1, 'DN', 1, GETDATE(), 'System'),
                    (10, N'Hợp đồng tín dụng (DN)', N'Hợp đồng tín dụng doanh nghiệp', 1, 'DN', 1, GETDATE(), 'System'),
                    (11, N'Giải ngân theo HM', N'Giải ngân theo hạn mức tín dụng', 1, 'DN', 1, GETDATE(), 'System'),
                    (12, N'Bảo lãnh theo HM', N'Bảo lãnh theo hạn mức tín dụng', 1, 'DN', 1, GETDATE(), 'System'),
                    (13, N'LC theo HM', N'LC theo hạn mức tín dụng', 1, 'DN', 1, GETDATE(), 'System'),
                    (14, N'Giải ngân theo món', N'Giải ngân theo món tín dụng', 1, 'DN', 1, GETDATE(), 'System'),
                    (15, N'Bảo lãnh theo món', N'Bảo lãnh theo món tín dụng', 1, 'DN', 1, GETDATE(), 'System'),
                    (16, N'Biên bản kiểm tra', N'Biên bản kiểm tra tín dụng', 1, 'DN', 1, GETDATE(), 'System'),
                    (17, N'Thu nợ', N'Nghiệp vụ thu nợ', 1, 'DN', 1, GETDATE(), 'System'),
                    
                    -- Dưới Tiền gửi (DN) - ID cha = 2
                    (18, N'Hợp đồng tiền gửi', N'Hợp đồng tiền gửi cho khách hàng doanh nghiệp', 2, 'DN', 1, GETDATE(), 'System'),
                    
                    -- Dưới Dịch vụ (DN) - ID cha = 3
                    (19, N'Mở tài khoản (TK)', N'Mở tài khoản cho khách hàng doanh nghiệp', 3, 'DN', 1, GETDATE(), 'System'),
                    (20, N'iBank', N'Dịch vụ iBank cho khách hàng doanh nghiệp', 3, 'DN', 1, GETDATE(), 'System'),
                    (21, N'Đo lường', N'Dịch vụ đo lường cho khách hàng doanh nghiệp', 3, 'DN', 1, GETDATE(), 'System'),
                    (22, N'Giao dịch qua fax', N'Giao dịch qua fax cho khách hàng doanh nghiệp', 3, 'DN', 1, GETDATE(), 'System'),
                    
                    -- Dưới Nghiệp vụ cá nhân (CN) - ID cha = 4
                    (23, N'Hợp đồng tín dụng (CN)', N'Hợp đồng tín dụng cho khách hàng cá nhân', 4, 'CN', 1, GETDATE(), 'System'),
                    (24, N'Bảng kê rút vốn (CN)', N'Bảng kê rút vốn cho khách hàng cá nhân', 4, 'CN', 1, GETDATE(), 'System'),
                    (25, N'Ủy nhiệm chi (CN)', N'Ủy nhiệm chi cho khách hàng cá nhân', 4, 'CN', 1, GETDATE(), 'System'),
                    (26, N'Hợp đồng thế chấp (CN)', N'Hợp đồng thế chấp cho khách hàng cá nhân', 4, 'CN', 1, GETDATE(), 'System'),
                    (27, N'Biên bản định giá (CN)', N'Biên bản định giá cho khách hàng cá nhân', 4, 'CN', 1, GETDATE(), 'System'),
                    (28, N'Biên bản bàn giao (CN)', N'Biên bản bàn giao cho khách hàng cá nhân', 4, 'CN', 1, GETDATE(), 'System'),
                    (29, N'Thỏa thuận cam kết tài sản (CN)', N'Thỏa thuận cam kết tài sản cho khách hàng cá nhân', 4, 'CN', 1, GETDATE(), 'System'),
                    (30, N'Thỏa thuận chi hộ (CN)', N'Thỏa thuận chi hộ cho khách hàng cá nhân', 4, 'CN', 1, GETDATE(), 'System');

                SET IDENTITY_INSERT [dbo].[BusinessOperations] OFF;
            ");
        }

        /// <summary>
        /// Xóa dữ liệu mẫu đã thêm
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Xóa các nghiệp vụ cấp 2 (ID từ 5 đến 30)
                DELETE FROM [BusinessOperations] WHERE [BusinessOperationID] BETWEEN 5 AND 30;
                
                -- Xóa các nghiệp vụ cấp 1 (ID từ 1 đến 4)
                DELETE FROM [BusinessOperations] WHERE [BusinessOperationID] BETWEEN 1 AND 4;
            ");
        }
    }
}
