using AutoMapper;
using Claims.Application.Mappings;
using Claims.Application.Services;
using Claims.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Claims.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            return services
                .AddScoped<IClaimService, ClaimService>()
                .AddScoped<ICoverService, CoverService>()
                .AddValidatorsFromAssemblyContaining<CoverValidator>()
                .AddValidatorsFromAssemblyContaining<ClaimValidator>();
        }

        public static IServiceCollection AddMappings(this IServiceCollection services)
        {
            return services.AddSingleton<IMapper>(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

                var mapperConfig = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile<ClaimProfile>();
                }, loggerFactory);

                return mapperConfig.CreateMapper();
            });
        }
    }
}
