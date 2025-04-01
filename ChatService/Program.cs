
using ChatService.Application.Hubs;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Data;
using ChatService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChatService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowChatClient", builder =>
                {
                    builder.WithOrigins("http://127.0.0.1:5500") 
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials(); // Quan trọng cho SignalR
                });
            });

            // Đăng ký dependencies
            builder.Services.AddDbContext<ChatDbContext>(options =>
                options.UseNpgsql(builder.Configuration["ConnectionStrings:DefaultConnection"]));
            builder.Services.AddScoped<IMessageRepository, MessageRepository>();
            builder.Services.AddSignalR();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseCors("AllowChatClient");

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
