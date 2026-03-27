using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Seller;
using System;
using System.Collections.Generic;

namespace BuyOldBike_BLL.Services.Seller
{
    public class OrderService
    {
        private readonly OrderRepository _repo;

        public OrderService()
        {
            _repo = new OrderRepository();
        }

        public List<Order> GetOrdersBySellerId(Guid sellerId)
        {
            return _repo.GetOrdersBySellerId(sellerId);
        }

        public void UpdateOrderStatus(Guid orderId, string newStatus)
        {
            _repo.UpdateOrderStatus(orderId, newStatus);
        }

        public Order BuyBikeWithWallet(Guid buyerId, Guid listingId)
        {
            return _repo.BuyBikeWithWallet(buyerId, listingId);
        }
    }
}