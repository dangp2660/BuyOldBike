using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_BLL.Services.Kyc
{
    public class KycExtractResult
    {
        public string IdNumber { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string DateOfBirth { get; set; } = null!;
        public string? Gender { get; set; }
        public string? Nationality { get; set; } 
        public string? PlaceOfOrigin { get; set; }
        public string? PlaceOfResidence { get; set; }
        public string? ExpiryDate { get; set; }
    }
}
