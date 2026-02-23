using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TheRealBank.Contexts;
using TheRealBank.Entities;

namespace TheRealBank.Repositories.Chat
{
    public sealed class ChatHistoryRepository : IChatHistoryRepository
    {
        private readonly MainContext _db;

        public ChatHistoryRepository(MainContext db) => _db = db;

        public async Task<List<ChatConversation>> GetByEmailAsync(string email, CancellationToken ct = default)
            => await _db.ChatConversations
                .AsNoTracking()
                .Where(c => c.UserEmail == email)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync(ct);

        public async Task<ChatConversation?> GetByIdAsync(int id, CancellationToken ct = default)
            => await _db.ChatConversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, ct);

        public async Task<ChatConversation> AddAsync(ChatConversation conversation, CancellationToken ct = default)
        {
            conversation.CreatedAt = DateTime.UtcNow;
            conversation.UpdatedAt = DateTime.UtcNow;
            _db.ChatConversations.Add(conversation);
            await _db.SaveChangesAsync(ct);
            return conversation;
        }

        public async Task UpdateAsync(ChatConversation conversation, CancellationToken ct = default)
        {
            conversation.UpdatedAt = DateTime.UtcNow;
            _db.ChatConversations.Update(conversation);
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.ChatConversations.FindAsync(new object?[] { id }, ct);
            if (entity is null) return;
            _db.ChatConversations.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
