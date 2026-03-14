using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Kyc;
using BuyOldBike_DAL.Repositories.Auth;
using Microsoft.EntityFrameworkCore;

namespace BuyOldBike_BLL.Services.Kyc
{
    public class EkycService
    {
        private readonly EkycOcrService _ocrService;
        private readonly KycRepository _kycRepo = new();
        private readonly UserRepository _usersRepo = new();

        public EkycService(EkycOcrService ocr) => _ocrService = ocr;

        public KycExtractResult ExtractKycInfo(byte[] frontImage, byte[] backImage, byte[] selfie)
        {
            if(frontImage.Length == 0 || backImage.Length == 0 || selfie.Length == 0)
                throw new ArgumentException("Ảnh CCCD mặt trước, mặt sau và ảnh selfie không được để trống.");
            return _ocrService.ExtractFromCccd(frontImage, backImage);
        }

        public bool RegisterBuyer(string email, string phone, string password,
            byte[] front, byte[] back, byte[] selfie)
        {
            KycExtractResult extractResult = ExtractKycInfo(front, back, selfie);
            return RegisterBuyer(email, phone, password, extractResult, front, back, selfie);
        }

        public bool RegisterBuyer(string email, string phone, string password, KycExtractResult extractResult,
            byte[] front, byte[] back, byte[] selfie)
        {
            if (string.IsNullOrWhiteSpace(extractResult.IdNumber) ||
                string.IsNullOrWhiteSpace(extractResult.FullName) ||
                string.IsNullOrWhiteSpace(extractResult.DateOfBirth))
                throw new ArgumentException("Thông tin eKYC không hợp lệ.");

            BuyOldBikeContext db = new();
            var transaction = db.Database.BeginTransaction();

            User? existing = db.Users.FirstOrDefault(u => u.Email == email);
            if(existing != null) return false;

            User user = new()
            {
                UserId = Guid.NewGuid(),
                Email = email,
                PhoneNumber = phone,
                Password = password,
                Role = "Buyer"
            };
            db.Users.Add(user);

            KycProfile kycProfile = new KycProfile()
            {
                KycId = Guid.NewGuid(),
                UserId = user.UserId,
                IdNumber = extractResult.IdNumber,
                FullName = extractResult.FullName,
                DateOfBirth = extractResult.DateOfBirth,
                VerifiedAt = DateTime.UtcNow,
                Gender = extractResult.Gender,
                Nationality = extractResult.Nationality,
                PlaceOfOrigin = extractResult.PlaceOfOrigin,
                PlaceOfResidence = extractResult.PlaceOfResidence,
                ExpiryDate = extractResult.ExpiryDate,
            };
            db.KycProfiles.Add(kycProfile);

            db.KycImages.AddRange(new List<KycImage>()
            {
                new KycImage()
                {
                    ImageId = Guid.NewGuid(),
                    KycId = kycProfile.KycId,
                    ImageType = "Front",
                    ImageUrl = "",
                    ImageData = front
                },
                new KycImage()
                {
                    ImageId = Guid.NewGuid(),
                    KycId = kycProfile.KycId,
                    ImageType = "Back",
                    ImageUrl = "",
                    ImageData = back
                },
                new KycImage()
                {
                    ImageId = Guid.NewGuid(),
                    KycId = kycProfile.KycId,
                    ImageType = "Selfie",
                    ImageUrl = "",
                    ImageData = selfie
                }
            });

            bool success = db.SaveChanges() > 0;

            transaction.Commit();
            return success;
        }
    }
}
