using ChatService.Core.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id).ValueGeneratedOnAdd();
            builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(50);
            builder.HasMany(u => u.Connections)
                .WithOne(uc => uc.User)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa các kết nối khi người dùng bị xóa
            builder.HasMany(u => u.Devices)
                .WithOne()
                .HasForeignKey(ud => ud.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa các thiết bị khi người dùng bị xóa

        }
    }
}
