using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MediatR;
using System.Reflection;
using TransactionService.Grpc;
using TransactionService.Application.Handlers;
using TransactionService.Infrastructure.DocGen;
using TransactionService.Infrastructure.Signing;
using TransactionService.Infrastructure.Extensions;
using TransactionService.Infrastructure.Messaging;
using TransactionService.Infrastructure.Interfaces;
using TransactionService.Infrastructure.Factories;
using Grpc.Net.ClientFactory;
using Serilog;

// Configure Serilog early to capture startup logs
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/transaction-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/transaction-service-.txt", rollingInterval: RollingInterval.Day));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});


// MediatR
builder.Services.AddMediatR(typeof(GetTransactionHandler).Assembly);
builder.Services.AddMediatR(typeof(GenerateReceiptHandler).Assembly);
builder.Services.AddMediatR(typeof(CreateShareableLinkHandler).Assembly);

// Infrastructure bindings
builder.Services.AddSingleton<ITransactionEventPublisherFactory, TransactionEventPublisherFactory>();
builder.Services.AddSingleton<IDocumentGenerator, PdfDocumentGenerator>();
builder.Services.AddSingleton<ILinkSigner>(sp => new HmacLinkSigner(builder.Configuration["Signing:Key"] ?? "dev-key"));
builder.Services.AddSingleton<IInMemoryQueue, InMemoryQueue>();

// Add health checks
builder.Services.AddHealthChecks();

// Add infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

var grpcAddress = builder.Configuration["GrpcSettings:GrpcServer"];

builder.Services.AddGrpcClient<Greeter.GreeterClient>(o =>
{
    o.Address = new Uri(grpcAddress!);
});

var app = builder.Build();

// Add Serilog request logging
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Transaction Service API v1");
            c.RoutePrefix = string.Empty; // Serve Swagger UI at root
        });
}

app.UseRouting();
app.UseAuthorization();

// Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";

    await next();
});

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (error != null)
        {
            Log.Error(error.Error, "Unhandled exception occurred");
            
            var response = new
            {
                error = "An internal server error occurred",
                requestId = Activity.Current?.Id ?? context.TraceIdentifier
            };
            
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    });
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                exception = x.Value.Exception?.Message,
                duration = x.Value.Duration.ToString()
            }),
            totalTime = report.TotalDuration.ToString()
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

app.MapControllers();

app.MapGet("/", () => "Transaction Service API is running.");

app.Run();

public partial class Program { } // for tests