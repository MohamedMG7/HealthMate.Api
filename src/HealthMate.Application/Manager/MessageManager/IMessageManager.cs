using HealthMate.Application.Messaging.Contracts;

namespace HealthMate.Application.Manager.MessageManager{
    public interface IMessageManager{
        Task SendMessageAsync(SendMessageDto dto, string senderId);
        Task<IEnumerable<InboxMessageDto>> GetInboxAsync(string userId);
        Task<IEnumerable<SentMessageDto>> GetSentMessagesAsync(string userId);
        Task<MessageDetailsDto?> GetMessageByIdAsync(int messageId, string userId);
        Task MarkAsReadAsync(int messageId, string userId);
        Task DeleteMessageAsync(int messageId, string userId);
        Task<int> GetUnreadCountAsync(string userId);
        Task<IEnumerable<AvailableReceiverDto>> GetAvailableReceiversAsync(string userId);

        
    }
}
