# BuyOldBike Feedback System Documentation

## Overview
The Feedback System enables buyers to review and rate sellers after completing transactions, helping build trust and transparency in the platform.

## Architecture

### Database Layer (DAL)

#### ReviewRepository
Location: `BuyOldBike_DAL/Repositories/Feedback/ReviewRepository.cs`

**Key Methods:**
- `GetReviewsBySellerId(sellerId)` - Get all reviews for a seller
- `GetReviewByOrderId(orderId)` - Get review for a specific order
- `GetAverageSellerRating(sellerId)` - Calculate average seller rating
- `GetSellerRatingStats(sellerId)` - Get detailed rating statistics with distribution
- `CreateReview(...)` - Create a new review
- `UpdateReview(...)` - Update existing review
- `DeleteReview(...)` - Delete a review
- `GetSellerReviewsPaginated(sellerId, pageNumber, pageSize)` - Paginated review retrieval

### Business Logic Layer (BLL)

#### ReviewService
Location: `BuyOldBike_BLL/Services/Feedback/ReviewService.cs`

**Key Methods:**
- `SubmitReview(orderId, buyerId, sellerId, rating, description)` - Submit a new review with validation
- `UpdateReview(reviewId, rating, description)` - Update review
- `DeleteReview(reviewId)` - Delete review
- `GetSellerReviews(sellerId)` - Get all reviews for a seller with ratings
- `GetSellerReviewsPaginated(sellerId, pageNumber, pageSize)` - Get paginated reviews
- `GetSellerRatingStats(sellerId)` - Get rating statistics
- `GetSellerAverageRating(sellerId)` - Get average rating

#### ReviewValidator
Location: `BuyOldBike_BLL/Features/Feedback/ReviewValidator.cs`

**Validation Rules:**
- Rating: Must be 1-5 stars
- Description: 10-1000 characters required
- User IDs: Must be valid and different (buyer ≠ seller)
- Order ID: Must be valid
- Prevents duplicate reviews per order

#### Data Transfer Objects (DTOs)
Location: `BuyOldBike_BLL/Features/Feedback/FeedbackDtos.cs`

- `SubmitReviewDto` - For submitting new reviews
- `ReviewDto` - For displaying reviews
- `SellerReputationDto` - For seller reputation/stats display

### Presentation Layer (UI)

#### SellerReviewsControl
Location: `BuyOldBike_Presentation/Controls/SellerReviewsControl.xaml`

**Features:**
- Display seller's average rating
- Show rating distribution (1-5 stars with count and percentage bars)
- List all reviews with pagination
- Display reviewer name, date, rating, and description
- Empty state when no reviews exist

**Usage:**
```csharp
var reviewsControl = new SellerReviewsControl();
reviewsControl.LoadSellerReviews(sellerId);
```

#### SubmitReviewControl
Location: `BuyOldBike_Presentation/Controls/SubmitReviewControl.xaml`

**Features:**
- Interactive 5-star rating selector
- Rich text description input (1000 character limit)
- Real-time character counter
- Form validation with error messages
- Seller information display
- Cancel and Submit buttons

**Usage:**
```csharp
var reviewForm = new SubmitReviewControl();
reviewForm.InitializeReview(orderId, buyerId, sellerId, "Seller Name");
reviewForm.ReviewSubmitted += (s, e) => {
    // Handle successful review submission
};
```

## Database Schema

### Review Entity
```
ReviewId (Guid) - Primary Key
OrderId (Guid) - Foreign Key to Order
BuyerId (Guid) - Foreign Key to User (Buyer)
SellerId (Guid) - Foreign Key to User (Seller)
Rating (int) - 1-5 stars
Description (string) - Review text
CreatedAt (DateTime) - Submission timestamp
```

### User Entity (Enhanced)
```
SellerRating (double) - Average seller rating
TotalReviews (int) - Total number of reviews received
LastReviewDate (DateTime) - Most recent review date
```

## Usage Examples

