using FluentValidation;
using FluentValidation.Results;

namespace Claims.Application.Extensions
{
    public static class ValidationError
    {
        public static void HandleErrors(this ValidationResult result)
        {
            if (result is not null && !result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }
    }
}
