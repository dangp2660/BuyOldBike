using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Chats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_BLL.Features.Chats
{
    public class ChatService
    {
        private readonly MessageRepository _repo;

        public ChatService() : this(new MessageRepository())
        {
        }

        public ChatService(MessageRepository repo)
        {
            _repo = repo;
        }

        public List<Message> GetMessages(Guid listingId, Guid userId1, Guid userId2)
        {
            return _repo.GetMessages(listingId, userId1, userId2);
        }

        public (bool Success, string Error) SendMessage(
            Guid listingId, Guid senderId, Guid receiverId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return (false, "Tin nhắn không được để trống.");

            if (content.Length > 1000)
                return (false, "Tin nhắn không được vượt quá 1000 ký tự.");

            var message = new Message
            {
                MessageId = Guid.NewGuid(),
                ListingId = listingId,
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content.Trim(),
                SentAt = DateTime.Now,
                IsRead = false
            };

            var ok = _repo.SendMessage(message);
            return ok ? (true, string.Empty) : (false, "Gửi thất bại.");
        }

        public void MarkAsRead(Guid listingId, Guid receiverId)
        {
            _repo.MarkAsRead(listingId, receiverId);
        }
        public List<Message> GetConversations(Guid sellerId)
        {
            return _repo.GetLatestMessagePerConversation(sellerId);
        }

        public int GetUnreadCount(Guid sellerId)
        {
            return _repo.CountUnread(sellerId);
        }
    }
}
