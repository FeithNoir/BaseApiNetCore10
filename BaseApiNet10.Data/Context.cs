using BaseApiNet10.Core.Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BaseApiNet10.Data
{
    public class Context : IdentityDbContext<User, IdentityRole, string>
    {
        public Context(DbContextOptions options) : base(options) { }

        // Override
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite();
        }
    }
}
