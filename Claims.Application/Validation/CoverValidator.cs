using Claims.Application.Dto;
using FluentValidation;

namespace Claims.Application.Validation
{
    public sealed class CoverValidator : AbstractValidator<CoverDto>
    {
        public CoverValidator()
        {
            RuleFor(x => x.StartDate)
                .Must(startDate => startDate >= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("StartDate cannot be in the past.");

            RuleFor(x => x.EndDate)
                .Must((cover, endDate) =>
                    endDate >= cover.StartDate)
                .WithMessage("EndDate cannot be earlier than StartDate.");

            RuleFor(x => x.EndDate)
                .Must((cover, endDate) =>
                    endDate <= cover.StartDate.AddYears(1))
                .When(x => x.EndDate >= x.StartDate)
                .WithMessage("The insurance period cannot exceed one year.");
        }
    }
}