using Claims.Application.Dto;
using Claims.Application.Interfaces;
using FluentValidation;

namespace Claims.Application.Validation
{
    public sealed class ClaimValidator : AbstractValidator<ClaimDto>
    {
        public ClaimValidator(ICoverRepository coverRepository)
        {
            RuleFor(claim => claim.CoverId)
                .NotEmpty()
                .WithMessage("CoverId cannot be null.");

            RuleFor(claim => claim.DamageCost)
                .LessThanOrEqualTo(100_000m)
                .WithMessage("DamageCost cannot exceed 100,000.");

            RuleFor(claim => claim)
                .CustomAsync(async (claim, context, cancellationToken) =>
                {
                    if (string.IsNullOrWhiteSpace(claim.CoverId))
                        return;

                    var cover = await coverRepository.GetByIdAsync(claim.CoverId, cancellationToken);

                    if (cover is null)
                    {
                        context.AddFailure(nameof(claim.CoverId), $"Cover '{claim.CoverId}' does not exist.");

                        return;
                    }

                    if (claim.Created < cover.StartDate || claim.Created > cover.EndDate)
                    {
                        context.AddFailure(nameof(claim.Created), "Created must fall within the related cover's insurance period.");
                    }
                });
        }
    }
}