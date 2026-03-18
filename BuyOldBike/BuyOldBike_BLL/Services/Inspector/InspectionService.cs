using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Seller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_BLL.Services.Inspector
{
    public class InspectionService
    {
        private readonly BikePostRepository _bikePostRepository;
        public InspectionService()
        {
            _bikePostRepository = new BikePostRepository(); 
        }

        public List<Inspection> GetPendingInspection()
        {
           return _bikePostRepository.GetPendingInspections();
        }

        public void ProcessInspection(Guid inspectionID, bool isPassed, int overallScore, string ?note)
        {
            string resuilt = isPassed ? StatusConstants.InspectionResult.Passed : 
                StatusConstants.InspectionResult.Failed;
            string listingStatus = isPassed ? StatusConstants.ListingStatus.Available : 
                StatusConstants.ListingStatus.Rejected;
            _bikePostRepository.UpdateInspectionResult(inspectionID,resuilt, listingStatus, overallScore, note);
        }
    }
}
