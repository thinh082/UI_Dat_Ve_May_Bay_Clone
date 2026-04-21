using System;
using System.Windows.Controls;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using UI_Dat_Ve_May_Bay.ViewModels;

namespace UI_Dat_Ve_May_Bay.Views
{
    public partial class AiChatView : UserControl
    {
        private INotifyCollectionChanged? _subscribedCollection;

        public AiChatView()
        {
            InitializeComponent();
            DataContextChanged += AiChatView_DataContextChanged;
            Unloaded += AiChatView_Unloaded;
        }

        private void AiChatView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_subscribedCollection != null)
                _subscribedCollection.CollectionChanged -= Messages_CollectionChanged;

            if (DataContext is AiChatViewModel vm)
            {
                _subscribedCollection = vm.Messages;
                _subscribedCollection.CollectionChanged += Messages_CollectionChanged;
                ScrollToBottom();
            }
            else
            {
                _subscribedCollection = null;
            }
        }

        private void Messages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => ScrollToBottom();

        private void ScrollToBottom()
            => Dispatcher.BeginInvoke(new Action(() => MessagesScrollViewer.ScrollToEnd()));

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                return;

            if (DataContext is AiChatViewModel vm && vm.SendMessageCommand.CanExecute(null))
            {
                vm.SendMessageCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void AiChatView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_subscribedCollection != null)
                _subscribedCollection.CollectionChanged -= Messages_CollectionChanged;
            _subscribedCollection = null;
        }
    }
}
