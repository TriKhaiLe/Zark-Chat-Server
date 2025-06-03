using ChatService.Core.Entities;
using ChatService.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Generic;
using System.Reflection.Emit;
using ChatService.Controllers.RequestModels;

namespace ChatService.Infrastructure.Data
{
    public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<MessageReadStatus> MessageReadStatuses { get; set; }
        public DbSet<EncryptedSessionKeyInfo> EncryptedSessionKeys { get; set; }
        public DbSet<UserConnection> UserConnections { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<UserDevice> UserDevices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new ConversationConfiguration());
            modelBuilder.ApplyConfiguration(new ConversationParticipantConfiguration());
            modelBuilder.ApplyConfiguration(new ChatMessageConfiguration());
            modelBuilder.ApplyConfiguration(new MessageReadStatusConfiguration());
            modelBuilder.ApplyConfiguration(new EventConfigucation());
            modelBuilder.ApplyConfiguration(new ParticipantConfiguration());
        }
    }

    internal class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
    {
        public void Configure(EntityTypeBuilder<Participant> builder)
        {
            builder.HasKey(e => new { e.EventId, e.UserId });

            builder.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Pending"); // "Pending", "Accepted", "Rejected"

            builder.HasOne(e => e.Event)
                .WithMany(e => e.Participants)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(e => e.EventId);
            builder.HasIndex(e => e.UserId);
        }
    }

    internal class EventConfigucation : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id).ValueGeneratedOnAdd();

            builder.Property(e => e.CreatorId).IsRequired();

            builder.HasOne<User>(u => u.Creator).WithMany().HasForeignKey(e => e.CreatorId);

            builder.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.Description)
                .HasMaxLength(400);

            builder.Property(e => e.StartTime).IsRequired();

            builder.Property(e => e.EndTime).IsRequired();

            builder.Property(e => e.Status).HasDefaultValue(false);

            builder.HasMany(e => e.Participants).WithOne(p => p.Event).HasForeignKey(p => p.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(e => e.CreatorId);
        }
    }

    internal class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            builder.HasKey(e => e.ConversationId);

                     builder.Property(e => e.Type)
                            .IsRequired()
                            .HasMaxLength(20); // "Private" hoặc "Group"

                     builder.Property(e => e.Name)
                            .HasMaxLength(100); 

                     // Index để tối ưu tìm kiếm theo thời gian
                     builder.HasIndex(e => e.LastMessageAt);

                     builder.OwnsMany(e => e.EncryptedSessionKeys, b =>
                     {
                            b.WithOwner().HasForeignKey("ConversationId");
                            b.Property<int>("Id");
                            b.HasKey("Id");
                            b.Property(x => x.UserId).IsRequired();
                            b.Property(x => x.EncryptedSessionKey).HasColumnType("text");
                     });
              }
       }

    internal class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
    {
        public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
        {
            builder.HasKey(e => new { e.ConversationId, e.UserId });

                     builder.HasOne(e => e.Conversation)
                            .WithMany(c => c.Participants)
                            .HasForeignKey(e => e.ConversationId)
                            .OnDelete(DeleteBehavior.Cascade);

                     builder.HasOne(e => e.User)
                            .WithMany() // Không cần navigation property ngược lại
                            .HasForeignKey(e => e.UserId)
                            .OnDelete(DeleteBehavior.Restrict);

                     builder.Property(e => e.Role)
                            .IsRequired()
                            .HasMaxLength(20); // "Member" hoặc "Admin"

                     builder.Property(e => e.JoinedAt)
                            .IsRequired();
              }
       }

    internal class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.HasKey(e => e.ChatMessageId);

                     builder.HasOne(e => e.Conversation)
                            .WithMany(c => c.Messages)
                            .HasForeignKey(e => e.ConversationId)
                            .OnDelete(DeleteBehavior.Cascade);

                     // Mối quan hệ với User (người gửi)
                     builder.HasOne(e => e.Sender)
                            .WithMany()
                            .HasForeignKey(e => e.UserSendId)
                            .OnDelete(DeleteBehavior.Restrict);

                     builder.Property(e => e.SendDate)
                            .IsRequired();

                     builder.Property(e => e.Type)
                            .IsRequired()
                            .HasMaxLength(20); // "Text", "Image", "Video", v.v.

                     builder.Property(e => e.Message)
                            .HasMaxLength(2000);

                     builder.Property(e => e.MediaLink)
                            .HasMaxLength(500);

                     // Index để tối ưu tìm kiếm tin nhắn
                     builder.HasIndex(e => e.SendDate);
              }
       }

    internal class MessageReadStatusConfiguration : IEntityTypeConfiguration<MessageReadStatus>
    {
        public void Configure(EntityTypeBuilder<MessageReadStatus> builder)
        {
            builder.HasKey(e => new { e.ChatMessageId, e.UserId });

                     // Mối quan hệ với ChatMessage
                     builder.HasOne(e => e.Message)
                            .WithMany(m => m.ReadStatuses)
                            .HasForeignKey(e => e.ChatMessageId)
                            .OnDelete(DeleteBehavior.Cascade);

                     // Mối quan hệ với User
                     builder.HasOne(e => e.User)
                            .WithMany()
                            .HasForeignKey(e => e.UserId)
                            .OnDelete(DeleteBehavior.Restrict);
              }
       }

}
