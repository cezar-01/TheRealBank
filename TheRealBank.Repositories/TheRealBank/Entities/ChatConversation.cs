using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TheRealBank.Entities
{
    public class ChatConversation
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string UserEmail { get; set; } = default!;

        [Required, MaxLength(200)]
        public string Title { get; set; } = "Nova conversa";

        [Required]
        public string MessagesJson { get; set; } = "[]";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
