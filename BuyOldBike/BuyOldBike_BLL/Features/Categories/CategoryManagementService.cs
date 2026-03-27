using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Categories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_BLL.Features.Categories
{
    public class CategoryManagementService
    {
        private readonly CategoryRepository _repo;

        public CategoryManagementService(CategoryRepository repo)
        {
            _repo = repo;
        }

        public List<BuyOldBike_DAL.Entities.Type> GetTypes()
            => _repo.GetAllTypes();

        public (bool Success, string Message) AddType(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (false, "Tên không được để trống.");
            var ok = _repo.AddType(name.Trim());
            return ok ? (true, "Đã thêm category.") : (false, "Thêm thất bại.");
        }

        public (bool Success, string Message) UpdateType(int id, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return (false, "Tên không được để trống.");
            var ok = _repo.UpdateType(id, newName.Trim());
            return ok ? (true, "Đã cập nhật category.") : (false, "Cập nhật thất bại.");
        }

        public (bool Success, string Message) DeleteType(int id)
        {
            var ok = _repo.DeleteType(id);
            return ok ? (true, "Đã xoá category.") : (false, "Xoá thất bại.");
        }

        public List<Brand> GetBrands()
            => _repo.GetAllBrands();

        public (bool Success, string Message) AddBrand(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (false, "Tên không được để trống.");
            var ok = _repo.AddBrand(name.Trim());
            return ok ? (true, "Đã thêm brand.") : (false, "Thêm thất bại.");
        }

        public (bool Success, string Message) UpdateBrand(int id, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return (false, "Tên không được để trống.");
            var ok = _repo.UpdateBrand(id, newName.Trim());
            return ok ? (true, "Đã cập nhật brand.") : (false, "Cập nhật thất bại.");
        }

        public (bool Success, string Message) DeleteBrand(int id)
        {
            var ok = _repo.DeleteBrand(id);
            return ok ? (true, "Đã xoá brand.") : (false, "Xoá thất bại.");
        }

        public List<FrameSize> GetFrameSizes()
            => _repo.GetAllFrameSizes();

        public (bool Success, string Message) AddFrameSize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return (false, "Giá trị không được để trống.");
            var ok = _repo.AddFrameSize(value.Trim());
            return ok ? (true, "Đã thêm frame size.") : (false, "Thêm thất bại.");
        }

        public (bool Success, string Message) UpdateFrameSize(int id, string newValue)
        {
            if (string.IsNullOrWhiteSpace(newValue))
                return (false, "Giá trị không được để trống.");
            var ok = _repo.UpdateFrameSize(id, newValue.Trim());
            return ok ? (true, "Đã cập nhật frame size.") : (false, "Cập nhật thất bại.");
        }

        public (bool Success, string Message) DeleteFrameSize(int id)
        {
            var ok = _repo.DeleteFrameSize(id);
            return ok ? (true, "Đã xoá frame size.") : (false, "Xoá thất bại.");
        }
    }
}
