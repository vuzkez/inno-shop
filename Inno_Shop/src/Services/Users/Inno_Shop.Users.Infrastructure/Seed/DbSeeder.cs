using Inno_Shop.Users.Application.Options;
using Inno_Shop.Users.Domain;
using Inno_Shop.Users.Infrastructure.Identity;
using Inno_Shop.Users.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inno_Shop.Users.Infrastructure.Seed
{
    public static class DbSeeder
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;

            var db = sp.GetRequiredService<UsersDbContext>();
            if (db.Database.IsRelational())
                await db.Database.MigrateAsync();
            else
                await db.Database.EnsureCreatedAsync();

            await SeedRolesAsync(sp);
            await SeedAdminAsync(sp);
        }

        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            foreach (var roleName in Enum.GetNames<Role>())
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            }
        }

        public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<AdminSeedOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
                return;

            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbSeeder));

            if (await userManager.FindByEmailAsync(options.Email) != null)
                return;

            var admin = new ApplicationUser
            {
                UserName = options.Email,
                Email = options.Email,
                Name = options.Name,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(admin, options.Password);
            if (!result.Succeeded)
            {
                logger.LogError("Admin seed failed: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(admin, nameof(Role.Admin));
            logger.LogInformation("Admin user {Email} created.", options.Email);
        }
    }
}
