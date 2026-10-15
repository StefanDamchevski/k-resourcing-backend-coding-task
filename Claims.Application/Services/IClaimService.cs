using Claims.Application.Dto;

namespace Claims.Application.Services
{
    public interface IClaimService
    {
        Task<IReadOnlyList<ClaimDto>> GetAllAsync(CancellationToken cancellationToken);

        Task<ClaimDto?> GetByIdAsync(string id, CancellationToken cancellationToken);

        Task<ClaimDto> CreateAsync(ClaimDto claim, CancellationToken cancellationToken);

        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
    }
}