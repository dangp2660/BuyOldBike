using BuyOldBike_BLL.Features.Categories;
using BuyOldBike_DAL.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BuyOldBike_Presentation.ViewModels
{
    public class CategoryManagementViewModel : INotifyPropertyChanged
    {
        private readonly CategoryManagementService _service;

        public ObservableCollection<BuyOldBike_DAL.Entities.Type> Types { get; } = new();
        public ObservableCollection<Brand> Brands { get; } = new();
        public ObservableCollection<FrameSize> FrameSizes { get; } = new();

        public CategoryManagementViewModel(CategoryManagementService service)
        {
            _service = service;
        }

        public void LoadAll()
        {
            LoadTypes();
            LoadBrands();
            LoadFrameSizes();
        }

        // ── Types ─────────────────────────────────────────────────
        public void LoadTypes()
        {
            Types.Clear();
            foreach (var t in _service.GetTypes()) Types.Add(t);
        }

        public void AddType(string name)
        {
            (bool ok, string msg) = _service.AddType(name);
            Show(msg, ok);
            if (ok) LoadTypes();
        }

        public void UpdateType(BuyOldBike_DAL.Entities.Type? selected, string newName)
        {
            if (selected == null) { Show("Chưa chọn category.", false); return; }
            (bool ok, string msg) = _service.UpdateType(selected.BikeTypeId, newName);
            Show(msg, ok);
            if (ok) LoadTypes();
        }

        public void DeleteType(BuyOldBike_DAL.Entities.Type? selected)
        {
            if (selected == null) { Show("Chưa chọn category.", false); return; }
            (bool ok, string msg) = _service.DeleteType(selected.BikeTypeId);
            Show(msg, ok);
            if (ok) LoadTypes();
        }

        // ── Brands ────────────────────────────────────────────────
        public void LoadBrands()
        {
            Brands.Clear();
            foreach (var b in _service.GetBrands()) Brands.Add(b);
        }

        public void AddBrand(string name)
        {
            (bool ok, string msg) = _service.AddBrand(name);
            Show(msg, ok);
            if (ok) LoadBrands();
        }

        public void UpdateBrand(Brand? selected, string newName)
        {
            if (selected == null) { Show("Chưa chọn brand.", false); return; }
            (bool ok, string msg) = _service.UpdateBrand(selected.BrandId, newName);
            Show(msg, ok);
            if (ok) LoadBrands();
        }

        public void DeleteBrand(Brand? selected)
        {
            if (selected == null) { Show("Chưa chọn brand.", false); return; }
            (bool ok, string msg) = _service.DeleteBrand(selected.BrandId);
            Show(msg, ok);
            if (ok) LoadBrands();
        }

        // ── Frame Sizes ───────────────────────────────────────────
        public void LoadFrameSizes()
        {
            FrameSizes.Clear();
            foreach (var f in _service.GetFrameSizes()) FrameSizes.Add(f);
        }

        public void AddFrameSize(string value)
        {
            (bool ok, string msg) = _service.AddFrameSize(value);
            Show(msg, ok);
            if (ok) LoadFrameSizes();
        }

        public void UpdateFrameSize(FrameSize? selected, string newValue)
        {
            if (selected == null) { Show("Chưa chọn frame size.", false); return; }
            (bool ok, string msg) = _service.UpdateFrameSize(selected.FrameSizeId, newValue);
            Show(msg, ok);
            if (ok) LoadFrameSizes();
        }

        public void DeleteFrameSize(FrameSize? selected)
        {
            if (selected == null) { Show("Chưa chọn frame size.", false); return; }
            (bool ok, string msg) = _service.DeleteFrameSize(selected.FrameSizeId);
            Show(msg, ok);
            if (ok) LoadFrameSizes();
        }

        private void Show(string msg, bool ok) =>
            MessageBox.Show(msg, ok ? "Thành công" : "Lỗi",
                MessageBoxButton.OK,
                ok ? MessageBoxImage.Information : MessageBoxImage.Error);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
