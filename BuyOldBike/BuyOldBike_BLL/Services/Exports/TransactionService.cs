using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Transaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_BLL.Services.Exports
{
    public class TransactionService
    {
        private readonly TransactionRepository _repo;

        public TransactionService()
        {
            _repo = new TransactionRepository();
        }

        public List<Order> GetTransactions()
        {
            return _repo.GetAllTransactions();
        }
    }
}
