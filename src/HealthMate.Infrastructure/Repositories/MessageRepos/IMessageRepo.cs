using HealthMate.Infrastructure.Data.Models;

namespace HealthMate.Infrastructure.Repositories.MessageRepos{
    public interface IMessageRepo{
        Task SendMessageAsync(Message message);
        Task<IEnumerable<Message>> GetInboxAsync(string userId);
        Task<IEnumerable<Message>> GetSentMessagesAsync(string userId);
        Task<Message?> GetMessageByIdAsync(int messageId);
        Task<IEnumerable<MessageAttachment>> GetAttachmentsAsync(int messageId);
        Task MarkAsReadAsync(int messageId);
        Task DeleteMessageAsync(int messageId, string currentUserId);
        Task<int> GetUnreadCountAsync(string userId);
        Task<IEnumerable<(ApplicationUser User, string? NationalId)>> GetAvailableReceiversAsync(string userId);

    }
}