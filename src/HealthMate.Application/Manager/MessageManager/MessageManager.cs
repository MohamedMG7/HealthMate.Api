using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.DTO.MessageDto;
using HealthMate.Infrastructure.Repositories.MessageRepos;

namespace HealthMate.Application.Manager.MessageManager{
    public class MessageManager : IMessageManager{
        private readonly IMessageRepo _messageRepo;

        public MessageManager(IMessageRepo messageRepo)
        {
            _messageRepo = messageRepo;
        }

        public async Task SendMessageAsync(SendMessageDto dto, string senderId)
        {
            // Basic validation
            if (senderId == dto.ReceiverId)
                throw new InvalidOperationException("Sender and receiver cannot be the same.");

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = dto.ReceiverId,
                Subject = dto.Subject,
                Body = dto.Body,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                Attachments = dto.Attachments?.Select(a => new MessageAttachment
                {
                    AttatchmentType = a.AttatchmentType,
                    AttatchmentId = a.AttatchmentId
                }).ToList() ?? new List<MessageAttachment>()
            };

            await _messageRepo.SendMessageAsync(message);
        }

        public async Task<IEnumerable<InboxMessageDto>> GetInboxAsync(string userId)
        {
            var messages = await _messageRepo.GetInboxAsync(userId);
            return messages.Select(m => new InboxMessageDto
            {
                Id = m.Id,
                Subject = m.Subject,
                SenderName = m.Sender.FullName,
                CreatedAt = m.CreatedAt,
                IsRead = m.IsRead
            });
        }

        public async Task<IEnumerable<SentMessageDto>> GetSentMessagesAsync(string userId)
        {
            var messages = await _messageRepo.GetSentMessagesAsync(userId);
            return messages.Select(m => new SentMessageDto
            {
                Id = m.Id,
                Subject = m.Subject,
                ReceiverName = m.Receiver.FullName,
                CreatedAt = m.CreatedAt
            });
        }

        public async Task<MessageDetailsDto?> GetMessageByIdAsync(int messageId, string userId)
        {
            var message = await _messageRepo.GetMessageByIdAsync(messageId);
            if (message == null || (message.ReceiverId != userId && message.SenderId != userId))
                return null;

            return new MessageDetailsDto
            {
                Id = message.Id,
                Subject = message.Subject,
                Body = message.Body,
                SenderName = message.Sender.FullName,
                ReceiverName = message.Receiver.FullName,
                CreatedAt = message.CreatedAt,
                IsRead = message.IsRead,
                Attachments = message.Attachments.Select(a => new AttachmentDto
                {
                    AttatchmentId = a.AttatchmentId,
                    AttatchmentType = a.AttatchmentType
                }).ToList()
            };
        }

        public async Task MarkAsReadAsync(int messageId, string userId)
        {
            var message = await _messageRepo.GetMessageByIdAsync(messageId);
            if (message != null && message.ReceiverId == userId)
                await _messageRepo.MarkAsReadAsync(messageId);
        }

        public async Task DeleteMessageAsync(int messageId, string userId)
        {
            await _messageRepo.DeleteMessageAsync(messageId, userId);
        }
        
        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _messageRepo.GetUnreadCountAsync(userId);
        }

        public async Task<IEnumerable<AvailableReceiverDto>> GetAvailableReceiversAsync(string userId)
        {
            var usersWithIds = await _messageRepo.GetAvailableReceiversAsync(userId);
            return usersWithIds.Select(pair => new AvailableReceiverDto
            {
                Id = pair.User.Id,
                Name = pair.User.FullName,
                NationlId = pair.NationalId ?? "N/A"
            });
        }

    }
}