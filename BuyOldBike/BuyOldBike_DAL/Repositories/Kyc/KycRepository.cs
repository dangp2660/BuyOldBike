using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_DAL.Repositories.Kyc
{
    public class KycRepository
    {
        private BuyOldBikeContext _db = new();
        public KycRepository() { }
        public bool CreateProfileWithImages(KycProfile profile, IEnumerable<KycImage> images)
        {
            _db.KycProfiles.Add(profile);
            _db.KycImages.AddRange(images);
            return _db.SaveChanges() > 0;
        }

        public KycProfile? GetKycProfile(Guid userId)
        {
            return _db.KycProfiles.Include(p => p.KycImages)
                .Where(GetKycProfile => GetKycProfile.UserId == userId)
                .OrderByDescending(p => p.VerifiedAt)
                .FirstOrDefault();
        }
    }
}
