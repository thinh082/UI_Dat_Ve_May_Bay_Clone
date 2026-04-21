using System;
using System.Windows.Input;

namespace UI_Dat_Ve_May_Bay.Core
{
    public class RelayCommand : ICommand
    {
        private readonly Action? _execute;
        private readonly Func<bool>? _canExecute;

        private readonly Action<object?>? _executeParam;
        private readonly Func<object?, bool>? _canExecuteParam;

        // Case 1: command không param (giữ nguyên để không vỡ code cũ)
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Case 2: command có param (mới)
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _executeParam = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteParam = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            if (_executeParam != null)
                return _canExecuteParam?.Invoke(parameter) ?? true;

            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            if (_executeParam != null)
            {
                _executeParam(parameter);
                return;
            }

            _execute?.Invoke();
        }

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
