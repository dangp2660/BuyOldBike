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
            User user = _userRepository.FindUser(email, password);
            if (user != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
