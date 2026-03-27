using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuyOldBike_BLL.Services.Users
{
    public class UserAddressQueryService
    {
        public UserAddressPrefill GetPrefill(Guid userId)
        {
            using var db = new BuyOldBikeContext();

            var result = new UserAddressPrefill();

            var user = db.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.UserId == userId);
            if (user != null && !string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                result = new UserAddressPrefill { PhoneNumber = user.PhoneNumber.Trim() };
            }

            var addr = db.Addresses
                .AsNoTracking()
                .FirstOrDefault(a => a.UserId == userId);

            if (addr == null) return result;

            return new UserAddressPrefill
            {
                PhoneNumber = string.IsNullOrWhiteSpace(result.PhoneNumber) ? (addr.PhoneNumber ?? "").Trim() : result.PhoneNumber,
                FullName = (addr.FullName ?? "").Trim(),
                Province = ((addr.Province ?? addr.City) ?? "").Trim(),
                District = (addr.District ?? "").Trim(),
                Ward = (addr.Ward ?? "").Trim(),
                Detail = (addr.Detail ?? "").Trim()
            };
        }
    }
}
