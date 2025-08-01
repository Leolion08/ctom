namespace CTOM.ViewModels
{
    public class BreadcrumbItem
    {
        public required string Text { get; set; }
        public required string Url { get; set; }
        public bool IsActive { get; set; }
    }
}
