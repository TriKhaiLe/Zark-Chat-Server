
using ChatService.Application.Hubs;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Authentication;
using ChatService.Infrastructure.Data;
using ChatService.Infrastructure.Repositories;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile(builder.Configuration["GoogleCredential:ServiceAccountPath"]),
            });

            builder.Services.AddAuthentication()
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = builder.Configuration["Authentication:Authority"];
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["Authentication:ValidIssuer"],
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["Authentication:ValidAudience"],
                        ValidateLifetime = true
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) &&
                                path.StartsWithSegments("/chatHub"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddHttpClient<IJwtProvider, JwtProvider>((sp, client) =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(builder.Configuration["Authentication:TokenUri"]);
            });

            // Đăng ký dependencies
            builder.Services.AddDbContext<ChatDbContext>(options =>
                options.UseNpgsql(builder.Configuration["ConnectionStrings:DefaultConnection"]));
            builder.Services.AddScoped<IMessageRepository, MessageRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
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
            app.UseCors("AllowChatClient");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<ChatHub>("/chatHub");

            //app.Use(async (context, next) =>
            //{
            //    if (context.Request.Path.StartsWithSegments("/chatHub"))
            //    {
            //        var authHeader = context.Request.Headers["Authorization"];
            //        Console.WriteLine($"SignalR request with Authorization: {authHeader}");
            //    }
            //    await next();
            //});

            app.UseHttpsRedirection();

            app.MapControllers();

            //app.MapGet("/protected-endpoint", (HttpContext context) =>
            //{
            //    return Results.Ok(new { message = "Bạn đã xác thực thành công!", user = context.User.Identity.Name });
            //}).RequireAuthorization();

            app.Run();
        }
    }
}
