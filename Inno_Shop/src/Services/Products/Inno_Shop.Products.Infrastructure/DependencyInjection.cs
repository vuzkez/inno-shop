using Inno_Shop.Products.Application.Interfaces;
using Inno_Shop.Products.Application.Options;
using Inno_Shop.Products.Infrastructure.Consumers;
using Inno_Shop.Products.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Inno_Shop.Products.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<DatabaseOptions>()
                .Bind(configuration.GetSection(DatabaseOptions.SectionName))
                .Validate(x => !string.IsNullOrWhiteSpace(x.DefaultConnection),
                    "Database Error: Connection string is missing.")
                .ValidateOnStart();

            services.AddOptions<RabbitMqOptions>()
                .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
                .Validate(x => !string.IsNullOrWhiteSpace(x.Host),
                    "RabbitMQ Error: Host is required.")
                .Validate(x => x.Port > 0 && x.Port <= 65535,
                    "RabbitMQ Error: Port must be between 1 and 65535.")
                .Validate(x => !string.IsNullOrWhiteSpace(x.VirtualHost),
                    "RabbitMQ Error: VirtualHost is required.")
                .Validate(x => !string.IsNullOrWhiteSpace(x.UserName),
                    "RabbitMQ Error: UserName is required.")
                .Validate(x => !string.IsNullOrWhiteSpace(x.Password),
                    "RabbitMQ Error: Password is required.")
                .ValidateOnStart();

            services.AddDbContext<ProductsDbContext>((provider, options) =>
            {
                var database = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
                options.UseSqlServer(database.DefaultConnection);
            });

            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ProductsDbContext>());

            services.AddMassTransit(bus =>
            {
                bus.AddConsumer<UserDeactivatedConsumer>();
                bus.AddConsumer<UserActivatedConsumer>();
                bus.AddConsumer<UserDeletedConsumer>();

                bus.UsingRabbitMq((context, cfg) =>
                {
                    var rabbit = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                    var virtualHost = rabbit.VirtualHost.TrimStart('/');
                    var hostUri = new Uri($"rabbitmq://{rabbit.Host}:{rabbit.Port}/{virtualHost}");

                    cfg.Host(hostUri, host =>
                    {
                        host.Username(rabbit.UserName);
                        host.Password(rabbit.Password);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}
