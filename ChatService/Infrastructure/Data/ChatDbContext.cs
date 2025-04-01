using ChatService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ChatService.Infrastructure.Data
{
    public class ChatDbContext : DbContext
    {
        private readonly string _tableName;

        public ChatDbContext(DbContextOptions<ChatDbContext> options, string tableName = "ConversationShards")
            : base(options)
        {
            _tableName = tableName;
        }

        public DbSet<Message> Messages { get; set; }
        public DbSet<ConversationShard> ConversationShards { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Message>().ToTable(_tableName.StartsWith("Messages_") ? _tableName : "Messages");
            modelBuilder.Entity<ConversationShard>().ToTable("ConversationShards");
        }
    }
}
