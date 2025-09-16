using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using TransactionService.Domain.Interfaces;
using TransactionService.Infrastructure.Interfaces;
using TransactionService.Infrastructure.Factories;
using TransactionService.Infrastructure.Messaging;
using TransactionService.Infrastructure.Repositories;
using TransactionService.Application.Interfaces;
using TransactionService.Infrastructure.Data;
using TransactionService.Domain.Factories;

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

            // Register messaging services
            services.AddScoped<ITransactionEventPublisherFactory, TransactionEventPublisherFactory>();
            services.AddScoped<ITransactionEventPublisher, TransactionEventPublisher>();
            services.AddScoped<ICreateTransactionEventConsumer, CreateTransactionEventConsumer>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddTransient<ITransactionFactory, TransactionFactory>();

            // Add health checks
            services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbitmq");

            return services;
        }
    }
}