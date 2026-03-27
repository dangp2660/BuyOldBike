using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BuyOldBike_DAL.Repositories.Seller.ListingRepository;

namespace BuyOldBike_BLL.Services.Listings
{
    public class ListingModerationService
    {
        private readonly ListingModerationRepository _repo;

        public ListingModerationService() : this(new ListingModerationRepository())
        {
        }

        public ListingModerationService(ListingModerationRepository repo)
        {
            _repo = repo;
        }

        public List<Listing> GetListings(string? searchTerm, string? status)
        {
            return _repo.GetFilteredListings(searchTerm, status);
        }

        public (bool Success, string Message) ApproveListing(Guid listingId)
        {
            var listing = _repo.GetById(listingId);
            if (listing == null)
                return (false, "Không tìm thấy bài đăng.");
            if (listing.Status == StatusConstants.ListingStatus.Available)
                return (false, "Bài đăng đã được duyệt rồi.");

            var ok = _repo.UpdateStatus(listingId, StatusConstants.ListingStatus.Available);
            return ok
                ? (true, $"Đã duyệt bài: {listing.Title}")
                : (false, "Cập nhật thất bại.");
        }

        public (bool Success, string Message) RejectListing(Guid listingId)
        {
            var listing = _repo.GetById(listingId);
            if (listing == null)
                return (false, "Không tìm thấy bài đăng.");
            if (listing.Status == StatusConstants.ListingStatus.Rejected)
                return (false, "Bài đăng đã bị reject rồi.");

            var ok = _repo.UpdateStatus(listingId, StatusConstants.ListingStatus.Rejected);
            return ok
                ? (true, $"Đã reject bài: {listing.Title}")
                : (false, "Cập nhật thất bại.");
        }

        public (bool Success, string Message) RemoveListing(Guid listingId)
        {
            var listing = _repo.GetById(listingId);
            if (listing == null)
                return (false, "Không tìm thấy bài đăng.");

            var ok = _repo.UpdateStatus(listingId, StatusConstants.ListingStatus.Deleted);
            return ok
                ? (true, $"Đã xoá bài: {listing.Title}")
                : (false, "Cập nhật thất bại.");
        }
    }
}
