using AutoMapper;
using Claims.Application.Dto;
using Claims.Application.Extensions;
using Claims.Application.Interfaces;
using Claims.Application.Validation;
using Claims.Domain.Entities;
using Claims.Domain.Enums;

namespace Claims.Application.Services
{
    public class CoverService(ICoverRepository coverRepository, IAuditQueue auditQueue, CoverValidator validator, IMapper mapper) : ICoverService
    {
        public async Task<IReadOnlyList<CoverDto>> GetAllAsync(CancellationToken cancellationToken)
        {
            var covers = await coverRepository.GetAllAsync(cancellationToken);

            return mapper.Map<IReadOnlyList<CoverDto>>(covers);
        }

        public async Task<CoverDto?> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            var cover = await coverRepository.GetByIdAsync(id, cancellationToken);

            return mapper.Map<CoverDto>(cover);
        }

        public async Task<CoverDto> CreateAsync(CoverDto cover, CancellationToken cancellationToken)
        {
            var result = await validator.ValidateAsync(cover, cancellationToken);

            result?.HandleErrors();

            cover.Id = Guid.NewGuid().ToString();

            cover.Premium = ComputePremium(cover.StartDate, cover.EndDate, cover.Type);

            var entity = mapper.Map<Cover>(cover);

            await coverRepository.AddAsync(entity, cancellationToken);

            auditQueue.Enqueue(AuditEntityTypes.Cover, cover.Id, AuditActionType.Create);

            return cover;
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
        {
            var deleted = await coverRepository.DeleteAsync(id, cancellationToken);

            if (!deleted)
                return false;

            auditQueue.Enqueue(AuditEntityTypes.Cover, id, AuditActionType.Delete);

            return true;
        }

        public decimal ComputePremium(DateOnly startDate, DateOnly endDate, CoverType coverType)
        {
            var dailyRate = 1250m * GetTypeMultiplier(coverType);

            var insuranceDays = endDate.DayNumber - startDate.DayNumber;

            var firstPeriodDays = Math.Min(insuranceDays, 30);
            var secondPeriodDays = Math.Clamp(insuranceDays - 30, 0, 150);
            var remainingDays = Math.Max(insuranceDays - 180, 0);

            var firstPeriodPremium = firstPeriodDays * dailyRate;

            var secondPeriodPremium = secondPeriodDays * dailyRate * (1m - GetSecondPeriodDiscount(coverType));

            var remainingPeriodPremium = remainingDays * dailyRate * (1m - GetRemainingPeriodDiscount(coverType));

            return firstPeriodPremium + secondPeriodPremium + remainingPeriodPremium;
        }

        private static decimal GetTypeMultiplier(CoverType coverType) =>
            coverType switch
            {
                CoverType.Yacht => 1.1m,
                CoverType.PassengerShip => 1.2m,
                CoverType.Tanker => 1.5m,
                _ => 1.3m
            };

        private static decimal GetSecondPeriodDiscount(CoverType coverType) =>
            coverType == CoverType.Yacht ? 0.05m : 0.02m;

        private static decimal GetRemainingPeriodDiscount(CoverType coverType) =>
            coverType == CoverType.Yacht ? 0.08m : 0.03m;
    }
}
