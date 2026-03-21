using BuyOldBike_DAL.Entities;

public partial class KycImage
{
    public Guid ImageId { get; set; }
    public Guid KycId { get; set; }

    public string? ImageType { get; set; }
    public string? ImageUrl { get; set; }

    public byte[]? ImageData { get; set; }
    public string? ContentType { get; set; }
    public string? FileName { get; set; }

    public DateTime? CreatedAt { get; set; }
    public virtual KycProfile Kyc { get; set; } = null!;
}