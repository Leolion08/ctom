using System.Diagnostics.CodeAnalysis;

#if NET6_0_OR_GREATER
// Tắt các cảnh báo liên quan đến trimming cho toàn bộ assembly
[assembly: UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Razor Pages không hỗ trợ trimming")]
[assembly: UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Razor Pages không hỗ trợ AOT")]

// Tắt các cảnh báo cụ thể liên quan đến MVC và Razor Pages
[assembly: UnconditionalSuppressMessage("AspNetCore", "IL2026", Justification = "Razor Pages không hỗ trợ trimming")]
[assembly: UnconditionalSuppressMessage("AspNetCore", "IL3050", Justification = "Razor Pages không hỗ trợ AOT")]
[assembly: UnconditionalSuppressMessage("AspNetCore", "IL3002", Justification = "Razor Pages không hỗ trợ AOT")]
#endif

// Tắt các cảnh báo liên quan đến trimming trong file này
#pragma warning disable IL2026, IL3050, IL3002
