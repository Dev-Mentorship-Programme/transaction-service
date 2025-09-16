using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TransactionService.Domain.Interfaces;

namespace TransactionService.Worker
{
    public class CreateTransactionEventConsumerWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<CreateTransactionEventConsumerWorker> logger) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly ILogger<CreateTransactionEventConsumerWorker> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting RabbitMQ consumer...");

            await using var scope = _scopeFactory.CreateAsyncScope();
            var consumer = scope.ServiceProvider.GetRequiredService<ICreateTransactionEventConsumer>();

            await consumer.StartConsumingAsync(stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var consumer = scope.ServiceProvider.GetRequiredService<ICreateTransactionEventConsumer>();

            await consumer.DisposeAsync();
            _logger.LogInformation("RabbitMQ consumer stopped.");
        }
    }
}
