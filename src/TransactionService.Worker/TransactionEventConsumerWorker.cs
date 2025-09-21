using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TransactionService.Domain.Interfaces;

namespace TransactionService.Worker
{
   public class TransactionEventConsumerWorker(
       IServiceScopeFactory scopeFactory,
       ILogger<TransactionEventConsumerWorker> logger) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly ILogger<TransactionEventConsumerWorker> _logger = logger;
        private ITransactionEventPublisher? _publisher;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try 
            {
                _logger.LogInformation("Starting Transaction Event Consumer worker...");

                await using var scope = _scopeFactory.CreateAsyncScope();
                
                // Use the factory to create and initialize the publisher
                var publisherFactory = scope.ServiceProvider.GetRequiredService<ITransactionEventPublisherFactory>();
                _logger.LogInformation("Creating and initializing Transaction Event Publisher...");
                _publisher = await publisherFactory.CreateAsync();
                _logger.LogInformation("Transaction Event Publisher created and initialized successfully.");

                // Then get and start the consumer
                var consumer = scope.ServiceProvider.GetRequiredService<ITransactionEventConsumer>();
                _logger.LogInformation("Consumer retrieved, starting consumption...");
                await consumer.StartConsumingAsync(stoppingToken);
                
                _logger.LogInformation("Consumer started successfully, keeping alive...");
                
                // Keep the task alive
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TransactionEventConsumerWorker");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                
                // Stop consumer first
                var consumer = scope.ServiceProvider.GetRequiredService<ITransactionEventConsumer>();
                await consumer.DisposeAsync();
                _logger.LogInformation("Transaction Event Consumer stopped.");

                // Then dispose publisher if it was created
                if (_publisher is IAsyncDisposable asyncPublisher)
                {
                    await asyncPublisher.DisposeAsync();
                    _logger.LogInformation("Transaction Event Publisher disposed.");
                }
                else if (_publisher is IDisposable disposablePublisher)
                {
                    disposablePublisher.Dispose();
                    _logger.LogInformation("Transaction Event Publisher disposed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping TransactionEventConsumerWorker");
            }
        }
    }
}