using BuyOldBike_BLL.Features.Auth;
using BuyOldBike_BLL.Features.Categories;
using BuyOldBike_BLL.Features.Transaction;
using BuyOldBike_BLL.Services.Exports;
using BuyOldBike_BLL.Services.Listings;
using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using BuyOldBike_Presentation.State;
using BuyOldBike_Presentation.ViewModels;
using Microsoft.Win32;
using System.Data.Common;
using System.Windows;
using System.Windows.Controls;

namespace BuyOldBike_Presentation.Views
{
    public partial class AdminWindow : Window
    {
        private AdminUserManagementViewModel? _userVm;
        private ListingModerationViewModel? _listingVm;
        private CategoryManagementViewModel? _categoryVm;
        private TransactionManagementViewModel? _transactionVm;

        public AdminWindow()
        {
            InitializeComponent();
            if (!RoleNavigator.EnsureRole(this, RoleConstants.Admin)) return;
            var userSvc = new UserManagementService();
            _userVm = new AdminUserManagementViewModel(userSvc);

            var listingSvc = new ListingModerationService();
            _listingVm = new ListingModerationViewModel(listingSvc);
            DgListings.ItemsSource = _listingVm.Listings;

            var categorySvc = new CategoryManagementService();
            _categoryVm = new CategoryManagementViewModel(categorySvc);
            TabCategoryManagement.DataContext = _categoryVm;

            var transactionSvc = new TransactionManagementService();
            _transactionVm = new TransactionManagementViewModel(transactionSvc);

            _transactionVm.OnViewDetailRequested += (order) =>
            {
                var dialog = new OrderDetailDialog(order) { Owner = this };
                dialog.ShowDialog();
            };
            DgTransactions.ItemsSource = _transactionVm.Orders;

            _userVm.OnViewProfileRequested += (user) =>
            {
                var dialog = new UserProfileDialog(user) { Owner = this };
                dialog.ShowDialog();
            };

            DgUsers.ItemsSource = _userVm.Users;
            _userVm.LoadUsers();
        }
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is not TabControl) return;
            if (_userVm == null) return;
            if (TabUserManagement.IsSelected)
                _userVm.LoadUsers();
            if (TabListingModeration.IsSelected)
                _listingVm?.LoadListings();
            if (TabCategoryManagement.IsSelected)
                _categoryVm?.LoadAll();
            if (TabTransactionManagement.IsSelected)
                _transactionVm?.LoadOrders();
        }

        private void BtnApproveListing_Click(object sender, RoutedEventArgs e)
        {
            if (_listingVm == null) return;
            if ((sender as Button)?.DataContext is not Listing listing) return;

            var confirm = MessageBox.Show(
                $"Duyệt bài \"{listing.Title}\"?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
                _listingVm.ApproveListing(listing.ListingId);
        }
        private void BtnRejectListing_Click(object sender, RoutedEventArgs e)
        {
            if (_listingVm == null) return;
            if ((sender as Button)?.DataContext is not Listing listing) return;

            var confirm = MessageBox.Show(
                $"Reject bài \"{listing.Title}\"?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.Yes)
                _listingVm.RejectListing(listing.ListingId);
        }
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (_userVm == null) return;
            _userVm.SearchText = TxtSearch.Text.Trim();

            if (CmbRole.SelectedItem is ComboBoxItem roleItem)
                _userVm.SelectedRole = roleItem.Tag?.ToString() ?? "All roles";

            if (CmbStatus.SelectedItem is ComboBoxItem statusItem)
                _userVm.SelectedStatus = statusItem.Tag?.ToString() ?? "All status";

            _userVm.LoadUsers();

        }

        private void CmbRole_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_userVm == null) return;
            if (CmbRole.SelectedItem is ComboBoxItem item)
                _userVm.SelectedRole = item.Tag?.ToString() ?? "All roles";
            _userVm.LoadUsers();
        }
        private void CmbStatus_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_userVm == null) return;
            if (CmbStatus.SelectedItem is ComboBoxItem item)
                _userVm.SelectedStatus = item.Tag?.ToString() ?? "All status";
            _userVm.LoadUsers();
        }
        private void BtnSuspend_Click(object sender, RoutedEventArgs e)
        {
            if (_userVm == null) return;
            if ((sender as Button)?.DataContext is not User user) return;

            var confirm = MessageBox.Show(
                $"Bạn có chắc muốn suspend \"{user.Email}\"?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
                _userVm.SuspendUser(user.UserId);
        }
        private void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            if (_userVm == null) return;
            if ((sender as Button)?.DataContext is not User user) return;
            _userVm.ActivateUser(user.UserId);
        }
        private void BtnChangeRole_Click(object sender, RoutedEventArgs e)
        {
            if (_userVm == null) return;
            if ((sender as Button)?.DataContext is not User user) return;
            var dialog = new ChangeRoleDialog(user) { Owner = this };
            if (dialog.ShowDialog() == true)
                _userVm.ChangeRole(user.UserId, dialog.SelectedRole);
        }

        private void BtnViewProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_userVm == null) return;
            if ((sender as Button)?.DataContext is not User user) return;
            _userVm.ViewProfile(user.UserId);
        }


        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LogoutManager.Logout(this);
        }

        private void BtnListingSearch_Click(object sender, RoutedEventArgs e)
        {
            if (_listingVm == null) return;
            _listingVm.SearchText = TxtListingSearch.Text.Trim();
            if (CmbListingStatus.SelectedItem is ComboBoxItem item)
                _listingVm.SelectedStatus = item.Tag?.ToString() ?? "All status";
            _listingVm.LoadListings();
        }

        //category 
        private void BtnAddType_Click(object sender, RoutedEventArgs e)
        {
            if (_categoryVm == null) return;
            _categoryVm.AddType(TxtTypeInput.Text);
            TxtTypeInput.Clear();
        }
        private void BtnEditType_Click(object sender, RoutedEventArgs e)
        {
            if (_categoryVm == null) return;
            _categoryVm.UpdateType(
                LbTypes.SelectedItem as BuyOldBike_DAL.Entities.Type,
                TxtTypeInput.Text);
        }
        private void BtnDeleteType_Click(object sender, RoutedEventArgs e)
        {
            if (_categoryVm == null) return;
            var confirm = MessageBox.Show("Xoá category này?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.Yes)
                _categoryVm.DeleteType(LbTypes.SelectedItem as BuyOldBike_DAL.Entities.Type);
        }
        private void LbTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LbTypes.SelectedItem is BuyOldBike_DAL.Entities.Type t)
                TxtTypeInput.Text = t.Name;
        }

        private void BtnAddBrand_Click(object sender, RoutedEventArgs e)
        {
            if (_categoryVm == null) return;
            _categoryVm.AddBrand(TxtBrandInput.Text);
            TxtBrandInput.Clear();
        }

        private void BtnEditBrand_Click(object sender, RoutedEventArgs e)
        {
            if (_categoryVm == null) return;
            _categoryVm.UpdateBrand(
                LbBrands.SelectedItem as Brand,
                TxtBrandInput.Text);
        }
        private void BtnDeleteBrand_Click(object sender, RoutedEventArgs e)
        {
            if (_categoryVm == null) return;
            var confirm = MessageBox.Show("Xoá brand này?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.Yes)
                _categoryVm.DeleteBrand(LbBrands.SelectedItem as Brand);
        }
        private void LbBrands_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LbBrands.SelectedItem is Brand b)
                TxtBrandInput.Text = b.BrandName;
        }
        private void BtnAddFrameSize_Click(object sender, RoutedEventArgs e)
        {
            if (_categoryVm == null) return;
            _categoryVm.AddFrameSize(TxtFrameSizeInput.Text);
            TxtFrameSizeInput.Clear();
        }
        private void BtnEditFrameSize_Click(object sender, RoutedEventArgs e)
        {
            if (_categoryVm == null) return;
            _categoryVm.UpdateFrameSize(
                LbFrameSizes.SelectedItem as FrameSize,
                TxtFrameSizeInput.Text);
        }
        private void BtnDeleteFrameSize_Click(object sender, RoutedEventArgs e)
        {
            if (_categoryVm == null) return;
            var confirm = MessageBox.Show("Xoá frame size này?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.Yes)
                _categoryVm.DeleteFrameSize(LbFrameSizes.SelectedItem as FrameSize);
        }
        private void LbFrameSizes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LbFrameSizes.SelectedItem is FrameSize f)
                TxtFrameSizeInput.Text = f.SizeValue;
        }

        //transaction
        private void BtnTransactionSearch_Click(object sender, RoutedEventArgs e)
        {
            if (_transactionVm == null) return;
            _transactionVm.SearchText = TxtTransactionSearch.Text.Trim();
            if (CmbTransactionStatus.SelectedItem is ComboBoxItem item)
                _transactionVm.SelectedStatus = item.Tag?.ToString() ?? "All status";
            _transactionVm.LoadOrders();
        }

        private void BtnViewOrderDetail_Click(object sender, RoutedEventArgs e)
        {
            if (_transactionVm == null) return;
            if ((sender as Button)?.DataContext is not Order order) return;
            _transactionVm.ViewDetail(order.OrderId);
        }

        private void BtnTransactionExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TransactionService service = new TransactionService();
                TransactionExportService exporter = new TransactionExportService();

                var transactions = service.GetTransactions();

                var dialog = new SaveFileDialog();
                dialog.Filter = "Excel File (*.xlsx)|*.xlsx";

                if (dialog.ShowDialog() == true)
                {
                    exporter.ExportTransactions(transactions, dialog.FileName);
                    MessageBox.Show("Xuất file thành công!",
                                    "Export Transactions",
                                     MessageBoxButton.OK,
                                     MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Xuất file thất bại!\n\n" + ex.Message,
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
