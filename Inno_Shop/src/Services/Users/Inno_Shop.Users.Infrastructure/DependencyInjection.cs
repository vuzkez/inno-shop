using System;
using System.Collections.Generic;
using System.Text;
using Inno_Shop.Users.Application.Interfaces;
using Inno_Shop.Users.Application.Options;
using Inno_Shop.Users.Infrastructure.Identity;
using Inno_Shop.Users.Infrastructure.Persistence;
using Inno_Shop.Users.Infrastructure.Repositories;
using Inno_Shop.Users.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Inno_Shop.Users.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddOptions<DatabaseOptions>()
                .Bind(configuration.GetSection(DatabaseOptions.SectionName))
                .Validate(x => !string.IsNullOrWhiteSpace(x.DefaultConnection), "Database Error: Connection string is missing.")
                .ValidateOnStart();

            services.AddOptions<RedisOptions>()
               .Bind(configuration.GetSection(RedisOptions.SectionName))
               .Validate(x => !string.IsNullOrWhiteSpace(x.ConnectionString), "Redis Error: Connection string is missing.")
               .ValidateOnStart();

            services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection(JwtOptions.SectionName))
                .Validate(x => !string.IsNullOrWhiteSpace(x.Secret) && x.Secret.Length >= 32,
                    "JWT Error: Secret key is required and MUST be at least 32 characters long (256 bits).")
                .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer),
                    "JWT Error: Issuer (iss) cannot be empty.")
                .Validate(x => !string.IsNullOrWhiteSpace(x.Audience),
                    "JWT Error: Audience (aud) cannot be empty.")
                .Validate(x => x.ExpiryMinutes > 0 && x.ExpiryMinutes <= 1440,
                    "JWT Error: ExpiryMinutes must be greater than 0 and less than or equal to 1440 (24 hours).")
                .Validate(x => x.RefreshTokenExpiryDays > 0, "JWT Error: RefreshTokenExpiryDays must be greater than 0.")
                .ValidateOnStart();

            services.AddOptions<EmailOptions>()
                .Bind(configuration.GetSection(EmailOptions.SectionName))
                .Validate(x => !string.IsNullOrWhiteSpace(x.Host), "Email Error: Host is required.")
                .Validate(x => !string.IsNullOrWhiteSpace(x.FromEmail), "Email Error: FromEmail is required.")
                .Validate(x => !string.IsNullOrWhiteSpace(x.UserName), "Email Error: UserName is required.")
                .Validate(x => !string.IsNullOrWhiteSpace(x.Password), "Email Error: Password is required.")
                .Validate(x => !string.IsNullOrWhiteSpace(x.FromName), "Email Error: FromName is required.")
                .ValidateOnStart();

            var dbOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>();

            services.AddDbContext<UsersDbContext>(opts =>
            {
                opts.UseSqlServer(dbOptions!.DefaultConnection);
            });

            services.AddIdentityCore<ApplicationUser>(opts =>
            {
                opts.Password.RequireDigit = true;
                opts.Password.RequiredLength = 6;
                opts.User.RequireUniqueEmail = true;
                opts.Lockout.MaxFailedAccessAttempts = 5;
                opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<UsersDbContext>()
            .AddDefaultTokenProviders();

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
                return ConnectionMultiplexer.Connect(options.ConnectionString);
            });

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IJwtProvider, JwtProvider>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITokenService, TokenService>();

            return services;
        }
    }
}
