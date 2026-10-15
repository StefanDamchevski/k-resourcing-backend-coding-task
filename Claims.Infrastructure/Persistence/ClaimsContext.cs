using Claims.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Claims.Infrastructure.Persistence
{
    public class ClaimsContext(DbContextOptions<ClaimsContext> options) : DbContext(options)
    {
        public DbSet<Claim> Claims { get; init; }

        public DbSet<Cover> Covers { get; init; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Claim>().ToCollection("claims");

            modelBuilder.Entity<Cover>().ToCollection("covers");
        }
    }
}
