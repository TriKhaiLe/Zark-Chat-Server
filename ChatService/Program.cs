﻿using ChatService.Application.Hubs;
using ChatService.Core.Interfaces;
using ChatService.Infrastructure.Authentication;
using ChatService.Infrastructure.Data;
using ChatService.Infrastructure.Repositories;
using ChatService.Model;
using ChatService.Services.Email;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace ChatService
{
    public class Program
    {
        [Obsolete("Obsolete")]
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var config = builder.Configuration;
            

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowChatClient", builder =>
                {
                    builder.WithOrigins(
                            "http://127.0.0.1:5500",
                            "https://zark-chat-web-client.vercel.app")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials(); // Quan trọng cho SignalR
                });
            });

            var authority = GetConfigValue(config, "Authentication:Authority", "AUTH_AUTHORITY");
            var validIssuer = GetConfigValue(config, "Authentication:ValidIssuer", "AUTH_VALID_ISSUER");
            var validAudience = GetConfigValue(config, "Authentication:ValidAudience", "AUTH_VALID_AUDIENCE");
            var tokenUri = GetConfigValue(config, "Authentication:TokenUri", "AUTH_TOKEN_URI");
            var connectionString =
                GetConfigValue(config, "ConnectionStrings:DefaultConnection", "DB_CONNECTION_STRING");

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

            builder.Services.AddHangfire(config =>
            {
                config.SetDataCompatibilityLevel((CompatibilityLevel.Version_180))
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UsePostgreSqlStorage(connectionString);
            });
            builder.Services.AddHangfireServer();
            
            


            builder.Services.AddDbContext<ChatDbContext>(options =>
                options.UseNpgsql(connectionString));
            
            builder.Services.AddScoped<EventNotificationJob>();
            builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
            builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IEventRepository, EventRepository>();
            builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
            builder.Services.AddMemoryCache();

            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.AddTransient<IEmailService, EmailService>();

            // Add this to allow direct access to EmailSettings via IOptions<EmailSettings>
            builder.Services.AddOptions<EmailSettings>().Bind(builder.Configuration.GetSection("EmailSettings"));

            builder.Services.AddSignalR();

            builder.Services.AddControllers();
            builder.Services.AddControllers().AddJsonOptions(opt =>
            {
                opt.JsonSerializerOptions.ReferenceHandler =
                    System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });

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

            // Log SenderName to confirm EmailSettings is loaded from env vars
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                var emailSettings = scope.ServiceProvider.GetRequiredService<IOptions<EmailSettings>>().Value;
                logger.LogInformation("EmailSettings.SenderName: {SenderName}", emailSettings.SenderName);
                logger.LogInformation("Authority: {Authority}", authority);
                logger.LogInformation("ValidIssuer: {Issuer}", validIssuer);
                logger.LogInformation("ValidAudience: {Audience}", validAudience);
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None); // Collapse all controllers by default
            });

            app.UseRouting();
            app.UseCors("AllowChatClient");
            app.UseAuthentication();
            app.UseAuthorization();
            
            //using (var scope = app.Services.CreateScope())
            //{
            //    var jobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

            //    jobManager.AddOrUpdate<EventNotificationJob>(
            //        "send-event-notifications",
            //        job => job.SendEventNotificationsAsync(),
            //        Cron.MinuteInterval(1)
            //    );
            //}

            app.MapHub<ChatHub>("/chatHub");
            app.UseHangfireDashboard("/hangfire");
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
