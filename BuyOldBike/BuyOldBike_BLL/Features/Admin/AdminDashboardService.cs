using BuyOldBike_DAL.Repositories.Admin;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BuyOldBike_BLL.Features.Admin
{
    public class AdminDashboardService
    {
        private readonly AdminDashboardRepository _repo;

        public AdminDashboardService()
        {
            _repo = new AdminDashboardRepository();
        }

        public AdminDashboardStats GetDashboardStats(int recentActivitiesLimit = 10)
        {
            var stats = new AdminDashboardStats
            {
                TotalUsers = _repo.CountUsers(),
                ActiveListings = _repo.CountActiveListings(),
                TotalTransactions = _repo.CountOrders(),
                SystemRevenue = _repo.SumSystemRevenue()
            };

            var activities = _repo.GetRecentActivities(recentActivitiesLimit)
                .Select(a => new AdminDashboardActivity
                {
                    CreatedAt = a.CreatedAt,
                    Message = a.Message
                })
                .ToList();

            stats.RecentActivities = activities;
            return stats;
        }
    }

    public class AdminDashboardStats
    {
        public int TotalUsers { get; set; }
        public int ActiveListings { get; set; }
        public int TotalTransactions { get; set; }
        public decimal SystemRevenue { get; set; }
        public List<AdminDashboardActivity> RecentActivities { get; set; } = new();
    }

    public class AdminDashboardActivity
    {
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

