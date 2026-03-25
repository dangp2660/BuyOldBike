using System;
using System.Collections.Generic;

namespace BuyOldBike_BLL.Features.Feedback
{
    public class ReviewValidator
    {
        private readonly List<string> _errors = new();

        public ReviewValidator ValidateRating(int rating)
        {
            if (rating < 1 || rating > 5)
            {
                _errors.Add("Rating must be between 1 and 5 stars");
            }
            return this;
        }

        public ReviewValidator ValidateDescription(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                _errors.Add("Description cannot be empty");
            }
            else if (description.Length < 10)
            {
                _errors.Add("Description must be at least 10 characters");
            }
            else if (description.Length > 1000)
            {
                _errors.Add("Description cannot exceed 1000 characters");
            }
            return this;
        }

        public ReviewValidator ValidateUserIds(Guid buyerId, Guid sellerId)
        {
            if (buyerId == Guid.Empty)
            {
                _errors.Add("Invalid buyer ID");
            }
            if (sellerId == Guid.Empty)
            {
                _errors.Add("Invalid seller ID");
            }
            if (buyerId == sellerId)
            {
                _errors.Add("Buyer and seller cannot be the same");
            }
            return this;
        }

        public ReviewValidator ValidateOrderId(Guid orderId)
        {
            if (orderId == Guid.Empty)
            {
                _errors.Add("Invalid order ID");
            }
            return this;
        }

        public bool IsValid()
        {
            return _errors.Count == 0;
        }

        public List<string> GetErrors()
        {
            return _errors;
        }

        public string GetErrorMessage()
        {
            return string.Join("; ", _errors);
        }

        public void Clear()
        {
            _errors.Clear();
        }
    }
}
