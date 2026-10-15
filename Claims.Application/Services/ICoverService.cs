using Claims.Application.Dto;
using Claims.Domain.Entities;
using Claims.Domain.Enums;

namespace Claims.Application.Services
{
    public interface ICoverService
    {
        Task<IReadOnlyList<CoverDto>> GetAllAsync(CancellationToken cancellationToken);

        Task<CoverDto?> GetByIdAsync(string id, CancellationToken cancellationToken);

        decimal ComputePremium(DateOnly startDate, DateOnly endDate, CoverType coverType);

        Task<CoverDto> CreateAsync(CoverDto cover, CancellationToken cancellationToken);

        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
    }
}