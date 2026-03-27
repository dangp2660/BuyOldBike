using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuyOldBike_BLL.Services.Lookups
{
    public class LookupService
    {
        public List<Brand> GetBrands()
        {
            using var db = new BuyOldBikeContext();
            return db.Brands
                .AsNoTracking()
                .OrderBy(b => b.BrandName)
                .ToList();
        }

        public List<BuyOldBike_DAL.Entities.Type> GetBikeTypes()
        {
            using var db = new BuyOldBikeContext();
            return db.Types
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .ToList();
        }
    }
}
