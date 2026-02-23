using System.Collections.Generic;
using System.Threading.Tasks;

namespace TheRealBank.Services.Chat
{
    public interface IChatHistoryService
    {
        Task<List<ChatConversationDto>> GetConversationsAsync(string email);
        Task<ChatConversationDto?> GetConversationAsync(int id);
        Task<ChatConversationDto> SaveConversationAsync(string email, int? id, string title, string messagesJson);
        Task DeleteConversationAsync(int id);
    }

    public class ChatConversationDto
    {
        public int Id { get; set; }
        public string UserEmail { get; set; } = "";
        public string Title { get; set; } = "";
        public string MessagesJson { get; set; } = "[]";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
