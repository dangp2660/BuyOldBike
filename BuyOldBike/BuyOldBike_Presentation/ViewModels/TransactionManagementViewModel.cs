using BuyOldBike_BLL.Features.Transaction;
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
    public class TransactionManagementViewModel : INotifyPropertyChanged
    {
        private readonly TransactionManagementService _service;

        public ObservableCollection<Order> Orders { get; } = new();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); }
        }

        private string _selectedStatus = "All status";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set { _selectedStatus = value; OnPropertyChanged(); }
        }

        public TransactionManagementViewModel(TransactionManagementService service)
        {
            _service = service;
        }

        public void LoadOrders()
        {
            try
            {
                var list = _service.GetOrders(SearchText.Trim(), SelectedStatus);
                Orders.Clear();
                foreach (var o in list) Orders.Add(o);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi load orders: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ViewDetail(Guid orderId)
        {
            var order = _service.GetOrderDetail(orderId);
            if (order == null)
            {
                MessageBox.Show("Không tìm thấy order.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            OnViewDetailRequested?.Invoke(order);
        }

        public event Action<Order>? OnViewDetailRequested;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
