using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_DAL.Repositories.Transaction
{
    public class TransactionRepository
    {
        private readonly BuyOldBikeContext _db;

        public TransactionRepository()
        {
            _db = new BuyOldBikeContext();
        }

        public List<Order> GetFilteredOrders(string? searchTerm, string? status)
        {
            var query = _db.Orders
                           .Include(o => o.Buyer)
                           .Include(o => o.Listing)
                           .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lower = searchTerm.ToLower();
                query = query.Where(o =>
                    o.OrderId.ToString().Contains(lower) ||
                    (o.Buyer != null && o.Buyer.Email.ToLower().Contains(lower)) ||
                    (o.Listing != null && o.Listing.Seller != null &&
                     o.Listing.Seller.Email.ToLower().Contains(lower)));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All status")
                query = query.Where(o => o.Status == status);

            return query.OrderByDescending(o => o.CreatedAt).ToList();
        }

        public Order? GetById(Guid orderId)
        {
            return _db.Orders
                      .Include(o => o.Buyer)
                      .Include(o => o.Listing)
                          .ThenInclude(l => l!.Seller)
                      .Include(o => o.Payments)
                      .FirstOrDefault(o => o.OrderId == orderId);
        }
        public List<Order> GetAllTransactions()
        {
            return _db.Orders
                .Include(o => o.Buyer)
                .Include(o => o.Listing)
                    .ThenInclude(l => l!.Seller)
                .Include(o => o.Payments)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();
        }
    }
}
