using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BuyOldBike_DAL.Repositories.Seller
{
    public class OrderRepository
    {
        private readonly BuyOldBikeContext _db;

        public OrderRepository()
        {
            _db = new BuyOldBikeContext();
        }

        public List<Order> GetOrdersBySellerId(Guid sellerId)
        {
            return _db.Orders
                .Where(o => o.Listing != null && o.Listing.SellerId == sellerId)
                .Include(o => o.Buyer)
                .Include(o => o.Listing)
                    .ThenInclude(l => l.Seller)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();
        }

        public void UpdateOrderStatus(Guid orderId, string newStatus)
        {
            var o = _db.Orders.Find(orderId);
            if (o == null) return;
            o.Status = newStatus;
            _db.SaveChanges();
        }
    }
}
