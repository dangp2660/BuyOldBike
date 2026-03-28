using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace BuyOldBike_DAL.Repositories.Admin
{
    public class AdminDashboardRepository
    {
        private readonly BuyOldBikeContext _db;

        public AdminDashboardRepository()
        {
            _db = new BuyOldBikeContext();
        }

        public int CountUsers()
        {
            try
            {
                return _db.Users.AsNoTracking().Count();
            }
            catch (DbException)
            {
                return 0;
            }
        }

        public int CountActiveListings()
        {
            try
            {
                return _db.Listings
                    .AsNoTracking()
                    .Count(l => l.Status == StatusConstants.ListingStatus.Available);
            }
            catch (DbException)
            {
                return 0;
            }
        }

        public int CountOrders()
        {
            try
            {
                return _db.Orders.AsNoTracking().Count();
            }
            catch (DbException)
            {
                return 0;
            }
        }

        public decimal SumSystemRevenue()
        {
            try
            {
                return _db.WalletTransactions
                    .AsNoTracking()
                    .Where(t => t.Type == "ListingPostFee" && t.Direction == "Debit")
                    .Sum(t => (decimal?)t.Amount) ?? 0m;
            }
            catch (DbException)
            {
                return 0m;
            }
        }

        public List<DashboardActivity> GetRecentActivities(int take)
        {
            if (take <= 0) return new List<DashboardActivity>();

            var listingActs = new List<DashboardActivity>();
            var orderActs = new List<DashboardActivity>();
            var walletActs = new List<DashboardActivity>();

            try
            {
                var listingRaw = _db.Listings
                    .AsNoTracking()
                    .Where(l => l.CreatedAt != null)
                    .OrderByDescending(l => l.CreatedAt)
                    .Select(l => new { l.CreatedAt, l.Title, l.Status })
                    .Take(take)
                    .ToList();

                listingActs = listingRaw
                    .Select(l => new DashboardActivity
                    {
                        CreatedAt = l.CreatedAt!.Value,
                        Message = $"Listing mới: {SafeTitle(l.Title)} ({l.Status})"
                    })
                    .ToList();
            }
            catch (DbException)
            {
                listingActs = new List<DashboardActivity>();
            }

            try
            {
                var orderRaw = _db.Orders
                    .AsNoTracking()
                    .Where(o => o.CreatedAt != null)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new { o.CreatedAt, o.OrderId, o.Status })
                    .Take(take)
                    .ToList();

                orderActs = orderRaw
                    .Select(o => new DashboardActivity
                    {
                        CreatedAt = o.CreatedAt!.Value,
                        Message = $"Giao dịch mới: {o.OrderId.ToString()[..8]} ({o.Status})"
                    })
                    .ToList();
            }
            catch (DbException)
            {
                orderActs = new List<DashboardActivity>();
            }

            try
            {
                var walletRaw = _db.WalletTransactions
                    .AsNoTracking()
                    .Where(t => t.CreatedAt != default)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new { t.CreatedAt, t.Type, t.Direction, t.Amount })
                    .Take(take)
                    .ToList();

                walletActs = walletRaw
                    .Select(t => new DashboardActivity
                    {
                        CreatedAt = t.CreatedAt,
                        Message = t.Type == "ListingPostFee"
                            ? $"Thu phí đăng bài: {t.Amount:N0} VND"
                            : $"Ví: {t.Type} {t.Direction} {t.Amount:N0} VND"
                    })
                    .ToList();
            }
            catch (DbException)
            {
                walletActs = new List<DashboardActivity>();
            }

            return listingActs
                .Concat(orderActs)
                .Concat(walletActs)
                .OrderByDescending(a => a.CreatedAt)
                .Take(take)
                .ToList();
        }

        private static string SafeTitle(string? title)
        {
            var t = (title ?? string.Empty).Trim();
            if (t.Length == 0) return "(không tiêu đề)";
            if (t.Length <= 60) return t;
            return t[..57] + "...";
        }
    }

    public class DashboardActivity
    {
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
