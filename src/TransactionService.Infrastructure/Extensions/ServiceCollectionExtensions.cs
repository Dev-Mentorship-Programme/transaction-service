using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using TransactionService.Domain.Interfaces;
using TransactionService.Infrastructure.Interfaces;
using TransactionService.Infrastructure.Factories;
using TransactionService.Infrastructure.Messaging;

namespace TransactionService.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Configure RabbitMQ settings using Action<T> overload
            services.Configure<RabbitMqSettings>(options =>
            {
                var section = configuration.GetSection("RabbitMq");
                section.Bind(options);
            });

            // Register messaging services
            services.AddScoped<ITransactionEventPublisherFactory, TransactionEventPublisherFactory>();
            services.AddScoped<ITransactionEventPublisher, TransactionEventPublisher>();
            services.AddScoped<ITransactionEventConsumer, TransactionEventConsumer>();

            // Add health checks
            services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbitmq");

            return services;
        }
    }
}