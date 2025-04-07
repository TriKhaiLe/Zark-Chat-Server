
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
            var config = builder.Configuration;

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

            var authority = GetConfigValue(config, "Authentication:Authority", "AUTH_AUTHORITY");
            var validIssuer = GetConfigValue(config, "Authentication:ValidIssuer", "AUTH_VALID_ISSUER");
            var validAudience = GetConfigValue(config, "Authentication:ValidAudience", "AUTH_VALID_AUDIENCE");
            var tokenUri = GetConfigValue(config, "Authentication:TokenUri", "AUTH_TOKEN_URI");
            var connectionString = GetConfigValue(config, "ConnectionStrings:DefaultConnection", "DB_CONNECTION_STRING");

            if (builder.Environment.IsDevelopment())
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(config["GoogleCredential:ServiceAccountPath"])
                });
            }
            else
            {
                var firebaseJson = Environment.GetEnvironmentVariable("FIREBASE_CONFIG_JSON");
                var credential = GoogleCredential.FromJson(firebaseJson);
                FirebaseApp.Create(new AppOptions
                {
                    Credential = credential
                });
            }

            builder.Services.AddAuthentication()
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = authority;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = validIssuer,
                        ValidateAudience = true,
                        ValidAudience = validAudience,
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
                client.BaseAddress = new Uri(tokenUri);
            });

            builder.Services.AddDbContext<ChatDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddScoped<IMessageRepository, MessageRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
            builder.Services.AddSignalR();

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseRouting();
            app.UseCors("AllowChatClient");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<ChatHub>("/chatHub");

            app.UseHttpsRedirection();

            app.MapControllers();

            app.Run();
        }

        static string? GetConfigValue(ConfigurationManager config, string key, string? fallbackEnvVar = null)
        {
            var configValue = config.GetValue<string>(key);
            return string.IsNullOrEmpty(configValue) ? Environment.GetEnvironmentVariable(fallbackEnvVar) : configValue;
        }

    }
}
