using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TransactionService.Domain.Interfaces;
using TransactionService.Domain.Events;
using Microsoft.Extensions.Logging;

namespace TransactionService.Infrastructure.Messaging
{
    public class TransactionEventPublisher(IOptions<RabbitMqSettings> options, ILogger<TransactionEventPublisher> logger) : ITransactionEventPublisher, IDisposable, IAsyncDisposable
    {
        private IChannel? _channel;
        private IConnection? _connection;
        private readonly RabbitMqSettings _settings = options.Value ?? throw new ArgumentNullException(nameof(options));
        private readonly ILogger<TransactionEventPublisher> _logger = logger;
        private bool _disposed = false;

        // Initialize connection and channel - call this method during startup
        public async Task InitializeAsync()
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password
            };

            // Create connection and channel from factory, not from null references
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }

        public async Task PublishAsync(TransactionCreatedEvent @event)
        {
            if (_disposed)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
            }

            if (_channel == null)
            {
                _logger.LogError("Publisher not initialized. Call Method: {MethodName} first", nameof(InitializeAsync));
                throw new InvalidOperationException("Publisher not initialized. Call InitializeAsync first.");
            }

            try
            {
                var message = JsonSerializer.Serialize(@event);
                var body = Encoding.UTF8.GetBytes(message);

                // Use the correct BasicPublishAsync overload for RabbitMQ.Client 7.x
                await _channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: _settings.QueueName,
                    body: body);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                _logger.LogError(ex, "Publisher not initialized. Call InitializeAsync first.");
                throw new InvalidOperationException($"Failed to publish event: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    _channel?.Dispose();
                    _connection?.Dispose();
                }
                catch
                {
                    // Ignore disposal exceptions
                }
                _disposed = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                try
                {
                    if (_channel is IAsyncDisposable asyncChannel)
                        await asyncChannel.DisposeAsync();
                    else
                        _channel?.Dispose();

                    if (_connection is IAsyncDisposable asyncConnection)
                        await asyncConnection.DisposeAsync();
                    else
                        _connection?.Dispose();
                }
                catch
                {
                    // Ignore disposal exceptions
                }

                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}