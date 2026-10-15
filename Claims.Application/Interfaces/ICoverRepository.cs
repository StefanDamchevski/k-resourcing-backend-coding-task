using Claims.Domain.Entities;

namespace Claims.Application.Interfaces
{
    public interface ICoverRepository
    {
        Task<IReadOnlyList<Cover>> GetAllAsync(CancellationToken cancellationToken);

        Task<Cover?> GetByIdAsync(string id, CancellationToken cancellationToken);

        Task AddAsync(Cover cover, CancellationToken cancellationToken);

        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
    }
}
