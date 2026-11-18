using System.Reflection;
using FluentValidation;
using Inno_Shop.Products.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Inno_Shop.Products.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(configuration =>
            {
                configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            services.AddSingleton(TimeProvider.System);

            return services;
        }
    }
}
