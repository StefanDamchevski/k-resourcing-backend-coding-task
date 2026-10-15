using System.Reflection;

namespace Claims.Extensions
{
    public static class Swagger
    {
        public static IServiceCollection AddSwagger(this IServiceCollection services)
        {
            return services
                .AddEndpointsApiExplorer()
                .AddSwaggerGen()
                .AddSwaggerGen(options =>
                {
                    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                    options.IncludeXmlComments(xmlPath);
                });
        }
    }
}
