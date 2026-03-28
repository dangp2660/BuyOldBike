using BuyOldBike_Presentation.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BuyOldBike_Presentation.Views
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        private readonly ChatViewModel _chatVm;
        public ChatWindow(Guid listingId, Guid buyerId, Guid sellerId)
        {
            InitializeComponent();

            _chatVm = new ChatViewModel(listingId, buyerId, sellerId);

            IcMessages.ItemsSource = _chatVm.Messages;
            TxtInput.DataContext = _chatVm;

            _chatVm.LoadMessages();
        }
        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            _chatVm.InputText = TxtInput.Text;
            _chatVm.Send();
            TxtInput.Text = string.Empty;

            ChatScrollViewer.ScrollToBottom();
        }
    }
}
