using Claims.Application;
using Claims.Extensions;
using Claims.Infrastructure;
using Claims.Middleware;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services
    .AddApplication()
    .AddMappings()
    .AddInfrastructure()
    .AddSwagger();

await builder.Services.AddClaimsContextAsync(builder.Configuration);

await builder.Services.AddAuditingContextAsync();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app
  .UseHttpsRedirection()
  .UseMiddleware<ExceptionHandlingMiddleware>()
  .UseAuthorization();

app.MapControllers();

await app.Services.ApplyMigrationsAsync();

await app.RunAsync();

public partial class Program
{

}