using Claims.Application.Interfaces;
using Claims.Domain.Entities;
using Claims.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure.Repositories
{
    public class CoverRepository(ClaimsContext claimsContext) : ICoverRepository
    {
        public async Task<IReadOnlyList<Cover>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await claimsContext.Covers
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public Task<Cover?> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            return claimsContext.Covers
                .AsNoTracking()
                .SingleOrDefaultAsync(cover => cover.Id == id, cancellationToken);
        }

        public async Task AddAsync(Cover cover, CancellationToken cancellationToken)
        {
            await claimsContext.Covers.AddAsync(cover, cancellationToken);

            await claimsContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
        {
            var cover = await claimsContext.Covers
                .SingleOrDefaultAsync(cover => cover.Id == id, cancellationToken);

            if (cover is null)
            {
                return false;
            }

            claimsContext.Covers.Remove(cover);

            await claimsContext.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
