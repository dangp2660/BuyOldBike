using BuyOldBike_DAL.Entities;
using System.Collections.Generic;
using System.Linq;

namespace BuyOldBike_BLL.Validators
{
    public static class ListingValidator
    {
        // Validate listing for publish. imageUrls are relative urls already persisted by LocalMediaService.
        // Returns dictionary: field -> error message. Empty dictionary = valid.
        public static Dictionary<string, string> ValidateForPublish(Listing listing, IEnumerable<string> imageUrls)
        {
            var errors = new Dictionary<string, string>();

            if (listing == null)
            {
                errors["Listing"] = "Listing object is required.";
                return errors;
            }

            if (string.IsNullOrWhiteSpace(listing.Title))
                errors["Title"] = "Title is required.";

            if (listing.Price == null || listing.Price <= 0)
                errors["Price"] = "Price must be greater than zero.";

            if (listing.BrandId == null)
                errors["Brand"] = "Brand is required.";

            if (imageUrls == null || !imageUrls.Any())
                errors["Images"] = "At least one image is required.";

            return errors;
        }
    }
}