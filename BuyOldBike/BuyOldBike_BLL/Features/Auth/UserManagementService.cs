using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_BLL.Features.Auth
{
    public class UserManagementService
    {
        private readonly UserRepository _userRepo;

        public UserManagementService() : this(new UserRepository())
        {
        }

        public UserManagementService(UserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public List<User> GetUsers(string? searchTerm, string? role)
        {
            return _userRepo.GetFilteredUsers(searchTerm, role);
        }

        public User? GetUserProfile(Guid userId)
        {
            return _userRepo.GetById(userId);
        }

        public (bool Success, string Message) ChangeUserRole(Guid userId, string newRole)
        {
            var validRoles = new[]
            {
                RoleConstants.Buyer,
                RoleConstants.Seller,
                RoleConstants.Inspector,
                RoleConstants.Admin
            };

            if (!validRoles.Contains(newRole))
                return (false, $"Role '{newRole}' không hợp lệ.");

            var user = _userRepo.GetById(userId);
            if (user == null)
                return (false, "Không tìm thấy người dùng.");

            if (user.Role == newRole)
                return (false, $"Người dùng đã có role '{newRole}' rồi.");

            var ok = _userRepo.UpdateRole(userId, newRole);
            return ok
                ? (true, $"Đã đổi role thành công → {newRole}")
                : (false, "Cập nhật thất bại, vui lòng thử lại.");
        }
        public (bool Success, string Message) SuspendUser(Guid userId)
        {
            var user = _userRepo.GetById(userId);
            if (user == null) return (false, "Không tìm thấy người dùng.");
            if (user.Status == StatusConstants.UserStatus.Suspended)
                return (false, "Người dùng đã bị suspended.");

            var ok = _userRepo.UpdateStatus(userId, StatusConstants.UserStatus.Suspended);
            return ok ? (true, "Đã suspend tài khoản.") : (false, "Cập nhật thất bại.");
        }
        public (bool Success, string Message) ActivateUser(Guid userId)
        {
            var user = _userRepo.GetById(userId);
            if (user == null) return (false, "Không tìm thấy người dùng.");
            if (user.Status == StatusConstants.UserStatus.Active)
                return (false, "Người dùng đã active.");

            var ok = _userRepo.UpdateStatus(userId, StatusConstants.UserStatus.Active);
            return ok ? (true, "Đã activate tài khoản.") : (false, "Cập nhật thất bại.");
        }

    }
}
