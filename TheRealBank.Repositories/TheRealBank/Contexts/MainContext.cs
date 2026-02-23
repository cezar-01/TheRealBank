using Microsoft.EntityFrameworkCore;
using TheRealBank.Entities;
using TheRealBank.Contexts.Base;

namespace TheRealBank.Contexts
{
    public class MainContext : DbContextBase
    {
        public MainContext(DbContextOptions options) : base(options) { }

        public DbSet<Customer> Customers { get; set; } = default!;
        public DbSet<ChatConversation> ChatConversations { get; set; } = default!;
    }
}