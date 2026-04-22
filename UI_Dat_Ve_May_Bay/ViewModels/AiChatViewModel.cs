using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UI_Dat_Ve_May_Bay.Core;
using UI_Dat_Ve_May_Bay.Services;

namespace UI_Dat_Ve_May_Bay.ViewModels
{
    public class AiChatViewModel : ObservableObject
    {
        private readonly GroqChatService? _chatService;
        private readonly List<GroqChatTurn> _conversation = new();

        private string _userInput = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isBusy;
        private bool _canChat;
        private string _apiKeyHint = string.Empty;

        public AiChatViewModel()
        {
            Messages = new ObservableCollection<AiChatMessage>();
            SendMessageCommand = new AsyncRelayCommand(SendMessageAsync, CanSendMessage);

            _canChat = true;
            _chatService = new GroqChatService();
            StatusMessage = "Connected to AI chat.";
            ApiKeyHint = "AI chat is ready.";

            AddAssistantMessage("Xin chao! Minh la tro ly AI. Ban hay dat cau hoi ve chuyen bay, dat ve, hanh ly hoac lich trinh.");
        }

        public ObservableCollection<AiChatMessage> Messages { get; }

        public AsyncRelayCommand SendMessageCommand { get; }

        public string UserInput
        {
            get => _userInput;
            set
            {
                if (!SetProperty(ref _userInput, value)) return;
                SendMessageCommand.RaiseCanExecuteChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (!SetProperty(ref _isBusy, value)) return;
                SendMessageCommand.RaiseCanExecuteChanged();
            }
        }

        public bool CanChat
        {
            get => _canChat;
            private set => SetProperty(ref _canChat, value);
        }

        public string ApiKeyHint
        {
            get => _apiKeyHint;
            private set => SetProperty(ref _apiKeyHint, value);
        }

        private bool CanSendMessage()
            => CanChat && !IsBusy && !string.IsNullOrWhiteSpace(UserInput);

        private async Task SendMessageAsync()
        {
            if (!CanChat || _chatService is null)
            {
                StatusMessage = "Cannot send message because API key is missing.";
                return;
            }

            var text = (UserInput ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return;

            AddUserMessage(text);
            _conversation.Add(new GroqChatTurn("user", text));
            UserInput = string.Empty;

            IsBusy = true;
            StatusMessage = "AI is generating a reply...";

            try
            {
                var (ok, reply, error) = await _chatService.GetReplyAsync(_conversation);
                if (!ok || string.IsNullOrWhiteSpace(reply))
                {
                    StatusMessage = string.IsNullOrWhiteSpace(error) ? "Cannot get AI response." : error;
                    AddAssistantMessage("Xin loi, hien tai minh chua tra loi duoc. Ban thu lai sau nhe.");
                    return;
                }

                _conversation.Add(new GroqChatTurn("assistant", reply));
                AddAssistantMessage(reply);
                StatusMessage = "Ready.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Chat error: " + ex.Message;
                AddAssistantMessage("Da xay ra loi khi goi AI. Ban vui long thu lai.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void AddUserMessage(string content)
            => Messages.Add(new AiChatMessage(true, content));

        private void AddAssistantMessage(string content)
            => Messages.Add(new AiChatMessage(false, content));
    }

    public class AiChatMessage
    {
        public AiChatMessage(bool isFromUser, string content)
        {
            IsFromUser = isFromUser;
            Content = content;
            Sender = isFromUser ? "You" : "AI";
        }

        public bool IsFromUser { get; }

        public string Sender { get; }

        public string Content { get; }
    }
}
