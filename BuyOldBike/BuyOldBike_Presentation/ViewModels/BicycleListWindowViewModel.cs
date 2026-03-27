using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Categories;
using BuyOldBike_DAL.Repositories.Seller;
using BuyOldBike_BLL.Features.Categories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BuyOldBike_Presentation.ViewModels
{
    public class BicycleListWindowViewModel : INotifyPropertyChanged
    {
        private readonly List<BicycleCardVm> _all = new List<BicycleCardVm>();
        private readonly List<BicycleCardVm> _filtered = new List<BicycleCardVm>();
        private int _currentPage = 1;
        private int _totalPages = 1;
        private string _searchText = "";
        private string _selectedBrand = "Tất cả";
        private string _selectedBikeType = "Tất cả";
        private bool _suppressApplyFilters;

        public ObservableCollection<BicycleCardVm> Listings { get; } = new ObservableCollection<BicycleCardVm>();
        public ObservableCollection<int> PageNumbers { get; } = new ObservableCollection<int>();
        public ObservableCollection<string> Brands { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> BikeTypes { get; } = new ObservableCollection<string>();

        public int PageSize { get; } = 25;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value ?? "";
                OnPropertyChanged();
                if (!_suppressApplyFilters) ApplyFilters();
            }
        }

        public string SelectedBrand
        {
            get => _selectedBrand;
            set
            {
                if (_selectedBrand == value) return;
                _selectedBrand = value ?? "Tất cả";
                OnPropertyChanged();
                if (!_suppressApplyFilters) ApplyFilters();
            }
        }

        public string SelectedBikeType
        {
            get => _selectedBikeType;
            set
            {
                if (_selectedBikeType == value) return;
                _selectedBikeType = value ?? "Tất cả";
                OnPropertyChanged();
                if (!_suppressApplyFilters) ApplyFilters();
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            private set
            {
                if (_currentPage == value) return;
                _currentPage = value;
                OnPropertyChanged();
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            private set
            {
                if (_totalPages == value) return;
                _totalPages = value;
                OnPropertyChanged();
            }
        }

        public void Load()
        {
            BikePostRepository _repo = new BikePostRepository();
            List<Listing> items = _repo.GetAvailableListings();

            _all.Clear();
            foreach (Listing l in items)
            {
                string brand = l.Brand?.BrandName ?? "";
                string type = l.BikeType?.Name ?? "";
                string meta = string.IsNullOrWhiteSpace(type) ? brand : $"{brand} • {type}";
                string? firstImageUrl = l.ListingImages?.FirstOrDefault()?.ImageUrl;
                var sellerRating = l.Seller?.SellerProfile?.SellerRating ?? 0;
                var totalReviews = l.Seller?.SellerProfile?.TotalReviews ?? 0;

                _all.Add(new BicycleCardVm
                {
                    ListingId = l.ListingId,
                    Title = l.Title ?? "",
                    Price = l.Price ?? 0,
                    BrandName = brand,
                    BikeTypeName = type,
                    MetaText = meta,
                    FirstImageUrl = firstImageUrl,
                    SellerRating = sellerRating,
                    SellerTotalReviews = totalReviews
                });
            }

            BuildFilterOptions();
            ApplyFilters();
        }

        public void ApplyFilters()
        {
            var query = _all.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SelectedBrand) && SelectedBrand != "Tất cả")
            {
                query = query.Where(x => string.Equals(x.BrandName, SelectedBrand, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedBikeType) && SelectedBikeType != "Tất cả")
            {
                query = query.Where(x => string.Equals(x.BikeTypeName, SelectedBikeType, StringComparison.OrdinalIgnoreCase));
            }

            var term = (SearchText ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var lower = term.ToLowerInvariant();
                query = query.Where(x =>
                    (x.Title ?? "").ToLowerInvariant().Contains(lower) ||
                    (x.MetaText ?? "").ToLowerInvariant().Contains(lower));
            }

            _filtered.Clear();
            _filtered.AddRange(query);

            RebuildPaging();
            GoToPage(1);
        }

        public void GoToPage(int page)
        {
            if (TotalPages < 1) TotalPages = 1;

            if (page < 1) page = 1;
            if (page > TotalPages) page = TotalPages;

            CurrentPage = page;

            Listings.Clear();
            var start = (CurrentPage - 1) * PageSize;
            var pageItems = _filtered.Skip(start).Take(PageSize);
            foreach (var item in pageItems)
            {
                Listings.Add(item);
            }
        }

        public void NextPage()
        {
            GoToPage(CurrentPage + 1);
        }

        public void PrevPage()
        {
            GoToPage(CurrentPage - 1);
        }

        private void RebuildPaging()
        {
            var count = _filtered.Count;
            TotalPages = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));

            PageNumbers.Clear();
            for (int i = 1; i <= TotalPages; i++)
            {
                PageNumbers.Add(i);
            }
        }

        private void BuildFilterOptions()
        {
            _suppressApplyFilters = true;
            var selectedBrand = string.IsNullOrWhiteSpace(SelectedBrand) ? "Tất cả" : SelectedBrand;
            var selectedType = string.IsNullOrWhiteSpace(SelectedBikeType) ? "Tất cả" : SelectedBikeType;

            Brands.Clear();
            Brands.Add("Tất cả");

            BikeTypes.Clear();
            BikeTypes.Add("Tất cả");
            try
            {
                var categoryService = new CategoryManagementService(new CategoryRepository());

                foreach (var b in categoryService
                    .GetBrands()
                    .Select(x => x.BrandName ?? "")
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    Brands.Add(b);
                }

                foreach (var t in categoryService
                    .GetTypes()
                    .Select(x => x.Name ?? "")
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    BikeTypes.Add(t);
                }
            }
            catch
            {
                foreach (var b in _all.Select(x => x.BrandName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    Brands.Add(b);
                }

                foreach (var t in _all.Select(x => x.BikeTypeName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    BikeTypes.Add(t);
                }
            }

            if (!Brands.Contains(selectedBrand))
            {
                _selectedBrand = "Tất cả";
                OnPropertyChanged(nameof(SelectedBrand));
            }
            else
            {
                _selectedBrand = selectedBrand;
                OnPropertyChanged(nameof(SelectedBrand));
            }

            if (!BikeTypes.Contains(selectedType))
            {
                _selectedBikeType = "Tất cả";
                OnPropertyChanged(nameof(SelectedBikeType));
            }
            else
            {
                _selectedBikeType = selectedType;
                OnPropertyChanged(nameof(SelectedBikeType));
            }

            _suppressApplyFilters = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
