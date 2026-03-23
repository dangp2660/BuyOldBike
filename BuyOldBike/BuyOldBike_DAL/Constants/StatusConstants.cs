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
            public const string Reserved = "Reserved";
            public const string Deposit_Pending = "Deposit_Pending";
            public const string Rejected = "Rejected";
            public const string Hidden = "Hidden";
            public const string Deleted = "Deleted";
        }

        public static class OrdersStatus
        {
            public const string Deposit_Pending = "Deposit_Pending";
            public const string Deposit_Paid = "Deposit_Paid";
            public const string Deposit_Failed = "Deposit_Failed";
            public const string Deposit_Expired = "Deposit_Expired";
        }

        public static class PaymentType
        {
            public const string VN_Pay = "VN_Pay";
            public const string Internal_Wallet = "Internal_Wallet";
        }

        public static class PaymentStatus
        {
            public const string Pending = "Pending";
            public const string Success = "Success";
            public const string Failed = "Failed";
            public const string Expired = "Expired";
            public const string Completed = "Completed";
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
