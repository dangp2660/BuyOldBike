using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Auth;

namespace BuyOldBike_BLL.Services.Auth
{
    public class RegisterService
    {
        private UserRepository _userRepository;

        public RegisterService()
        {
            _userRepository = new UserRepository();
        }

        public bool RegisterForBuyer(string email, string phoneNumber, string password)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(phoneNumber) ||
                string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            User? existingUser = _userRepository.FindByEmail(email);
            if (existingUser != null)
            {
                return false;
            }

            User user = new User
            {
                UserId = Guid.NewGuid(),
                Email = email,
                PhoneNumber = phoneNumber,
                Password = password,
                Role = "Buyer"  
            };

            return _userRepository.AddUser(user);
        }
    }
}
