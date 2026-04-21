using System.Windows;
using UI_Dat_Ve_May_Bay.ViewModels;

namespace UI_Dat_Ve_May_Bay.Views
{
    public partial class PolicyWindow : Window
    {
        public PolicyWindow()
        {
            InitializeComponent();
            var viewModel = new PolicyViewModel();
            DataContext = viewModel;
            PolicyScrollViewer.ScrollChanged += (s, e) =>
            {
                viewModel.OnScrollChanged(e.VerticalOffset, e.ExtentHeight - e.ViewportHeight);
            };
        }

        private void AgreeButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}


