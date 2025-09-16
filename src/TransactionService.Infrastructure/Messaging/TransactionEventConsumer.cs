using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TransactionService.Domain.Events;
using TransactionService.Domain.Interfaces;
using TransactionService.Application.Commands;

namespace TransactionService.Infrastructure.Messaging
{
    public class CreateTransactionEventConsumer(
        IOptions<RabbitMqSettings> options,
        ILogger<CreateTransactionEventConsumer> logger, IServiceScopeFactory serviceScopeFactory) : ICreateTransactionEventConsumer
    {
        private readonly RabbitMqSettings _settings = options.Value;
        private readonly ILogger<CreateTransactionEventConsumer> _logger = logger;
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private IConnection? _connection;
        private IChannel? _channel;

        public async Task StartConsumingAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.QueueDeclareAsync(queue: _settings.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    await ProcessMessageAsync(ea, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from RabbitMQ.");
                }

                await Task.CompletedTask;
            };

            await _channel.BasicConsumeAsync(queue: _settings.QueueName, autoAck: true, consumer: consumer, cancellationToken: cancellationToken);

            _logger.LogInformation("Started consuming from queue: {QueueName}", _settings.QueueName);
        }

        private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var transactionEvent = JsonSerializer.Deserialize<CreateTransactionEvent>(message);

                if (transactionEvent == null)
                {
                    _logger.LogWarning("Received null transaction event, skipping message");
                    await _channel!.BasicNackAsync(ea.DeliveryTag, false, false, cancellationToken);
                    return;
                }

                _logger.LogInformation("Processing CreateTransactionEvent: {TransactionId}", transactionEvent.AccountId);

                // Create a new scope for processing this message
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                
                // Get MediatR from the scope and send the command directly
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                
                // Create transaction using MediatR command directly
                var command = new CreateTransactionCommand
                {
                    AccountId = transactionEvent.AccountId,
                    DestinationAccountId = transactionEvent.DestinationAccountId,
                    Amount = transactionEvent.Amount,
                    OpeningBalance = transactionEvent.OpeningBalance,
                    Narration = transactionEvent.Narration,
                    Type = transactionEvent.Type,
                    Currency = transactionEvent.Currency,
                    Channel = transactionEvent.Channel,
                    Metadata = JsonSerializer.Serialize(transactionEvent.Metadata),
                };

                var transactionId = await mediator.Send(command, cancellationToken);

                // Acknowledge the message after successful processing
                await _channel!.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                
                _logger.LogInformation("Successfully processed CreateTransactionEvent: {TransactionId}", transactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from RabbitMQ");
                
                // Reject the message and don't requeue it to avoid infinite retry loops
                // You might want to implement a dead letter queue or retry mechanism here
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, false, cancellationToken);
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_channel != null)
                    await _channel.DisposeAsync();

                if (_connection != null)
                    await _connection.DisposeAsync();

                _logger.LogInformation("RabbitMQ consumer disposed.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during RabbitMQ consumer disposal.");
            }
        }
    }
}
