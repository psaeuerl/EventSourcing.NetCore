using Core;
using Core.Configuration;
using Core.Exceptions;
using Core.Kafka;
using Core.OpenTelemetry;
using Core.WebApi.Middlewares.ExceptionHandling;
using Core.WebApi.OptimisticConcurrency;
using Core.WebApi.Swagger;
using Marten.Events.Daemon;
using Marten.Exceptions;
using Microsoft.OpenApi.Models;
using Npgsql;
using OpenTelemetry.Trace;
using Payments;

var builder = WebApplication.CreateBuilder(args);

builder.AddKafkaProducer<string, string>("kafka", settings =>
{
    settings.Config.AllowAutoCreateTopics = true;
});

builder.Services
    .AddNpgsqlDataSource(builder.Configuration.GetRequiredConnectionString("payments"))
    .AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Payments", Version = "v1" });
        c.OperationFilter<MetadataOperationFilter>();
    })
    .AddKafkaProducer()
    .AddCoreServices()
    .AddDefaultExceptionHandler(
        (exception, _) => exception switch
        {
            AggregateNotFoundException => exception.MapToProblemDetails(StatusCodes.Status404NotFound),
            ConcurrencyException => exception.MapToProblemDetails(StatusCodes.Status412PreconditionFailed),
            _ => null
        })
    .AddPaymentsModule(builder.Configuration)
    .AddOptimisticConcurrencyMiddleware()
    .AddOpenTelemetry("Payments", OpenTelemetryOptions.Build(options =>
        options
            .WithTracing(t => t.AddSource("Marten"))
            .WithMetrics(m => m.AddMeter("Marten"))
            .DisableConsoleExporter(true)
    ))
    .AddControllers();

builder.Services
    .AddHealthChecks()
    .AddMartenAsyncDaemonHealthCheck(maxEventLag: 500);

var app = builder.Build();

app.UseExceptionHandler()
    .UseOptimisticConcurrencyMiddleware()
    .UseRouting()
    .UseAuthorization()
    .UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    })
    .UseSwagger()
    .UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payments V1");
        c.RoutePrefix = string.Empty;
    });

app.Run();

public partial class Program
{
}
