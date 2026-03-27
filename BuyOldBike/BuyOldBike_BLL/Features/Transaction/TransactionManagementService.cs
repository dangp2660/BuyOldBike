using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Transaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_BLL.Features.Transaction
{
    public class TransactionManagementService
    {
        private readonly TransactionRepository _repo;

        public TransactionManagementService(TransactionRepository repo)
        {
            _repo = repo;
        }

        public List<Order> GetOrders(string? searchTerm, string? status)
        {
            return _repo.GetFilteredOrders(searchTerm, status);
        }

        public Order? GetOrderDetail(Guid orderId)
        {
            return _repo.GetById(orderId);
        }
    }
}
