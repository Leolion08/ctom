using CTOM.Models.Config;
using CTOM.Models.Responses;
using CTOM.Models.DTOs;
using CTOM.Models.Entities;
using CTOM.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Linq;
using CTOM.Models;
using ExcelDataReader;
using System.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CTOM.Services;

public class ExcelImportService
{
    private readonly ExcelImportConfig _config;
    private readonly ILogger<ExcelImportService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;

    // Public properties to access configuration
    public double MaxFileSizeMB => _config.MaxFileSizeMB;
    public int MaxRows => _config.MaxRows;

    public ExcelImportService(
        IOptions<ExcelImportConfig> config,
        ILogger<ExcelImportService> logger,
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));

        _logger.LogDebug("Đã khởi tạo ExcelImportService với {Count} trường cấu hình", _config.Fields?.Count ?? 0);
    }

    public async Task<ImportResponse> ImportFromExcel(Stream fileStream, string? sheetName = null, bool hasHeader = true, Dictionary<string, FieldConfig>? fieldConfigs = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var importErrors = new List<ImportError>();
        var importedData = new List<object>();
        int totalRows = 0;

        // Validate file size
        if (fileStream.Length > _config.MaxFileSizeMB * 1024 * 1024)
        {
            return new ImportResponse
            {
                Success = false,
                Message = $"Kích thước file vượt quá giới hạn cho phép ({_config.MaxFileSizeMB}MB)"
            };
        }

        // Sử dụng cấu hình từ tham số hoặc từ _config
        var configToUse = fieldConfigs ?? _config.Fields;

        try
        {
            // 1. Đọc file Excel thành DataSet
            using var reader = ExcelReaderFactory.CreateReader(fileStream);
            var configuration = new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = hasHeader
                }
            };
            var dataSet = reader.AsDataSet(configuration);

            // 2. Kiểm tra sheet tồn tại
            // Kiểm tra null cho dataSet và Tables
            if (dataSet?.Tables == null || dataSet.Tables.Count == 0)
            {
                return new ImportResponse
                {
                    Message = "Không tìm thấy bảng dữ liệu nào trong file Excel"
                };
            }

            var tableName = string.IsNullOrEmpty(sheetName) ? dataSet.Tables[0]?.TableName : sheetName;
            if (string.IsNullOrEmpty(tableName) || !dataSet.Tables.Contains(tableName))
            {
                return new ImportResponse
                {
                    Message = $"Không tìm thấy sheet '{tableName}' trong file Excel"
                };
            }

            var dataTable = dataSet.Tables[tableName];
            if (dataTable?.Rows == null)
            {
                return new ImportResponse
                {
                    Message = "Không thể đọc dữ liệu từ bảng"
                };
            }

            totalRows = dataTable.Rows.Count;

            if (totalRows == 0)
            {
                return new ImportResponse
                {
                    Success = false,
                    Message = "Không có dữ liệu để import"
                };
            }

            // Kiểm tra số dòng vượt quá giới hạn
            if (_config.MaxRows > 0 && totalRows > _config.MaxRows)
            {
                var errorMessage = $"File có {totalRows} dòng dữ liệu. Vượt quá giới hạn cho phép ({_config.MaxRows} dòng)";
                var errors = new List<ImportError>
                {
                    new()
                    {
                        RowNumber = 0,
                        ColumnName = "Hệ thống",
                        ErrorMessage = errorMessage,
                        Severity = ErrorSeverity.Error
                    }
                };
                return ImportResponse.CreateErrorResponse(errorMessage, errors);
            }

            // 3. Kiểm tra các cột bắt buộc
            var requiredFields = _config.Fields
                .Where(f => f.Value.Required && !string.IsNullOrEmpty(f.Value.Label))
                .ToDictionary(f => f.Value.Label.Trim(), f => f.Key);

            // Lấy danh sách tên cột từ Excel (giữ nguyên cả khoảng trắng để kiểm tra)
            var excelColumns = dataTable.Columns.Cast<DataColumn>()
                .Select(c => new
                {
                    OriginalName = c.ColumnName?.ToString() ?? string.Empty,
                    TrimmedName = c.ColumnName?.ToString()?.Trim() ?? string.Empty
                })
                .ToList();

            // Kiểm tra tất cả các cột trong Excel xem có cột nào có khoảng trắng thừa không
            foreach (var column in excelColumns)
            {
                if (column.OriginalName != column.TrimmedName)
                {
                    const string errorTemplate = "Tên cột không được chứa khoảng trắng thừa: '{OriginalName}'";
                    var errorMessage = $"Tên cột không được chứa khoảng trắng thừa: '{column.OriginalName}'";
                    _logger.LogError(errorTemplate, column.OriginalName);
                    return ImportResponse.CreateErrorResponse(errorMessage,
                    [
                        new()
                        {
                            RowNumber = 0,
                            ColumnName = column.OriginalName,
                            ErrorMessage = errorMessage,
                            Severity = ErrorSeverity.Error
                        }
                    ]);
                }
            }

            // Kiểm tra các cột bắt buộc
            var missingRequiredColumns = new List<string>();
            foreach (var field in requiredFields)
            {
                var fieldLabel = field.Key;
                var fieldName = field.Value;

                // Tìm cột phù hợp (không phân biệt hoa thường)
                var columnExists = excelColumns.Any(col =>
                    string.Equals(col.TrimmedName, fieldLabel, StringComparison.OrdinalIgnoreCase));

                if (!columnExists)
                {
                    const string errorTemplate = "Thiếu cột bắt buộc: '{FieldLabel}'";
                    var errorMessage = $"Thiếu cột bắt buộc: '{fieldLabel}'";
                    _logger.LogError(errorTemplate, fieldLabel);
                    return ImportResponse.CreateErrorResponse(errorMessage,
                    [
                        new()
                        {
                            RowNumber = 0,
                            ColumnName = fieldName,
                            ErrorMessage = errorMessage,
                            Severity = ErrorSeverity.Error
                        }
                    ]);
                }
            }

            // 4. Lấy thông tin người dùng hiện tại
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("Không thể lấy HttpContext, sử dụng ImportContext mặc định");
                var contextErrors = new List<ImportError>
                {
                    new()
                    {
                        RowNumber = 0,
                        ColumnName = "Hệ thống",
                        ErrorMessage = "Không thể xác định ngữ cảnh người dùng",
                        Severity = ErrorSeverity.Error
                    }
                };
                return ImportResponse.CreateErrorResponse("Lỗi hệ thống: Không thể xác định ngữ cảnh người dùng", contextErrors);
            }

            var importContext = await ImportContext.FromHttpContextAsync(httpContext, _userManager) ?? new ImportContext();
            // 4. Xử lý từng dòng dữ liệu

            var importedCount = 0;
            var updatedCount = 0;
            var processedRows = 0;

            // Bắt đầu transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Xử lý từng dòng dữ liệu
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    var row = dataTable.Rows[i];
                    processedRows++;
                    var rowNumber = i + (hasHeader ? 2 : 1);

                    try
                    {
                        // Bỏ qua dòng trống
                        if (row.ItemArray.All(c => c == null || string.IsNullOrWhiteSpace(c.ToString())))
                        {

                            continue;
                        }

                        // Map dữ liệu từ DataRow sang entity
                        var entity = MapDataRowToKhachHangDN(row, importContext);
                        if (entity == null)
                        {
                            importErrors.Add(new ImportError
                            {
                                RowNumber = rowNumber,
                                ColumnName = "Hệ thống",
                                ErrorMessage = "Không thể ánh xạ dữ liệu",
                                Severity = ErrorSeverity.Error
                            });
                            continue;
                        }

                        // Kiểm tra khách hàng đã tồn tại chưa
                        var existingKhachHang = await _context.KhachHangDNs
                            .FirstOrDefaultAsync(kh => kh.SoCif == entity.SoCif);

                        if (existingKhachHang != null)
                        {
                            // Lấy danh sách các thuộc tính từ cấu hình
                            var properties = typeof(KhachHangDN).GetProperties()
                                .Where(p => _config.Fields.ContainsKey(p.Name) && p.CanWrite)
                                .ToList();

                            // Cập nhật từng thuộc tính dựa trên cấu hình
                            foreach (var prop in properties)
                            {
                                try
                                {
                                    var value = prop.GetValue(entity);
                                    prop.SetValue(existingKhachHang, value);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Lỗi khi cập nhật thuộc tính {PropertyName}", prop.Name);
                                }
                            }

                            // Cập nhật thông tin người thực hiện
                            existingKhachHang.UserThucHienId = importContext.UserName;
                            existingKhachHang.PhongThucHien = importContext.MaPhong;
                            existingKhachHang.NgayCapNhatDuLieu = DateTime.Now;

                            _context.KhachHangDNs.Update(existingKhachHang);
                            updatedCount++;
                        }
                        else
                        {
                            // Thêm mới
                            entity.UserThucHienId = importContext.UserName;
                            entity.PhongThucHien = importContext.MaPhong;
                            entity.NgayCapNhatDuLieu = DateTime.Now;

                            await _context.KhachHangDNs.AddAsync(entity);
                            importedCount++;
                        }

                        // Lưu thay đổi sau mỗi 100 bản ghi để tránh quá tải
                        if (processedRows % 100 == 0)
                        {
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi xử lý dòng {RowNumber}", rowNumber);
                        importErrors.Add(new ImportError
                        {
                            RowNumber = rowNumber,
                            ColumnName = "Hệ thống",
                            //ErrorMessage = $"Lỗi khi xử lý dữ liệu: {ex.Message}",
                            ErrorMessage = ex.Message,
                            Severity = ErrorSeverity.Error
                        });
                    }
                }

                // Lưu các thay đổi còn lại
                await _context.SaveChangesAsync();

                // Nếu có lỗi, rollback transaction
                if (importErrors.Count > 0)
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning("Đã hủy import do có {ErrorCount} lỗi xảy ra", importErrors.Count);

                    return new ImportResponse
                    {
                        TotalRows = totalRows,
                        SuccessCount = 0,
                        Errors = importErrors,
                        Message = $"Có {importErrors.Count} lỗi xảy ra khi import dữ liệu"
                    };
                }

                // Nếu không có lỗi, commit transaction
                await transaction.CommitAsync();

                _logger.LogInformation("Import thành công: {Imported} bản ghi mới, {Updated} bản ghi được cập nhật",
                    importedCount, updatedCount);

                return new ImportResponse
                {
                    TotalRows = totalRows,
                    SuccessCount = importedCount + updatedCount,
                    //Message = $"Đã import thành công {importedCount} bản ghi và cập nhật {updatedCount} bản ghi"
                    Message = $"Đã cập nhật thành công {importedCount + updatedCount} bản ghi."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xử lý import dữ liệu");

                return new ImportResponse
                {
                    TotalRows = totalRows,
                    SuccessCount = 0,
                    Errors =
                    [
                        new()
                        {
                            RowNumber = 0,
                            ColumnName = "",
                            ErrorMessage = ex.Message,
                            Severity = ErrorSeverity.Error
                        }
                    ],
                    Message = "Có lỗi xảy ra khi xử lý dữ liệu"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi import dữ liệu từ Excel");
            var importError = new ImportError
            {
                RowNumber = 1,
                ColumnName = "Hệ thống",
                ErrorMessage = ex.Message,
                Severity = ErrorSeverity.Error
            };
            return ImportResponse.CreateErrorResponse("Có lỗi xảy ra khi import dữ liệu", [importError]);
        }
    }


    /// <summary>
    /// Ánh xạ dữ liệu từ DataRow sang đối tượng KhachHangDN
    /// </summary>
    /// <param name="row">Dòng dữ liệu cần ánh xạ</param>
    /// <param name="context">Ngữ cảnh import dữ liệu</param>
    /// <returns>Đối tượng KhachHangDN đã được ánh xạ</returns>
    private KhachHangDN MapDataRowToKhachHangDN(DataRow row, ImportContext context)
    {
        var khachHang = new KhachHangDN();
        var missingRequiredFields = new List<string>();

        // Tạo từ điển ánh xạ giữa tên cột Excel và tên trường trong DB
        var excelToDbMap = new Dictionary<string, (string FieldName, FieldConfig Config)>(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("=== BẮT ĐẦU TẠO MAPPING TỪ CẤU HÌNH ===");
        // Tạo map từ tên cột Excel sang tên trường trong đối tượng
        if (_config?.Fields != null)
        {
            foreach (var field in _config.Fields)
            {
                if (field.Value == null) continue;

                var fieldName = field.Value.Field ?? field.Key; // Sử dụng Field nếu có, không thì dùng key
                var fieldLabel = field.Value.Label?.Trim() ?? fieldName;

                if (string.IsNullOrEmpty(fieldLabel))
                {
                    continue;
                }

                excelToDbMap[fieldLabel] = (fieldName, field.Value);
            }
        }

        // Xử lý từng cột trong DataRow

        foreach (DataColumn column in row.Table.Columns)
        {
            var columnName = column.ColumnName?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(columnName))
            {

                continue;
            }

            // Lấy giá trị thô từ DataRow
            var rawValue = row[column];
            var displayValue = rawValue?.ToString()?.Trim() ?? "(null)";

            // Chỉ log thông tin chi tiết ở mức Debug
            _logger.LogDebug("Đang xử lý cột: '{ColumnName}'", columnName);

            // Tìm field config tương ứng với tên cột (phân biệt hoa thường)
            var fieldMapping = excelToDbMap.FirstOrDefault(x =>
                string.Equals(x.Key, columnName, StringComparison.Ordinal));

            if (fieldMapping.Key == null)
            {
                // Nếu không tìm thấy, thử tìm không phân biệt hoa thường
                fieldMapping = excelToDbMap.FirstOrDefault(x =>
                    string.Equals(x.Key, columnName, StringComparison.OrdinalIgnoreCase));

                if (fieldMapping.Key == null)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Không tìm thấy mapping cho cột: '{ColumnName}'", columnName);
                    }
                    continue;
                }

                _logger.LogDebug("Đã tìm thấy mapping (không phân biệt hoa thường): '{ColumnName}' -> {FieldName}",
                    columnName, fieldMapping.Value.FieldName);
            }

            var fieldName = fieldMapping.Value.FieldName;
            var fieldConfig = fieldMapping.Value.Config;
            var fieldValue = displayValue == "(null)" ? string.Empty : displayValue;

            _logger.LogDebug("Đã ánh xạ: '{ColumnName}' -> {FieldName}", columnName, fieldName);

            // Xử lý đặc biệt cho trường SoCif
            if (fieldName.Equals("SoCif", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(fieldValue))
                    {
                        var cleanValue = fieldValue.Trim();
                        khachHang.SoCif = cleanValue;
                        _logger.LogDebug("Đã gán SoCif: {Value}", cleanValue);
                    }
                    else if (fieldConfig.Required)
                    {
                        _logger.LogWarning("Bỏ qua giá trị SoCif trống (trường bắt buộc)");
                        missingRequiredFields.Add($"{columnName}"); // SoCif
                    }
                    else
                    {
                        _logger.LogDebug("Bỏ qua giá trị SoCif trống (không bắt buộc)");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý trường SoCif");
                    if (fieldConfig.Required)
                    {
                        missingRequiredFields.Add($"{columnName}"); // SoCif
                    }
                }
                continue;
            }

            // Xử lý các trường khác
            _logger.LogDebug("Xử lý: {ColumnName} -> {FieldName}", columnName, fieldName);

            // Kiểm tra trường bắt buộc
            if (fieldConfig.Required && string.IsNullOrEmpty(fieldValue))
            {
                _logger.LogWarning("Thiếu giá trị cho trường bắt buộc: '{ColumnName}'", columnName);
                missingRequiredFields.Add(columnName);
                continue;
            }

            try
            {
                // Bỏ qua nếu không có giá trị
                if (string.IsNullOrEmpty(fieldValue)) continue;

                // Sử dụng reflection để gán giá trị cho thuộc tính tương ứng
                var property = typeof(KhachHangDN).GetProperty(fieldName, System.Reflection.BindingFlags.IgnoreCase |
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (property == null)
                {
                    _logger.LogWarning("Không tìm thấy thuộc tính {FieldName} trong đối tượng KhachHangDN", fieldName);
                    continue;
                }

                object? convertedValue = null;
                var conversionSucceeded = true;

                // Xử lý theo kiểu dữ liệu
                if (property.PropertyType == typeof(string))
                {
                    // Kiểm tra độ dài tối đa cho chuỗi
                    var maxLength = fieldConfig.MaxLength ?? 0;
                    if (maxLength > 0 && fieldValue.Length > maxLength)
                    {
                        _logger.LogWarning("Giá trị vượt quá độ dài tối đa {MaxLength} ký tự (trường {FieldName})",
                            maxLength, fieldName);

                        // Cắt bớt nếu vượt quá độ dài tối đa
                        convertedValue = fieldValue[..maxLength];
                    }
                    else
                    {
                        convertedValue = fieldValue;
                    }
                }
                else if (property.PropertyType == typeof(int?) || property.PropertyType == typeof(int))
                {
                    if (int.TryParse(fieldValue, out int intValue))
                    {
                        convertedValue = intValue;
                    }
                    else if (fieldConfig.Required)
                    {
                        _logger.LogWarning("Giá trị '{FieldValue}' không hợp lệ cho số nguyên (trường {FieldName})",
                            fieldValue, fieldName);
                        conversionSucceeded = false;
                    }
                }
                else if (property.PropertyType == typeof(DateTime?) || property.PropertyType == typeof(DateTime))
                {
                    if (DateTime.TryParse(fieldValue, out DateTime dateValue))
                    {
                        convertedValue = dateValue;
                    }
                    else if (fieldConfig.Required)
                    {
                        _logger.LogWarning("Giá trị '{FieldValue}' không hợp lệ cho ngày tháng (trường {FieldName})",
                            fieldValue, fieldName);
                        conversionSucceeded = false;
                    }
                }
                else if (property.PropertyType == typeof(bool?) || property.PropertyType == typeof(bool))
                {
                    var lowerValue = fieldValue.ToLower();
                    if (lowerValue == "x" || lowerValue == "1" || lowerValue == "true" || lowerValue == "có" || lowerValue == "yes")
                    {
                        convertedValue = true;
                    }
                    else if (lowerValue == "0" || lowerValue == "false" || lowerValue == "không" || lowerValue == "no")
                    {
                        convertedValue = false;
                    }
                    else if (fieldConfig.Required)
                    {
                        _logger.LogWarning("Giá trị '{FieldValue}' không hợp lệ cho kiểu boolean (trường {FieldName})",
                            fieldValue, fieldName);
                        conversionSucceeded = false;
                    }
                }

                // Gán giá trị nếu chuyển đổi thành công
                if (conversionSucceeded && convertedValue != null)
                {
                    property.SetValue(khachHang, convertedValue);
                    _logger.LogDebug("Đã gán {FieldName} = {Value}", fieldName,
                        convertedValue is string ? $"\"{convertedValue}\"" : convertedValue);
                }
            }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý trường {FieldName}", fieldName);
                    if (fieldConfig.Required)
                    {
                        missingRequiredFields.Add(columnName);
                    }
                }
            } // Kết thúc foreach (DataColumn column in row.Table.Columns)

            // Kiểm tra các trường bắt buộc còn thiếu
            if (missingRequiredFields.Count > 0)
            {
                var missingFields = string.Join(", ", missingRequiredFields.Take(5));
                if (missingRequiredFields.Count > 5)
                {
                    missingFields += $" và {missingRequiredFields.Count - 5} trường khác";
                }

                _logger.LogError("Không thể import dữ liệu vì thiếu {Count} trường bắt buộc: {MissingFields}",
                    missingRequiredFields.Count, missingFields);

                throw new InvalidOperationException($"Thiếu {missingRequiredFields.Count} trường bắt buộc: {missingFields}");
            }

            // Cập nhật thông tin từ context
            if (!string.IsNullOrEmpty(context.MaPhong))
            {
                khachHang.PhongThucHien = context.MaPhong;
                _logger.LogDebug("Đã cập nhật phòng thực hiện từ context: {MaPhong}", context.MaPhong);
            }
            else
            {
                _logger.LogDebug("Không có thông tin phòng thực hiện từ context");
            }

            // Cập nhật thông tin người thực hiện
            khachHang.UserThucHienId = context.UserName; // Lưu UserName vào UserThucHienId
            khachHang.NgayCapNhatDuLieu = DateTime.Now;

            // Log thông tin cơ bản về đối tượng đã tạo
            _logger.LogDebug("Đã tạo mới đối tượng KhachHangDN - SoCif: {SoCif}, Tên: {TenCif}",
                khachHang.SoCif, khachHang.TenCif);

            return khachHang;
    }
}
