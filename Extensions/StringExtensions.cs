namespace CTOM.Extensions;

/// <summary>
/// Các phương thức mở rộng cho kiểu string
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Chuyển đổi chuỗi sang dạng camelCase
    /// </summary>
    /// <param name="str">Chuỗi cần chuyển đổi</param>
    /// <returns>Chuỗi đã được chuyển sang camelCase</returns>
    public static string ToCamelCase(this string str)
    {
        if (string.IsNullOrEmpty(str) || str.Length < 2)
            return str.ToLowerInvariant();

        return char.ToLowerInvariant(str[0]) + str[1..];
    }
}
