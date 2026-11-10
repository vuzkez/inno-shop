using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Inno_Shop.Users.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Inno_Shop.Users.Infrastructure.Persistence
{
    public class UsersDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public UsersDbContext(DbContextOptions<UsersDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
