using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TransactionService.Domain.Interfaces;
using TransactionService.Infrastructure.Factories;
using TransactionService.Infrastructure.Messaging;
using TransactionService.Infrastructure.Repositories;
using TransactionService.Application.Interfaces;
using TransactionService.Infrastructure.Data;
using TransactionService.Domain.Factories;
using TransactionService.Domain.Events;
using TransactionService.Application.Commands;
using TransactionService.Application.Events;
using TransactionService.Infrastructure.Interfaces;
using TransactionService.Infrastructure.Services;

namespace TransactionService.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Add DbContext here
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Register interface -> implementation
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // Configure RabbitMQ settings using Action<T> overload
        services.Configure<RabbitMqSettings>(options =>
        {
            var section = configuration.GetSection("RabbitMq");
            section.Bind(options);
        });

        services.AddMediatR(typeof(CreateTransactionCommand).Assembly);
        services.AddMediatR(typeof(TransactionCreatedNotification).Assembly);

        // Register messaging services
        services.AddScoped<ITransactionEventPublisherFactory, TransactionEventPublisherFactory>();
        services.AddScoped<ITransactionEventConsumer, TransactionEventConsumer>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IEventHandler<CreateTransactionEvent>, CreateTransactionEventHandler>();
        services.AddTransient<ITransactionFactory, TransactionFactory>();

        // Register receipt services
        services.AddScoped<ICloudinaryService, CloudinaryService>();
        services.AddScoped<IReceiptGeneratorService, ReceiptGeneratorService>();
        services.AddScoped<IReceiptService, ReceiptService>();

        // Add health checks
        services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbitmq");
        services.AddHealthChecks().AddCheck<TransactionEventConsumer>("transaction-consumer");

        return services;
        }
    }
}