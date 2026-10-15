using Claims.Application.Interfaces;
using Claims.Infrastructure.Auditing;
using Claims.Infrastructure.Persistence;
using Claims.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System.Runtime.InteropServices;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;

namespace Claims.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            return services
                .AddScoped<IClaimRepository, ClaimRepository>()
                .AddScoped<IAuditRepository, AuditRepository>()
                .AddScoped<ICoverRepository, CoverRepository>()
                .AddSingleton<IAuditQueue, AuditQueue>()
                .AddHostedService<AuditBackgroundService>();
        }

        public static async Task AddClaimsContextAsync(this IServiceCollection services, IConfiguration configuration)
        {
            var mongoContainer = new MongoDbBuilder()
                .WithImage("mongo:latest")
                .Build();

            await mongoContainer.StartAsync();

            services.AddDbContext<ClaimsContext>(options =>
            {
                var client = new MongoClient(mongoContainer.GetConnectionString());
                var database = client.GetDatabase(configuration["MongoDb:DatabaseName"]); // Use a default/test database name
                options.UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName);
            });
        }

        public static async Task AddAuditingContextAsync(this IServiceCollection services)
        {
            var sqlContainer = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    ? new MsSqlBuilder()
                        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                    : new()

                ).Build();

            await sqlContainer.StartAsync();

            services.AddDbContext<AuditContext>(options => options.UseSqlServer(sqlContainer.GetConnectionString()));
        }

        public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            var context = scope.ServiceProvider.GetRequiredService<AuditContext>();

            await context.Database.MigrateAsync();
        }
    }
}