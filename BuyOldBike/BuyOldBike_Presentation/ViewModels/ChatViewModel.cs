using BuyOldBike_BLL.Features.Chats;
using BuyOldBike_DAL.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BuyOldBike_Presentation.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        private readonly ChatService _service;

        public Guid ListingId { get; }
        public Guid SenderId { get; }
        public Guid ReceiverId { get; }

        public ObservableCollection<Message> Messages { get; } = new();

        private string _inputText = string.Empty;
        public string InputText
        {
            get => _inputText;
            set { _inputText = value; OnPropertyChanged(); }
        }

        public ChatViewModel(Guid listingId, Guid senderId, Guid receiverId)
        {
            ListingId = listingId;
            SenderId = senderId;
            ReceiverId = receiverId;

            var repo = new BuyOldBike_DAL.Repositories.Chats.MessageRepository();
            _service = new ChatService(repo);
        }

        public void LoadMessages()
        {
            try
            {
                var list = _service.GetMessages(ListingId, SenderId, ReceiverId);
                Messages.Clear();
                foreach (var m in list)
                {
                    m.IsMine = m.SenderId == SenderId;
                    Messages.Add(m);
                }

                // Đánh dấu đã đọc
                _service.MarkAsRead(ListingId, SenderId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi load tin nhắn: {ex.Message}");
            }

        }

        public void Send()
        {
            var (ok, err) = _service.SendMessage(
                ListingId, SenderId, ReceiverId, InputText);

            if (!ok)
            {
                MessageBox.Show(err, "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            InputText = string.Empty;
            LoadMessages();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
