namespace BuyOldBike_BLL.Services.Users
{
    public sealed class UserAddressPrefill
    {
        public string PhoneNumber { get; init; } = "";
        public string FullName { get; init; } = "";
        public string Province { get; init; } = "";
        public string District { get; init; } = "";
        public string Ward { get; init; } = "";
        public string Detail { get; init; } = "";
    }
}
