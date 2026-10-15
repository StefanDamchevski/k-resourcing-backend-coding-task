using Claims.Domain.Entities;

namespace Claims.Application.Interfaces
{
    public interface IClaimRepository
    {
        Task<IReadOnlyList<Claim>> GetAllAsync(CancellationToken cancellationToken);

        Task<Claim?> GetByIdAsync(string id, CancellationToken cancellationToken);

        Task AddAsync(Claim claim, CancellationToken cancellationToken);

        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
    }
}
