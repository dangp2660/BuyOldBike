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

        //ADMIN
        public List<User> GetFilteredUsers(string? searchTerm, string? role)
        {
            var query = _db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lower = searchTerm.ToLower();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(lower) ||
                    u.PhoneNumber.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(role) && role != "All roles")
                query = query.Where(u => u.Role == role);

            return query.OrderBy(u => u.Email).ToList();
        }

        public User? GetById(Guid userId)
        {
            return _db.Users.FirstOrDefault(u => u.UserId == userId);
        }

        public bool UpdateRole(Guid userId, string newRole)
        {
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null) return false;
            user.Role = newRole;
            return _db.SaveChanges() > 0;
        }
        public bool UpdateStatus(Guid userId, string newStatus)
        {
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null) return false;
            user.Status = newStatus;
            return _db.SaveChanges() > 0;
        }
    }

}

