using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Seller;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BuyOldBike_Presentation.ViewModels
{
    public class BicycleListWindowViewModel : INotifyPropertyChanged
    {
        private readonly List<BicycleCardVm> _all = new List<BicycleCardVm>();
        private int _currentPage = 1;
        private int _totalPages = 1;

        public ObservableCollection<BicycleCardVm> Listings { get; } = new ObservableCollection<BicycleCardVm>();
        public ObservableCollection<int> PageNumbers { get; } = new ObservableCollection<int>();

        public int PageSize { get; } = 25;

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

                _all.Add(new BicycleCardVm
                {
                    ListingId = l.ListingId,
                    Title = l.Title ?? "",
                    Price = l.Price ?? 0,
                    MetaText = meta,
                    FirstImageUrl = firstImageUrl
                });
            }

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
            var pageItems = _all.Skip(start).Take(PageSize);
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
            var count = _all.Count;
            TotalPages = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));

            PageNumbers.Clear();
            for (int i = 1; i <= TotalPages; i++)
            {
                PageNumbers.Add(i);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
