    using BuyOldBike_DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_DAL.Repositories.Auth
{
    public class UserRepository
    {
        private BuyOldBikeContext _db;

        public UserRepository()
        {
            _db = new BuyOldBikeContext();
        }

        public List<User> GetAllUsers()
        {
            return _db.Users.ToList();
        }

        public User? FindUser(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            return _db.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
        }

        public User? FindByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return _db.Users.FirstOrDefault(u => u.Email == email);
        }

        public bool AddUser(User user)
        {
            _db.Users.Add(user);
            return _db.SaveChanges() > 0;
        }

    }
}
