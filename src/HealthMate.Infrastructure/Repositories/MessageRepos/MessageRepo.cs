using HealthMate.Infrastructure.Data;
using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Application.Abstractions.Enums;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Repositories.MessageRepos
{
    public class MessageRepo : IMessageRepo
    {
        private readonly HealthMateContext _context;

        public MessageRepo(HealthMateContext context)
        {
            _context = context;
        }

        public async Task SendMessageAsync(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Message>> GetInboxAsync(string userId)
        {
            return await _context.Messages
                .Where(m => m.ReceiverId == userId)
                .Include(m => m.Sender)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetSentMessagesAsync(string userId)
        {
            return await _context.Messages
                .Where(m => m.SenderId == userId)
                .Include(m => m.Receiver)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<Message?> GetMessageByIdAsync(int messageId)
        {
            return await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m => m.Id == messageId);
        }

        public async Task<IEnumerable<MessageAttachment>> GetAttachmentsAsync(int messageId)
        {
            return await _context.MessageAttachments
                .Where(a => a.MessageId == messageId)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message != null && !message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteMessageAsync(int messageId, string currentUserId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null)
                return;

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Messages
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .CountAsync();
        }

        public async Task<IEnumerable<(ApplicationUser User, string? NationalId)>> GetAvailableReceiversAsync(string userId)
        {
            // Step 1: Get the user
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                Console.WriteLine($"User not found: {userId}");
                return Enumerable.Empty<(ApplicationUser, string?)>();
            }
            
            Console.WriteLine($"User found: {user.FullName}, Type: {user.UserType}");

            // Step 2: Get all encounters (for debugging)
            var allEncounters = await _context.Encounters.ToListAsync();
            Console.WriteLine($"Total encounters in database: {allEncounters.Count}");

            if (user.UserType == UserType.Patient)
            {
                var patientId = await _context.Patients
                    .Where(patient => patient.ApplicationUserId == userId)
                    .Select(patient => patient.Id)
                    .FirstOrDefaultAsync();

                var providerIds = await _context.Encounters
                    .Where(encounter => encounter.PatientId == patientId)
                    .Select(encounter => encounter.HealthCareProviderId)
                    .Distinct()
                    .ToArrayAsync();

                Console.WriteLine($"Found {providerIds.Length} encounters for patient");

                var providers = await _context.HealthCareProviders
                    .Include(provider => provider.ApplicationUser)
                    .Where(provider => providerIds.Contains(provider.HealthCareProvider_Id))
                    .ToListAsync();

                var result = providers
                    .Select(provider => (provider.ApplicationUser, (string?)null))
                    .Distinct()
                    .ToList();

                Console.WriteLine($"Returning {result.Count} unique healthcare providers");
                return result;
            }

            if (user.UserType == UserType.HealthCareProvider)
            {
                var providerId = await _context.HealthCareProviders
                    .Where(provider => provider.ApplicationUserId == userId)
                    .Select(provider => provider.HealthCareProvider_Id)
                    .FirstOrDefaultAsync();

                var patientIds = await _context.Encounters
                    .Where(encounter => encounter.HealthCareProviderId == providerId)
                    .Select(encounter => encounter.PatientId)
                    .Distinct()
                    .ToArrayAsync();

                Console.WriteLine($"Found {patientIds.Length} encounters for healthcare provider");

                var patients = await _context.Patients
                    .Where(patient => patientIds.Contains(patient.Id))
                    .ToListAsync();

                var patientUserIds = patients
                    .Select(patient => patient.ApplicationUserId)
                    .Where(static id => !string.IsNullOrWhiteSpace(id))
                    .Distinct()
                    .ToArray();
                var patientUsers = await _context.Users
                    .Where(u => patientUserIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id);

                var result = patients
                    .Where(patient => patient.ApplicationUserId is not null && patientUsers.ContainsKey(patient.ApplicationUserId))
                    .Select(patient => (patientUsers[patient.ApplicationUserId!], (string?)patient.NationalId.Value))
                    .Distinct()
                    .ToList();

                Console.WriteLine($"Returning {result.Count} unique patients");
                return result;
            }

            return Enumerable.Empty<(ApplicationUser, string?)>();
        }


    }
}
