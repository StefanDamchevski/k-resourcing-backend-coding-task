using Claims.Application.Interfaces;
using Claims.Domain.Entities;
using Claims.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure.Repositories
{
    public class ClaimRepository(ClaimsContext claimsContext) : IClaimRepository
    {
        public async Task AddAsync(Claim claim, CancellationToken cancellationToken)
        {
            await claimsContext.Claims.AddAsync(claim, cancellationToken);

            await claimsContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
        {
            var claim = await GetByIdAsync(id, cancellationToken);

            if (claim is null)
                return false;

            claimsContext.Claims.Remove(claim);

            await claimsContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<IReadOnlyList<Claim>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await claimsContext.Claims
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<Claim?> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            return await claimsContext.Claims
                .AsNoTracking()
                .FirstOrDefaultAsync(claim => claim.Id == id, cancellationToken);
        }
    }
}
