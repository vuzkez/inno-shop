using System;
using System.Collections.Generic;
using System.Text;
using Inno_Shop.Users.Application.Interfaces;
using Inno_Shop.Users.Application.Options;
using Inno_Shop.Users.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Inno_Shop.Users.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();

            return services;
        }
    }
}
