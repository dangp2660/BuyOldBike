using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Seller;
using BuyOldBike_DAL.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuyOldBike_BLL.Services.Seller
{
    public class InspectionService
    {
        private readonly BikePostRepository _bikePostRepository;

        public InspectionService()
        {
            _bikePostRepository = new BikePostRepository();
        }
            
        public List<Inspection> GetPendingRequests()
        {
            return  _bikePostRepository.GetPendingInspections();
        }

        public void ProcessInspection(Guid inspectionId, bool isPassed)
        {
            string result = isPassed ? StatusConstants.InspectionResult.Passed : StatusConstants.InspectionResult.Failed;
            string listingStatus = isPassed ? StatusConstants.ListingStatus.Available : StatusConstants.ListingStatus.Rejected;

            _bikePostRepository.UpdateInspectionResult(inspectionId, result, listingStatus);
        }
    }
}
