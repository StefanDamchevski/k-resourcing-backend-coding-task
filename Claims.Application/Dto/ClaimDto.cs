using Claims.Domain.Enums;

namespace Claims.Application.Dto
{
    public class ClaimDto
    {
        public string Id { get; set; } = string.Empty;

        public string CoverId { get; set; } = string.Empty;

        public DateOnly Created { get; set; }

        public string Name { get; set; } = string.Empty;

        public ClaimType Type { get; set; }

        public decimal DamageCost { get; set; }
    }
}
