using System.Collections.Generic;

namespace CTOM.ViewModels
{
    public class PageHeaderViewModel
    {
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string Icon { get; set; }
        public bool ShowBreadcrumb { get; set; } = true;
        public required List<BreadcrumbItem> BreadcrumbItems { get; set; }
        public string? ActionText { get; set; }
        public string? ActionUrl { get; set; }
        public string? ActionIcon { get; set; }
        public bool ShowActionButton => !string.IsNullOrEmpty(ActionText) && !string.IsNullOrEmpty(ActionUrl);
        public string? SearchPlaceholder { get; set; }
    }
}
