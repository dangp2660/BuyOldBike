using BuyOldBike_BLL.Features.Chats;
using BuyOldBike_DAL.Entities;
using BuyOldBike_DAL.Repositories.Chats;
using BuyOldBike_Presentation.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_Presentation.ViewModels
{
    public class ChatService
    {
        private readonly MessageRepository _repo;

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
        public List<ConversationViewModel> GetConversationsForBuyer(Guid buyerId)
        {
            var messages = _repo.GetAllMessagesForUser(buyerId);

            return messages
                .GroupBy(x => new
                {
                    x.ListingId,
                    OtherUserId = x.SenderId == buyerId ? x.ReceiverId : x.SenderId
                })
                .Select(g =>
                {
                    var last = g.OrderByDescending(x => x.SentAt).First();

                    return new ConversationViewModel
                    {
                        ListingId = g.Key.ListingId,
                        OtherUserId = g.Key.OtherUserId,

                        OtherUserEmail = last.SenderId == buyerId
                            ? last.Receiver?.Email??""
                            : last.Sender?.Email??"",

                        ListingTitle = last.Listing?.Title??"",
                        LastMessage = last.Content,
                        LastTime = last.SentAt
                    };
                })
                .OrderByDescending(x => x.LastTime)
                .ToList();
        }
    }
}
