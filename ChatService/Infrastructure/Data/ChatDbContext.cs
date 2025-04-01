using ChatService.Core.Entities;
using ChatService.Infrastructure.Data.Configurations;
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

        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new MessageConfiguration());
        }
    }
}
