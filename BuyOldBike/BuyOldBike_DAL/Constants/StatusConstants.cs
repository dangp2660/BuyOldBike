using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_DAL.Constants
{
    public class StatusConstants
    {
        public static class ListingStatus
        {
            public const string Pending_Inspection = "Pending_Inspection";
            public const string Available = "Available";
            public const string Rejected = "Rejected";
        }

        public static class InspectionStatus
        {
            public const string Pending = "Pending";
            public const string Completed = "Completed";

        }

        public static class InspectionResult
        {
            public const string Passed = "Passed";
            public const string Failed = "Failed";
        }
    }
}
