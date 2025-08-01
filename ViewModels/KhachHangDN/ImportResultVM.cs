using System.Collections.Generic;

namespace CTOM.ViewModels.KhachHangDN
{
    public class ImportResultVM
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
