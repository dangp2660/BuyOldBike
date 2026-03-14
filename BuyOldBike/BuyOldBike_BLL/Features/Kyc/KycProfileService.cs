using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Kyc;
using System;

namespace BuyOldBike_BLL.Services.Kyc
{
    public class KycProfileService
    {
        private readonly KycRepository _kycRepository = new();

        public KycProfile? GetLatestProfile(Guid userId)
        {
            return _kycRepository.GetKycProfile(userId);
        }
    }
}
