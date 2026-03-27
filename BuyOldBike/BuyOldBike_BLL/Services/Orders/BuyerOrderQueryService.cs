using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuyOldBike_BLL.Services.Orders
{
    public class BuyerOrderQueryService
    {
        public List<Order> GetDepositOrders(Guid buyerId)
        {
            using var db = new BuyOldBikeContext();

            return db.Orders
                .AsNoTracking()
                .Where(o => o.BuyerId == buyerId &&
                            (o.Status == StatusConstants.OrdersStatus.Deposit_Pending ||
                             o.Status == StatusConstants.OrdersStatus.Deposit_Paid ||
                             o.Status == StatusConstants.OrdersStatus.Paid ||
                             o.Status == StatusConstants.OrdersStatus.Deposit_Failed ||
                             o.Status == StatusConstants.OrdersStatus.Deposit_Expired ||
                             o.Status == StatusConstants.OrdersStatus.Disputed ||
                             o.Status == StatusConstants.OrdersStatus.Dispute_Resolved))
                .Include(o => o.Listing)
                .Include(o => o.Payments)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();
        }
    }
}
