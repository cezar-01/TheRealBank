using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheRealBank.Entities;
using TheRealBank.Repositories.Chat;

namespace TheRealBank.Services.Chat
{
    public class ChatHistoryService : IChatHistoryService
    {
        private readonly IChatHistoryRepository _repo;

        public ChatHistoryService(IChatHistoryRepository repo) => _repo = repo;

        public async Task<List<ChatConversationDto>> GetConversationsAsync(string email)
        {
            var list = await _repo.GetByEmailAsync(email);
            return list.Select(Map).ToList();
        }

        public async Task<ChatConversationDto?> GetConversationAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity is null ? null : Map(entity);
        }

        public async Task<ChatConversationDto> SaveConversationAsync(string email, int? id, string title, string messagesJson)
        {
            if (id.HasValue && id.Value > 0)
            {
                var existing = await _repo.GetByIdAsync(id.Value);
                if (existing is not null && existing.UserEmail == email)
                {
                    existing.Title = title;
                    existing.MessagesJson = messagesJson;
                    await _repo.UpdateAsync(existing);
                    return Map(existing);
                }
            }

            var entity = new ChatConversation
            {
                UserEmail = email,
                Title = title,
                MessagesJson = messagesJson
            };
            await _repo.AddAsync(entity);
            return Map(entity);
        }

        public async Task DeleteConversationAsync(int id)
        {
            await _repo.DeleteAsync(id);
        }

        private static ChatConversationDto Map(ChatConversation e) => new()
        {
            Id = e.Id,
            UserEmail = e.UserEmail,
            Title = e.Title,
            MessagesJson = e.MessagesJson,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
