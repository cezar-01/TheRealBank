using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheRealBank.Entities;

namespace TheRealBank.Repositories.Chat
{
    public interface IChatHistoryRepository
    {
        Task<List<ChatConversation>> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<ChatConversation?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ChatConversation> AddAsync(ChatConversation conversation, CancellationToken ct = default);
        Task UpdateAsync(ChatConversation conversation, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }
}
