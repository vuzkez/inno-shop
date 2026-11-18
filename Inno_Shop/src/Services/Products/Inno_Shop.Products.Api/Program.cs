using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Inno_Shop.Products.Api.Common;
using Inno_Shop.Products.Application;
using Inno_Shop.Products.Application.Options;
using Inno_Shop.Products.Infrastructure;
using Inno_Shop.Products.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Inno_Shop.Products.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddApplication();

            builder.Services.AddControllers();
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddHealthChecks();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "InnoShop Products API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Вставьте accessToken, полученный в Users API (/api/auth/login)"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
            {
                if (allowedOrigins.Length > 0)
                    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
            }));

            builder.Services.AddOptions<JwtOptions>()
                .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
                .Validate(x => !string.IsNullOrWhiteSpace(x.Secret) && x.Secret.Length >= 32,
                    "JWT Error: Secret key is required and MUST be at least 32 characters long (256 bits).")
                .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer),
                    "JWT Error: Issuer (iss) cannot be empty.")
                .Validate(x => !string.IsNullOrWhiteSpace(x.Audience),
                    "JWT Error: Audience (aud) cannot be empty.")
                .ValidateOnStart();

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

            builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtOptions>>((opts, jwtOptionsAccessor) =>
                {
                    var jwtOptions = jwtOptionsAccessor.Value;

                    opts.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        NameClaimType = JwtRegisteredClaimNames.Name,
                        RoleClaimType = "role",
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
                    };
                    opts.MapInboundClaims = false;
                });

            var app = builder.Build();

            await ApplyMigrationsAsync(app);

            app.UseExceptionHandler();
            app.UseStatusCodePages();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHealthChecks("/health");
            app.MapControllers();

            await app.RunAsync();
        }

        private static async Task ApplyMigrationsAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();
            await database.Database.MigrateAsync();
        }
    }
}