### Display Seller Reviews
```csharp
var reviewsControl = new SellerReviewsControl();
mainWindow.Content = reviewsControl;
reviewsControl.LoadSellerReviews(new Guid("seller-id-here"));
```

### Submit a Review
```csharp
var reviewForm = new SubmitReviewControl();
var stackPanel = new StackPanel();
stackPanel.Children.Add(reviewForm);

reviewForm.InitializeReview(
    orderId: new Guid("order-id"),
    buyerId: new Guid("buyer-id"),
    sellerId: new Guid("seller-id"),
    sellerName: "John Seller"
);

reviewForm.ReviewSubmitted += (s, e) => {
    MessageBox.Show("Review submitted successfully!");
    // Refresh reviews list
};
```

### Get Seller Statistics
```csharp
var reviewService = new ReviewService();
var (avgRating, totalCount, distribution) = reviewService.GetSellerRatingStats(sellerId);

Console.WriteLine($"Average: {avgRating}/5");
Console.WriteLine($"Total Reviews: {totalCount}");
Console.WriteLine($"5 Stars: {distribution[5]}");
Console.WriteLine($"4 Stars: {distribution[4]}");
// etc...
```

## Business Rules

1. **One Review Per Order** - A buyer can only submit one review per order
2. **No Self-Reviews** - Buyers cannot review themselves
3. **Rating Scale** - All reviews must have a rating between 1 and 5 stars
4. **Description Required** - Reviews must include a description of 10-1000 characters
5. **Immutable History** - Reviews cannot be deleted by users (managed by admin)
6. **Seller Reputation** - Seller rating is automatically calculated from all reviews

## Features

### For Buyers
- ✅ Submit review after purchase (1-5 stars + description)
- ✅ View seller's review history and reputation
- ✅ See review distribution breakdown
- ✅ Read other buyers' reviews before purchasing

### For Sellers
- ✅ View all reviews received
- ✅ See average rating and total review count
- ✅ View rating distribution
- ✅ Response mechanism (future enhancement)

### For Admin (Future)
- 🔄 Moderate reviews (remove inappropriate content)
- 🔄 Handle review disputes
- 🔄 Generate reputation reports
- 🔄 Track trending complaints

## Future Enhancements

1. **Seller Response** - Allow sellers to respond to reviews
2. **Review Photos** - Buyers can attach images/videos
3. **Helpful Votes** - Other users can mark reviews as helpful
4. **Review Filtering** - Filter by rating, date, keyword
5. **Review Reports** - Report inappropriate reviews
6. **Badge System** - Award "Verified Seller" badges based on ratings
7. **Reputation History** - Track rating changes over time
8. **Analytics Dashboard** - Detailed seller performance metrics

## Integration Checklist

- [x] Review entity created in DAL
- [x] ReviewRepository with full CRUD operations
- [x] ReviewService with business logic
- [x] ReviewValidator for input validation
- [x] User entity enhanced with reputation fields
- [x] SellerReviewsControl for viewing reviews
- [x] SubmitReviewControl for submitting reviews
- [ ] Integrate into Order completion workflow
- [ ] Add review notification emails
- [ ] Create admin review moderation panel
- [ ] Add review analytics dashboard

## Testing

### Unit Test Coverage Needed
- Review validation rules
- Rating statistics calculations
- Duplicate review prevention
- Character limit enforcement

### Integration Test Coverage Needed
- Create review → Update user rating
- Delete review → Recalculate user rating
- Pagination accuracy
- Data persistence

### UI Test Coverage Needed
- Star rating interaction
- Character counter accuracy
- Form submission validation
- Error message display

## Dependencies

- EntityFrameworkCore (DB access)
- System.Collections.ObjectModel (UI collections)
- Windows Presentation Foundation (WPF)

## Version History

- **v1.0** (Current) - Initial implementation with review submission and viewing
  - Basic 1-5 star rating system
  - Review submission with validation
  - Review viewing with pagination
  - Seller reputation statistics
