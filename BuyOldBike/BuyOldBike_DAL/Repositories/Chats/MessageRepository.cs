using BuyOldBike_DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_DAL.Repositories.Chats
{
    public class MessageRepository
    {
        private readonly BuyOldBikeContext _db;

        public MessageRepository()
        {
            _db = new BuyOldBikeContext();
        }

        public List<Message> GetMessages(Guid listingId, Guid userId1, Guid userId2)
        {
            return _db.Messages
                      .Include(m => m.Sender)
                      .Where(m =>
                          m.ListingId == listingId &&
                          ((m.SenderId == userId1 && m.ReceiverId == userId2) ||
                           (m.SenderId == userId2 && m.ReceiverId == userId1)))
                      .OrderBy(m => m.SentAt)
                      .ToList();
        }
        public bool SendMessage(Message message)
        {
            _db.Messages.Add(message);
            return _db.SaveChanges() > 0;
        }

        public void MarkAsRead(Guid listingId, Guid receiverId)
        {
            var unread = _db.Messages
                            .Where(m => m.ListingId == listingId &&
                                        m.ReceiverId == receiverId &&
                                        !m.IsRead)
                            .ToList();
            unread.ForEach(m => m.IsRead = true);
            _db.SaveChanges();
        }
        // Lấy danh sách các cuộc hội thoại của seller
        public List<Message> GetLatestMessagePerConversation(Guid sellerId)
        {
            return _db.Messages
                      .Include(m => m.Sender)
                      .Include(m => m.Listing)
                      .Where(m => m.ReceiverId == sellerId || m.SenderId == sellerId)
                      .GroupBy(m => new {
                          m.ListingId,
                          BuyerId = m.SenderId == sellerId ? m.ReceiverId : m.SenderId
                      })
                      .Select(g => g.OrderByDescending(m => m.SentAt).First())
                      .ToList();
        }

        public int CountUnread(Guid sellerId)
        {
            return _db.Messages
                      .Count(m => m.ReceiverId == sellerId && !m.IsRead);
        }
    }
}
