using BuyOldBike_BLL.Features.Chats;
using BuyOldBike_DAL.Repositories.Chats;
using BuyOldBike_Presentation.State;
using BuyOldBike_Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace BuyOldBike_Presentation.Views
{
    public partial class BuyerMessagesView : Window
    {
        private ChatService _chatService;
        private ChatViewModel? _chatVm;

        public BuyerMessagesView()
        {
            InitializeComponent();

            _chatService = new ChatService(new MessageRepository());

            LoadConversations();
        }

        private void LoadConversations()
        {
            var user = AppSession.CurrentUser;
            if (user == null) return;

            LbConversations.ItemsSource =
                _chatService.GetConversationsForBuyer(user.UserId);
        }

        private void LbConversations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var conv = LbConversations.SelectedItem as ConversationViewModel;
            if (conv == null) return;

            var user = AppSession.CurrentUser;

            _chatVm = new ChatViewModel(
                conv.ListingId,
                user.UserId,
                conv.OtherUserId);

            IcMessages.ItemsSource = _chatVm.Messages;
            TxtInput.DataContext = _chatVm;

            _chatVm.LoadMessages();
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (_chatVm == null) return;

            _chatVm.InputText = TxtInput.Text;
            _chatVm.Send();

            TxtInput.Text = "";
            ChatScrollViewer.ScrollToBottom();
        }

        private void BtnRefreshChat_Click(object sender, RoutedEventArgs e)
        {
            if (_chatVm == null) return;

            _chatVm.LoadMessages();
            LoadConversations(); 

            ChatScrollViewer.ScrollToBottom();
        }
    }
}