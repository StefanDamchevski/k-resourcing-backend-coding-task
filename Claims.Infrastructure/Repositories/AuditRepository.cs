using Claims.Application.Interfaces;
using Claims.Domain.Entities;
using Claims.Infrastructure.Persistence;

namespace Claims.Infrastructure.Repositories
{
    public class AuditRepository(AuditContext auditContext) : IAuditRepository
    {
        public async Task AuditClaimAsync(string claimId, string action, CancellationToken cancellationToken)
        {
            var claimAudit = new ClaimAudit()
            {
                Created = DateTime.Now,
                HttpRequestType = action,
                ClaimId = claimId
            };

            await auditContext.AddAsync(claimAudit, cancellationToken);

            await auditContext.SaveChangesAsync(cancellationToken);
        }

        public async Task AuditCoverAsync(string coverId, string action, CancellationToken cancellationToken)
        {
            var coverAudit = new CoverAudit()
            {
                Created = DateTime.Now,
                HttpRequestType = action,
                CoverId = coverId
            };

            await auditContext.AddAsync(coverAudit, cancellationToken);

            await auditContext.SaveChangesAsync(cancellationToken);
        }
    }
}
