using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_DAL.Entities
{
    public partial class Message
    {
        public Guid MessageId { get; set; }
        public Guid ListingId { get; set; }
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }

        public virtual User? Sender { get; set; }
        public virtual User? Receiver { get; set; }
        public virtual Listing? Listing { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsMine { get; set; }
    }
}
