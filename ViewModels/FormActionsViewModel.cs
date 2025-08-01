namespace CTOM.ViewModels
{
    public class FormActionsViewModel
    {
        public bool ShowBackButton { get; set; } = true;
        public string BackUrl { get; set; } = "#";
        
        public bool ShowSaveButton { get; set; } = true;
        public bool ShowSaveAndContinueButton { get; set; } = false;
        
        public bool ShowDeleteButton { get; set; } = false;
        public string? DeleteUrl { get; set; }
        public string? DeleteItemName { get; set; }
        
        public string? CustomButtonText { get; set; }
        public string? CustomButtonUrl { get; set; }
        public string CustomButtonClass { get; set; } = "btn-outline-primary";
        public string? CustomButtonIcon { get; set; }
    }
}
