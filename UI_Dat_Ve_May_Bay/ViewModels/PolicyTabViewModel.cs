using UI_Dat_Ve_May_Bay.Core;

namespace UI_Dat_Ve_May_Bay.ViewModels
{
    /// <summary>
    /// ViewModel for the Policy tab in the main navigation menu.
    /// This is different from PolicyViewModel which is used for the policy dialog window.
    /// </summary>
    public class PolicyTabViewModel : ObservableObject
    {
        public PolicyTabViewModel()
        {
            // This ViewModel is simple - it just displays static policy content
            // No commands or data loading needed
        }
    }
}
