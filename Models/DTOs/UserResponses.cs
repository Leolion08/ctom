using System.Text.Json.Serialization;

namespace CTOM.Models.DTOs
{
    public class UserNameCheckResponse
    {
        public bool IsAvailable { get; set; }
        public bool Exists { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        public bool IsAvailable { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    /// <summary>
    /// Context cho source generation
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(UserNameCheckResponse))]
    [JsonSerializable(typeof(ErrorResponse))]
    public partial class AppJsonContext : JsonSerializerContext
    {
    }
}
