using System.Windows.Input;
using UI_Dat_Ve_May_Bay.Core;

namespace UI_Dat_Ve_May_Bay.ViewModels
{
    public class PolicyViewModel : ObservableObject
    {
        private bool _isScrolledToBottom;
        private bool _isAgreeButtonEnabled;

        public ICommand AgreeCommand { get; }

        public bool IsScrolledToBottom
        {
            get => _isScrolledToBottom;
            set => SetProperty(ref _isScrolledToBottom, value);
        }

        public bool IsAgreeButtonEnabled
        {
            get => _isAgreeButtonEnabled;
            set => SetProperty(ref _isAgreeButtonEnabled, value);
        }

        public PolicyViewModel()
        {
            AgreeCommand = new RelayCommand(Agree, () => IsScrolledToBottom);
            IsAgreeButtonEnabled = false;
        }

        public void OnScrollChanged(double scrollOffset, double maxScrollOffset)
        {
            IsScrolledToBottom = scrollOffset >= maxScrollOffset - 10;
            IsAgreeButtonEnabled = IsScrolledToBottom;
            if (AgreeCommand is RelayCommand cmd)
                cmd.RaiseCanExecuteChanged();
        }

        private void Agree()
        {
        }
    }
}

