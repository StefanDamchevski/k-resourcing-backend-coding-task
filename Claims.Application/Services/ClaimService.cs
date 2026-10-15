using AutoMapper;
using Claims.Application.Dto;
using Claims.Application.Extensions;
using Claims.Application.Interfaces;
using Claims.Application.Validation;
using Claims.Domain.Entities;
using Claims.Domain.Enums;

namespace Claims.Application.Services
{
    public class ClaimService(IClaimRepository claimRepository, IAuditQueue auditQueue, ClaimValidator validator, IMapper mapper) : IClaimService
    {
        public async Task<ClaimDto> CreateAsync(ClaimDto claim, CancellationToken cancellationToken)
        {
            var result = await validator.ValidateAsync(claim, cancellationToken);

            result?.HandleErrors();

            claim.Id = Guid.NewGuid().ToString();

            var entity = mapper.Map<Claim>(claim);

            await claimRepository.AddAsync(entity, cancellationToken);

            auditQueue.Enqueue(AuditEntityTypes.Claim, claim.Id, AuditActionType.Create);

            return claim;
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
        {
            var deleted = await claimRepository.DeleteAsync(id, cancellationToken);

            if (!deleted)
                return false;

            auditQueue.Enqueue(AuditEntityTypes.Claim, id, AuditActionType.Delete);

            return true;
        }

        public async Task<IReadOnlyList<ClaimDto>> GetAllAsync(CancellationToken cancellationToken)
        {
            var claims = await claimRepository.GetAllAsync(cancellationToken);

            return mapper.Map<IReadOnlyList<ClaimDto>>(claims);
        }

        public async Task<ClaimDto?> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            var claim = await claimRepository.GetByIdAsync(id, cancellationToken);

            return mapper.Map<ClaimDto>(claim);
        }
    }
}
