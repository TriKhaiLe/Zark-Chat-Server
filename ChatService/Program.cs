
using ChatService.Application.Hubs;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Data;
using ChatService.Infrastructure.Repositories;
using ChatService.Infrastructure.Sharding;
using Microsoft.EntityFrameworkCore;

namespace ChatService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Đăng ký dependencies
            builder.Services.AddDbContext<ChatDbContext>(options =>
                options.UseNpgsql(builder.Configuration["MetaConnectionString"])); // Connection cho metadata
            builder.Services.AddScoped<ShardManager>();
            builder.Services.AddScoped<IShardDistributor, ShardDistributor>();
            builder.Services.AddScoped<IMessageRepository, MessageRepository>();
            builder.Services.AddScoped<ChatService.Application.Services.ChatService>();
            builder.Services.AddSignalR();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();

            app.MapHub<ChatHub>("/chatHub");

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
