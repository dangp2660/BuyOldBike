using BuyOldBike_DAL.Entities;
using System.Collections.Generic;

namespace BuyOldBike_BLL.Services.Dispute;

public sealed class DisputeImageResult
{
    public List<ReturnRequestImage> BuyerImages { get; set; } = [];

    public List<ImageUrlItem> InspectorImages { get; set; } = [];
}

public sealed class ImageUrlItem
{
    public string ImageUrl { get; set; } = string.Empty;
}

