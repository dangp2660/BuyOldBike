using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Seller;


namespace BuyOldBike_BLL.Services.BicycleListWindow
{
    public class ListingBrowseService
    {
        private readonly BikePostRepository _repo;

        public ListingBrowseService()
        {
            _repo = new BikePostRepository();
        }

        public List<Listing> GetAvailableListings()
        {
            return _repo.GetAvailableListings();
        }
    }
}
