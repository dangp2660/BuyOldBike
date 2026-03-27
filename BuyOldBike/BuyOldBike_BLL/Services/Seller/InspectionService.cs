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
            ProcessInspection(inspectionId, isPassed, 0, null);
        }

        public void ProcessInspection(Guid inspectionId, bool isPassed, int overallScore, string? notes)
        {
            string result = isPassed ? StatusConstants.InspectionResult.Passed : StatusConstants.InspectionResult.Failed;
            string listingStatus = isPassed ? StatusConstants.ListingStatus.Available : StatusConstants.ListingStatus.Rejected;

            _bikePostRepository.UpdateInspectionResult(inspectionId, result, listingStatus, overallScore, notes);
        }

        public void ProcessInspection(Guid inspectionId, bool isPassed, int overallScore, string? notes,
            IReadOnlyDictionary<string, bool> componentResults)
        {
            ProcessInspection(inspectionId, isPassed, overallScore, notes, componentResults, Enumerable.Empty<string>());
        }

        public void ProcessInspection(Guid inspectionId, bool isPassed, int overallScore, string? notes,
            IReadOnlyDictionary<string, bool> componentResults, IEnumerable<string> reportImageUrls)
        {
            string result = isPassed ? StatusConstants.InspectionResult.Passed : StatusConstants.InspectionResult.Failed;
            string listingStatus = isPassed ? StatusConstants.ListingStatus.Available : StatusConstants.ListingStatus.Rejected;

            _bikePostRepository.UpdateInspectionResult(inspectionId, result, listingStatus, overallScore, notes, componentResults, reportImageUrls);
        }

        public List<InspectionImage> GetInspectionImages(Guid inspectionId)
        {
            return _bikePostRepository.GetInspectionImages(inspectionId);
        }
    }
}
