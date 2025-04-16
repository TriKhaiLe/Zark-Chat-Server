
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
using Microsoft.OpenApi.Models;

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

            builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
            builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();

            builder.Services.AddSignalR();

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "ChatService API", Version = "v1" });

                // Add JWT Authentication
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IjcxMTE1MjM1YTZjNjE0NTRlZmRlZGM0NWE3N2U0MzUxMzY3ZWViZTAiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL3NlY3VyZXRva2VuLmdvb2dsZS5jb20vemFya2NoYXQtZWZhMjYiLCJhdWQiOiJ6YXJrY2hhdC1lZmEyNiIsImF1dGhfdGltZSI6MTc0NDAyMzQ3MywidXNlcl9pZCI6IlVOYXpiRkJ4ektjdnl4SWlZVjNIWno2VUtITTIiLCJzdWIiOiJVTmF6YkZCeHpLY3Z5eElpWVYzSFp6NlVLSE0yIiwiaWF0IjoxNzQ0MDIzNDczLCJleHAiOjE3NDQwMjcwNzMsImVtYWlsIjoienhjQGdtYWlsLmNvbSIsImVtYWlsX3ZlcmlmaWVkIjpmYWxzZSwiZmlyZWJhc2UiOnsiaWRlbnRpdGllcyI6eyJlbWFpbCI6WyJ6eGNAZ21haWwuY29tIl19LCJzaWduX2luX3Byb3ZpZGVyIjoicGFzc3dvcmQifX0.N417LmZQKDbLodFDOgmDbOW5_Jt2RAlF3ShwtKsdZyqvLGsGJKVYLB2-mxlQaj7-f3MlKzDWaYC3mZP3rk0JCeV0FkmCg7hNhDVtjDnXjzEw6ZAW09PnKcU1x78o13xSSMNJyfgHNjlbRiJVZ-janNmlnFuH6IntORULcUKSfZHL2hfDZynkBCJh4LIL7BLyg1zx_EmyiK7mc9h8LchSTxrT10me_GKjsGRUNrByo2p31w19tunadYTuMi4hNdP4iCeUq4Ee7vxsZtzuoo95zxGAhpQZRBOmtD1Q3FeJpIt90K-C77cfu6LbFMgpouaHig-ZnLrQBD2gg9EXk0TFCQ",
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });

                options.EnableAnnotations();
            });

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            });

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
