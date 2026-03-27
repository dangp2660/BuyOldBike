using BuyOldBike_DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_DAL.Repositories.Categories
{
    public class CategoryRepository
    {
        private readonly BuyOldBikeContext _db;

        public CategoryRepository()
        {
            _db = new BuyOldBikeContext();
        }

        public List<BuyOldBike_DAL.Entities.Type> GetAllTypes()
            => _db.Types.OrderBy(t => t.Name).ToList();

        public bool AddType(string name)
        {
            _db.Types.Add(new Entities.Type { Name = name });
            return _db.SaveChanges() > 0;
        }

        public bool UpdateType(int id, string newName)
        {
            var entity = _db.Types.FirstOrDefault(t => t.BikeTypeId == id);
            if (entity == null) return false;
            entity.Name = newName;
            return _db.SaveChanges() > 0;
        }

        public bool DeleteType(int id)
        {
            var entity = _db.Types.FirstOrDefault(t => t.BikeTypeId == id);
            if (entity == null) return false;
            _db.Types.Remove(entity);
            return _db.SaveChanges() > 0;
        }

        public List<Brand> GetAllBrands()
            => _db.Brands.OrderBy(b => b.BrandName).ToList();

        public bool AddBrand(string name)
        {
            _db.Brands.Add(new Brand { BrandName = name });
            return _db.SaveChanges() > 0;
        }

        public bool UpdateBrand(int id, string newName)
        {
            var entity = _db.Brands.FirstOrDefault(b => b.BrandId == id);
            if (entity == null) return false;
            entity.BrandName = newName;
            return _db.SaveChanges() > 0;
        }

        public bool DeleteBrand(int id)
        {
            var entity = _db.Brands.FirstOrDefault(b => b.BrandId == id);
            if (entity == null) return false;
            _db.Brands.Remove(entity);
            return _db.SaveChanges() > 0;
        }

        public List<FrameSize> GetAllFrameSizes()
            => _db.FrameSizes.OrderBy(f => f.SizeValue).ToList();

        public bool AddFrameSize(string value)
        {
            _db.FrameSizes.Add(new FrameSize { SizeValue = value });
            return _db.SaveChanges() > 0;
        }

        public bool UpdateFrameSize(int id, string newValue)
        {
            var entity = _db.FrameSizes.FirstOrDefault(f => f.FrameSizeId == id);
            if (entity == null) return false;
            entity.SizeValue = newValue;
            return _db.SaveChanges() > 0;
        }

        public bool DeleteFrameSize(int id)
        {
            var entity = _db.FrameSizes.FirstOrDefault(f => f.FrameSizeId == id);
            if (entity == null) return false;
            _db.FrameSizes.Remove(entity);
            return _db.SaveChanges() > 0;
        }
    }
}
