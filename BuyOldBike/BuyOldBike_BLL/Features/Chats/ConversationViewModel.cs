using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_BLL.Features.Chats
{
    public class ConversationViewModel
    {
        public Guid ListingId { get; set; }
        public Guid OtherUserId { get; set; }

        public string OtherUserEmail { get; set; } = "";
        public string ListingTitle { get; set; } = "";

        public string LastMessage { get; set; } = "";
        public DateTime LastTime { get; set; }
    }
}
