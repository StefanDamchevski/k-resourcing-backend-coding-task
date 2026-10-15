using Claims.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure.Persistence
{
    public class AuditContext(DbContextOptions<AuditContext> options) : DbContext(options)
    {
        public DbSet<ClaimAudit> ClaimAudits { get; set; }

        public DbSet<CoverAudit> CoverAudits { get; set; }
    }
}
