using BuyOldBike_BLL.Features.Auth;
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
    public class AdminUserManagementViewModel : INotifyPropertyChanged
    {
        private readonly UserManagementService _service;

        public ObservableCollection<User> Users { get; } = new();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); }
        }

        private string _selectedRole = "All roles";
        public string SelectedRole
        {
            get => _selectedRole;
            set { _selectedRole = value; OnPropertyChanged(); }
        }

        private User? _selectedUser;
        public User? SelectedUser
        {
            get => _selectedUser;
            set { _selectedUser = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public event Action<User>? OnViewProfileRequested;

        public AdminUserManagementViewModel(UserManagementService service)
        {
            _service = service;
        }

        public void LoadUsers()
        {
            IsLoading = true;
            try
            {
                var list = _service.GetUsers(SearchText.Trim(), SelectedRole);
                Users.Clear();
                foreach (var u in list)
                    Users.Add(u);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi load users: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        public void ChangeRole(Guid userId, string newRole)
        {
            var (ok, msg) = _service.ChangeUserRole(userId, newRole);
            MessageBox.Show(msg,
                ok ? "Thành công" : "Lỗi",
                MessageBoxButton.OK,
                ok ? MessageBoxImage.Information : MessageBoxImage.Error);

            if (ok) LoadUsers();
        }

        public void ViewProfile(Guid userId)
        {
            var user = _service.GetUserProfile(userId);
            if (user == null)
            {
                MessageBox.Show("Không tìm thấy user.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            OnViewProfileRequested?.Invoke(user);
        }
        public void SuspendUser(Guid userId)
        {
            (bool ok, string msg) = _service.SuspendUser(userId);
            MessageBox.Show(msg,
                ok ? "Thành công" : "Lỗi",
                MessageBoxButton.OK,
                ok ? MessageBoxImage.Information : MessageBoxImage.Error);

            if (ok) LoadUsers();
        }
        public void ActivateUser(Guid userId)
        {
            (bool ok, string msg) = _service.ActivateUser(userId);
            MessageBox.Show(msg,
                ok ? "Thành công" : "Lỗi",
                MessageBoxButton.OK,
                ok ? MessageBoxImage.Information : MessageBoxImage.Error);

            if (ok) LoadUsers();
        }
        private string _selectedStatus = "All status";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set { _selectedStatus = value; OnPropertyChanged(); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
