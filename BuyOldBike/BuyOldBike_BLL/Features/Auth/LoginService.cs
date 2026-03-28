using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_BLL.Services.Auth
{
    public class LoginService
    {
        private UserRepository _userRepository;
        public LoginService()
        {
            _userRepository = new UserRepository();
        }
        public bool Login(string email, string password)
        {
            var user = _userRepository.FindUser(email, password);
            if (user == null) return false;
            if (user.Status == StatusConstants.UserStatus.Suspended) return false;
            return true;
        }

        public User? LoginAndGetUser(string email, string password)
        {
            return _userRepository.FindUser(email, password);
        }
        public enum LoginResult
        {
            Success,
            InvalidCredentials,
            Suspended
        }
        public LoginResult LoginWithResult(string email, string password)
        {
            var user = _userRepository.FindUser(email, password);

            if (user == null)
                return LoginResult.InvalidCredentials;

            if (user.Status == StatusConstants.UserStatus.Suspended)
                return LoginResult.Suspended;

            return LoginResult.Success;
        }
        public string GetRoleUser(string email) => _userRepository.FindByEmail(email)?.Role ?? "";

    }
}
