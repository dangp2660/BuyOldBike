
using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Chats;
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
    public class SellerChatViewModel : INotifyPropertyChanged
    {
        private readonly ChatService _service;
        public Guid SellerId { get; }

        // Danh sách cuộc hội thoại
        public ObservableCollection<Message> Conversations { get; } = new();

        // Tin nhắn trong cuộc hội thoại đang chọn
        public ObservableCollection<Message> CurrentMessages { get; } = new();

        private Message? _selectedConversation;
        public Message? SelectedConversation
        {
            get => _selectedConversation;
            set
            {
                _selectedConversation = value;
                OnPropertyChanged();
                if (value != null) LoadCurrentMessages(value);
            }
        }

        private string _inputText = string.Empty;
        public string InputText
        {
            get => _inputText;
            set { _inputText = value; OnPropertyChanged(); }
        }

        // BuyerId của cuộc hội thoại đang chọn
        private Guid? _currentBuyerId;
        private Guid? _currentListingId;

        public SellerChatViewModel(Guid sellerId)
        {
            SellerId = sellerId;
            var repo = new MessageRepository();
            _service = new ChatService(repo);
        }

        public void LoadConversations()
        {
            try
            {
                var list = _service.GetConversations(SellerId);
                Conversations.Clear();
                foreach (var m in list) Conversations.Add(m);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi load conversations: {ex.Message}");
            }
        }

        private void LoadCurrentMessages(Message selected)
        {
            // BuyerId là người kia (không phải seller)
            _currentBuyerId = selected.SenderId == SellerId
                ? selected.ReceiverId
                : selected.SenderId;
            _currentListingId = selected.ListingId;

            try
            {
                var list = _service.GetMessages(
                    selected.ListingId, SellerId, _currentBuyerId.Value);
                CurrentMessages.Clear();
                foreach (var m in list)
                {
                    m.IsMine = m.SenderId == SellerId;
                    CurrentMessages.Add(m);
                }

                // Đánh dấu đã đọc
                _service.MarkAsRead(selected.ListingId, SellerId);

                // Reload conversations để cập nhật badge
                LoadConversations();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi load tin nhắn: {ex.Message}");
            }
        }

        public void Send()
        {
            if (_currentBuyerId == null || _currentListingId == null)
            {
                MessageBox.Show("Chọn một cuộc hội thoại trước.");
                return;
            }

            var (ok, err) = _service.SendMessage(
                _currentListingId.Value, SellerId,
                _currentBuyerId.Value, InputText);

            if (!ok)
            {
                MessageBox.Show(err, "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            InputText = string.Empty;
            LoadCurrentMessages(SelectedConversation!);
        }

        public void Refresh()
        {
            if (SelectedConversation != null)
                LoadCurrentMessages(SelectedConversation);
            else
                LoadConversations();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
