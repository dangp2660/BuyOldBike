using BuyOldBike_DAL.Constants;
using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BuyOldBike_DAL.Repositories.Seller
{
    public class BikePostRepository
    {
        private static readonly (int Id, string Name)[] DefaultInspectionComponents =
        [
            (1, "Frame System"),
            (2, "Brake System"),
            (3, "Drivetrain"),
            (4, "Handlebar and Steering"),
        ];

        public void SaveFullListing(Listing listing, List<string> imageUrls, Inspection inspection)
        {
            using var db = new BuyOldBikeContext();
            using var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                if (listing.SellerId == null) throw new InvalidOperationException("Thiếu thông tin người bán.");
                if (listing.Price == null || listing.Price <= 0) throw new InvalidOperationException("Giá xe không hợp lệ.");

                var postingFee = Math.Round(listing.Price.Value * 0.05m, 0, MidpointRounding.AwayFromZero);
                if (postingFee > 0)
                {
                    var wallet = db.UserWallets.FirstOrDefault(w => w.UserId == listing.SellerId.Value);
                    if (wallet == null)
                    {
                        wallet = new UserWallet
                        {
                            WalletId = Guid.NewGuid(),
                            UserId = listing.SellerId.Value,
                            Balance = 0m,
                            UpdatedAt = DateTime.Now
                        };
                        db.UserWallets.Add(wallet);
                        db.SaveChanges();
                    }

                    if (wallet.Balance < postingFee)
                        throw new InvalidOperationException("Số dư ví không đủ để trả phí đăng bài (5%).");

                    wallet.Balance -= postingFee;
                    wallet.UpdatedAt = DateTime.Now;
                    db.WalletTransactions.Add(new WalletTransaction
                    {
                        WalletTransactionId = Guid.NewGuid(),
                        WalletId = wallet.WalletId,
                        Amount = postingFee,
                        Direction = "Debit",
                        Type = "ListingPostFee",
                        Note = $"Phí đăng bài 5% cho xe: {listing.Title}",
                        CreatedAt = DateTime.Now
                    });
                    db.SaveChanges();
                }

                db.Listings.Add(listing);
                db.SaveChanges();

                foreach (var url in imageUrls)
                {
                    var img = new ListingImage
                    {
                        ImageId = Guid.NewGuid(),
                        ListingId = listing.ListingId,
                        ImageUrl = url
                    };
                    db.ListingImages.Add(img);
                }

                inspection.ListingId = listing.ListingId;
                inspection.InspectionLocationId = EnsureInspectionLocationId(db, inspection.InspectionLocationId);
                if (inspection.CreatedAt == default)
                {
                    inspection.CreatedAt = DateTime.Now;
                }
                db.Inspections.Add(inspection);

                db.SaveChanges();

                EnsureInspectionComponentsSeeded(db);
                EnsureInspectionScoresSeededForInspection(db, inspection.InspectionId);
                db.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void EnsureInspectionCatalogSeeded()
        {
            using var db = new BuyOldBikeContext();
            EnsureInspectionComponentsSeeded(db);
        }

        public void UpdateListing(Listing listing)
        {
            using var db = new BuyOldBikeContext();

            var existing = db.Listings.Find(listing.ListingId);
            if (existing == null) return;
            if (!string.Equals(existing.Status, StatusConstants.ListingStatus.Pending_Inspection, StringComparison.Ordinal))
                throw new InvalidOperationException("Chỉ được chỉnh sửa bài đăng trước khi kiểm định (trạng thái 'Pending_Inspection').");

            existing.Title = listing.Title;
            existing.Description = listing.Description;
            existing.Price = listing.Price;
            existing.BrandId = listing.BrandId;
            existing.BikeTypeId = listing.BikeTypeId;
            existing.FrameNumber = listing.FrameNumber;
            existing.UsageDuration = listing.UsageDuration;
            existing.Status = listing.Status ?? existing.Status;

            db.SaveChanges();
        }

        public void HideListing(Guid listingId)
        {
            using var db = new BuyOldBikeContext();

            var l = db.Listings.Find(listingId);
            if (l == null) return;
            if (!string.Equals(l.Status, StatusConstants.ListingStatus.Available, StringComparison.Ordinal))
                throw new InvalidOperationException("Chỉ được ẩn bài đăng khi đang ở trạng thái 'Available'.");
            l.Status = StatusConstants.ListingStatus.Hidden;

            db.SaveChanges();
        }

        public void UnhideListing(Guid listingId)
        {
            using var db = new BuyOldBikeContext();

            var l = db.Listings.Find(listingId);
            if (l == null) return;
            if (!string.Equals(l.Status, StatusConstants.ListingStatus.Hidden, StringComparison.Ordinal))
                throw new InvalidOperationException("Chỉ được hiển thị lại bài đăng khi đang ở trạng thái 'Hidden'.");
            l.Status = StatusConstants.ListingStatus.Available;

            db.SaveChanges();
        }

        public void AddListingImages(Guid listingId, List<string> imageUrls)
        {
            using var db = new BuyOldBikeContext();

            var listing = db.Listings.Find(listingId);
            if (listing == null) return;
            if (!string.Equals(listing.Status, StatusConstants.ListingStatus.Pending_Inspection, StringComparison.Ordinal))
                throw new InvalidOperationException("Chỉ được thêm ảnh trước khi kiểm định (trạng thái 'Pending_Inspection').");

            foreach (var url in imageUrls)
            {
                db.ListingImages.Add(new ListingImage
                {
                    ImageId = Guid.NewGuid(),
                    ListingId = listingId,
                    ImageUrl = url
                });
            }

            db.SaveChanges();
        }

        public void SoftDeleteListing(Guid listingId)
        {
            using var db = new BuyOldBikeContext();

            var l = db.Listings.Find(listingId);
            if (l == null) return;
            if (!string.Equals(l.Status, StatusConstants.ListingStatus.Available, StringComparison.Ordinal))
                throw new InvalidOperationException("Chỉ được xóa bài đăng khi đang ở trạng thái 'Available'.");
            l.Status = StatusConstants.ListingStatus.Deleted;

            db.SaveChanges();
        }

        public List<Inspection> GetPendingInspections()
        {
            using var db = new BuyOldBikeContext();

            EnsureInspectionComponentsSeeded(db);

            var inspections = db.Inspections
                .Include(i => i.Listing)
                .ThenInclude(l => l.Seller)
                .Where(i => i.Status == StatusConstants.InspectionStatus.Pending)
                .OrderByDescending(i => i.CreatedAt)
                .ToList();

            if (inspections.Count > 0)
            {
                var inspectionIds = inspections.Select(i => i.InspectionId).Distinct().ToList();
                EnsureInspectionScoresSeededForInspections(db, inspectionIds);
                db.SaveChanges();
            }

            return inspections;
        }

        public List<InspectionImage> GetInspectionImages(Guid inspectionId)
        {
            using var db = new BuyOldBikeContext();
            return db.InspectionImages
                .AsNoTracking()
                .Where(i => i.InspectionId == inspectionId)
                .OrderByDescending(i => i.CreatedAt)
                .ToList();
        }

        public void UpdateInspectionResult(Guid inspectionId, string result,
            string listingStatus, int overallScore, string? notes)
        {
            UpdateInspectionResult(inspectionId, result, listingStatus, overallScore, notes, Enumerable.Empty<string>());
        }

        public void UpdateInspectionResult(Guid inspectionId, string result,
            string listingStatus, int overallScore, string? notes,
            IEnumerable<string> reportImageUrls)
        {
            using var db = new BuyOldBikeContext();

            Inspection? inspection = db.Inspections.Find(inspectionId);
            if (inspection != null)
            {
                inspection.Status = StatusConstants.InspectionStatus.Completed;
                inspection.Result = result;
                inspection.OverallScore = overallScore;

                if (result == StatusConstants.InspectionResult.Passed)
                {
                    inspection.RejectReason = null;
                }
                else
                {
                    inspection.RejectReason = notes;
                }

                Listing? listing = db.Listings.Find(inspection.ListingId);
                if (listing != null)
                {
                    listing.Status = listingStatus;
                }

                var normalizedImageUrls = (reportImageUrls ?? Enumerable.Empty<string>())
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Select(u => u.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (normalizedImageUrls.Count > 0)
                {
                    var existing = db.InspectionImages
                        .Where(i => i.InspectionId == inspectionId)
                        .Select(i => i.ImageUrl)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    foreach (var url in normalizedImageUrls)
                    {
                        if (existing.Contains(url)) continue;
                        db.InspectionImages.Add(new InspectionImage
                        {
                            ImageId = Guid.NewGuid(),
                            InspectionId = inspectionId,
                            ImageUrl = url,
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                db.SaveChanges();
            }
        }

        public void UpdateInspectionResult(Guid inspectionId, string result,
            string listingStatus, int overallScore, string? notes,
            IReadOnlyDictionary<string, bool> componentResults)
        {
            UpdateInspectionResult(inspectionId, result, listingStatus, overallScore, notes, componentResults, Enumerable.Empty<string>());
        }

        public void UpdateInspectionResult(Guid inspectionId, string result,
            string listingStatus, int overallScore, string? notes,
            IReadOnlyDictionary<string, bool> componentResults,
            IEnumerable<string> reportImageUrls)
        {
            using var db = new BuyOldBikeContext();

            EnsureInspectionComponentsSeeded(db);
            EnsureInspectionScoresSeededForInspection(db, inspectionId);

            var normalized = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in componentResults)
            {
                if (!string.IsNullOrWhiteSpace(kv.Key))
                {
                    normalized[kv.Key.Trim()] = kv.Value;
                }
            }

            foreach (var (id, name) in DefaultInspectionComponents)
            {
                normalized.TryGetValue(name, out var passed);

                var score = db.InspectionScores.Find(inspectionId, id);
                if (score == null)
                {
                    score = new InspectionScore
                    {
                        InspectionId = inspectionId,
                        ComponentId = id
                    };
                    db.InspectionScores.Add(score);
                }

                score.Score = passed ? 1 : 0;
            }

            Inspection? inspection = db.Inspections.Find(inspectionId);
            if (inspection != null)
            {
                inspection.Status = StatusConstants.InspectionStatus.Completed;
                inspection.Result = result;
                inspection.OverallScore = overallScore;

                if (result == StatusConstants.InspectionResult.Passed)
                {
                    inspection.RejectReason = null;
                }
                else
                {
                    inspection.RejectReason = notes;
                }

                Listing? listing = db.Listings.Find(inspection.ListingId);
                if (listing != null)
                {
                    listing.Status = listingStatus;
                }
            }

            var normalizedImageUrls = (reportImageUrls ?? Enumerable.Empty<string>())
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedImageUrls.Count > 0)
            {
                var existing = db.InspectionImages
                    .Where(i => i.InspectionId == inspectionId)
                    .Select(i => i.ImageUrl)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var url in normalizedImageUrls)
                {
                    if (existing.Contains(url)) continue;
                    db.InspectionImages.Add(new InspectionImage
                    {
                        ImageId = Guid.NewGuid(),
                        InspectionId = inspectionId,
                        ImageUrl = url,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            db.SaveChanges();
        }

        public List<Listing> GetListingsBySellerId(Guid sellerId)
        {
            using var db = new BuyOldBikeContext();

            return db.Listings
                .Include(l => l.Brand)
                .Include(l => l.BikeType)
                .Where(l => l.SellerId == sellerId)
                .Where(l => l.Status != StatusConstants.ListingStatus.Deleted)
                .OrderByDescending(l => l.CreatedAt)
                .ToList();
        }

        private Guid EnsureInspectionLocationId(BuyOldBikeContext db, Guid inspectionLocationId)
        {
            if (inspectionLocationId != Guid.Empty)
            {
                var exists = db.InspectionLocations.Any(x => x.InspectionLocationId == inspectionLocationId);
                if (exists) return inspectionLocationId;
            }

            var existing = db.InspectionLocations
                .OrderBy(x => x.Type)
                .Select(x => x.InspectionLocationId)
                .FirstOrDefault();

            if (existing != Guid.Empty) return existing;

            var location = new InspectionLocation
            {
                InspectionLocationId = Guid.NewGuid(),
                Type = "Default",
                AddressLine = "Default",
                City = "Default"
            };
            db.InspectionLocations.Add(location);
            db.SaveChanges();
            return location.InspectionLocationId;
        }

        private void EnsureInspectionComponentsSeeded(BuyOldBikeContext db)
        {
            var existingIds = db.InspectionComponents.Select(x => x.ComponentId).ToHashSet();
            var toAdd = new List<InspectionComponent>();

            foreach (var (id, name) in DefaultInspectionComponents)
            {
                if (!existingIds.Contains(id))
                {
                    toAdd.Add(new InspectionComponent
                    {
                        ComponentId = id,
                        ComponentName = name
                    });
                }
            }

            if (toAdd.Count > 0)
            {
                db.InspectionComponents.AddRange(toAdd);
                db.SaveChanges();
            }
        }

        private void EnsureInspectionScoresSeededForInspection(BuyOldBikeContext db, Guid inspectionId)
        {
            var defaultComponentIds = DefaultInspectionComponents.Select(x => x.Id).ToArray();

            var existing = db.InspectionScores
                .Where(s => s.InspectionId == inspectionId && defaultComponentIds.Contains(s.ComponentId))
                .Select(s => s.ComponentId)
                .ToHashSet();

            var toAdd = new List<InspectionScore>();
            foreach (var componentId in defaultComponentIds)
            {
                if (!existing.Contains(componentId))
                {
                    toAdd.Add(new InspectionScore
                    {
                        InspectionId = inspectionId,
                        ComponentId = componentId,
                        Score = null,
                        Note = null
                    });
                }
            }

            if (toAdd.Count > 0)
            {
                db.InspectionScores.AddRange(toAdd);
            }
        }

        private void EnsureInspectionScoresSeededForInspections(BuyOldBikeContext db, IReadOnlyCollection<Guid> inspectionIds)
        {
            if (inspectionIds.Count == 0) return;

            var defaultComponentIds = DefaultInspectionComponents.Select(x => x.Id).ToArray();

            var existingPairs = db.InspectionScores
                .Where(s => inspectionIds.Contains(s.InspectionId) && defaultComponentIds.Contains(s.ComponentId))
                .Select(s => new { s.InspectionId, s.ComponentId })
                .ToList();

            var existing = new HashSet<(Guid InspectionId, int ComponentId)>(
                existingPairs.Select(x => (x.InspectionId, x.ComponentId)));

            var toAdd = new List<InspectionScore>();
            foreach (var inspectionId in inspectionIds)
            {
                foreach (var componentId in defaultComponentIds)
                {
                    if (!existing.Contains((inspectionId, componentId)))
                    {
                        toAdd.Add(new InspectionScore
                        {
                            InspectionId = inspectionId,
                            ComponentId = componentId,
                            Score = null,
                            Note = null
                        });
                    }
                }
            }

            if (toAdd.Count > 0)
            {
                db.InspectionScores.AddRange(toAdd);
            }
        }

        public List<Listing> GetAvailableListings()
        {
            using var db = new BuyOldBikeContext();

            return db.Listings
                .Include(l => l.Brand)
                .Include(l => l.BikeType)
                .Include(l => l.ListingImages)
                .Include(l => l.Seller)
                    .ThenInclude(s => s.SellerProfile)
                .Where(l => l.Status == StatusConstants.ListingStatus.Available)
                .OrderByDescending(l => l.CreatedAt)
                .ToList();
        }

        public Listing? GetListingDetailById(Guid listingId)
        {
            using var db = new BuyOldBikeContext();

            return db.Listings
                .Include(l => l.Brand)
                .Include(l => l.BikeType)
                .Include(l => l.ListingImages)
                .FirstOrDefault(l => l.ListingId == listingId);
        }

        public void IncrementListingViews(Guid listingId)
        {
            using var db = new BuyOldBikeContext();
            db.Database.ExecuteSqlInterpolated($"UPDATE dbo.listings SET views = views + 1 WHERE listing_id = {listingId}");
        }
    }
}
