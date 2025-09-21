using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TransactionService.Domain.Events;
using TransactionService.Domain.Interfaces;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TransactionService.Domain.Factories;
using TransactionService.Domain.Entities;

namespace TransactionService.Infrastructure.Messaging
{
    public class TransactionEventConsumer : ITransactionEventConsumer, IHealthCheck
    {
        private readonly RabbitMqSettings _settings;
        private readonly ILogger<TransactionEventConsumer> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly Meter _meter;
        private readonly Dictionary<string, Type> _eventTypeMap;
        
        private IConnection? _connection;
        private IChannel? _channel;
        private bool _isHealthy = false;
        
        // Metrics
        private readonly Counter<long> _messagesProcessed;
        private readonly Counter<long> _messagesRejected;
        private readonly Counter<long> _messagesRequeued;
        private readonly Histogram<double> _processingDuration;

        // Circuit breaker state
        private DateTime _lastFailureTime = DateTime.MinValue;
        private int _consecutiveFailures = 0;
        private readonly int _maxConsecutiveFailures = 5;
        private readonly TimeSpan _circuitBreakerTimeout = TimeSpan.FromMinutes(2);

        public TransactionEventConsumer(
            IOptions<RabbitMqSettings> options,
            ILogger<TransactionEventConsumer> logger,
            IServiceScopeFactory serviceScopeFactory,
            IMeterFactory? meterFactory = null)
        {
            _settings = options.Value;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _meter = meterFactory?.Create("TransactionService.Consumer") ?? new Meter("TransactionService.Consumer");
            // Initialize event type mapping
            _eventTypeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                { "CreateTransaction", typeof(CreateTransactionEvent) }
            };
            
            // Initialize metrics
            _messagesProcessed = _meter.CreateCounter<long>("messages_processed_total", "Total number of messages processed");
            _messagesRejected = _meter.CreateCounter<long>("messages_rejected_total", "Total number of messages rejected");
            _messagesRequeued = _meter.CreateCounter<long>("messages_requeued_total", "Total number of messages requeued");
            _processingDuration = _meter.CreateHistogram<double>("message_processing_duration_seconds", "Time taken to process a message");
        }

        public async Task StartConsumingAsync(CancellationToken cancellationToken)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.HostName,
                    Port = _settings.Port,
                    UserName = _settings.UserName,
                    Password = _settings.Password,
                    // Add connection resilience
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = await factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                // Declare main queue
                await _channel.QueueDeclareAsync(
                    queue: _settings.QueueName, 
                    durable: true, 
                    exclusive: false, 
                    autoDelete: false, 
                    arguments: null, 
                    cancellationToken: cancellationToken);

                // Declare Dead Letter Queue
                var dlqName = $"{_settings.QueueName}.dlq";
                await _channel.QueueDeclareAsync(
                    queue: dlqName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: cancellationToken);

                // Set QoS to process one message at a time for better error handling
                await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    await ProcessMessageAsync(ea, cancellationToken);
                };

                // Use manual ACK (autoAck: false) for better control
                await _channel.BasicConsumeAsync(
                    queue: _settings.QueueName, 
                    autoAck: false, 
                    consumer: consumer, 
                    cancellationToken: cancellationToken);

                _isHealthy = true;
                _logger.LogInformation("Started consuming from queue: {QueueName}", _settings.QueueName);
            }
            catch (Exception ex)
            {
                _isHealthy = false;
                _logger.LogError(ex, "Failed to start consuming from queue: {QueueName}", _settings.QueueName);
                throw;
            }
        }

        private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            var messageProcessed = false;
            var startTime = DateTime.UtcNow;
            
            // Check circuit breaker
            if (IsCircuitBreakerOpen())
            {
                _logger.LogWarning("Circuit breaker is open, requeuing message");
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken);
                _messagesRequeued.Add(1);
                return;
            }
            
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                _logger.LogDebug("Received message: {Message}", message);
                
                // Parse message to determine event type
                var eventWrapper = JsonSerializer.Deserialize<JsonElement>(message);
                
                if (!eventWrapper.TryGetProperty("EventType", out var eventTypeProperty))
                {
                    _logger.LogError("Message missing EventType property");
                    await SendToDeadLetterQueue(ea, "Message missing EventType property", cancellationToken);
                    await _channel!.BasicRejectAsync(ea.DeliveryTag, requeue: false, cancellationToken);
                    _messagesRejected.Add(1);
                    return;
                }

                var eventType = eventTypeProperty.GetString();
                if (string.IsNullOrEmpty(eventType) || !_eventTypeMap.TryGetValue(eventType, out Type? eventDataType))
                {
                    _logger.LogError("Unknown or unsupported event type: {EventType}", eventType);
                    await SendToDeadLetterQueue(ea, $"Unknown event type: {eventType}", cancellationToken);
                    await _channel!.BasicRejectAsync(ea.DeliveryTag, requeue: false, cancellationToken);
                    _messagesRejected.Add(1);
                    return;
                }

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters =
                    {
                        new EnumMemberConverter<TransactionCurrency>(),
                        new EnumMemberConverter<TransactionType>(),
                        new EnumMemberConverter<TransactionChannel>()
                    }
                };

                if (JsonSerializer.Deserialize(message, eventDataType, jsonOptions) is not ITransactionEvent eventData)
                {
                    _logger.LogWarning("Received null transaction event, sending to DLQ");
                    await SendToDeadLetterQueue(ea, "Null transaction event", cancellationToken);
                    await _channel!.BasicRejectAsync(ea.DeliveryTag, requeue: false, cancellationToken);
                    _messagesRejected.Add(1);
                    return;
                }

                _logger.LogInformation("Processing {EventType} for AccountId: {AccountId}", eventType, eventData.AccountId);

                // Route to appropriate handler
                await RouteEventToHandlerAsync(eventData, cancellationToken);

                // Only ACK after everything succeeds
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken);
                messageProcessed = true;
                
                // Reset circuit breaker on success
                _consecutiveFailures = 0;
                
                _messagesProcessed.Add(1);
                _logger.LogInformation("Successfully processed {EventType} for AccountId: {AccountId}", eventType, eventData.AccountId);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to deserialize message, sending to DLQ");
                await SendToDeadLetterQueue(ea, $"JSON deserialization error: {jsonEx.Message}", cancellationToken);
                
                if (!messageProcessed)
                {
                    await _channel!.BasicRejectAsync(ea.DeliveryTag, requeue: false, cancellationToken);
                    _messagesRejected.Add(1);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Message processing cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message, will requeue for retry");
                
                // Update circuit breaker state
                _consecutiveFailures++;
                _lastFailureTime = DateTime.UtcNow;
                
                // Check if message should be sent to DLQ after max retries
                var retryCount = GetRetryCount(ea);
                const int maxRetries = 3;
                
                if (retryCount >= maxRetries)
                {
                    _logger.LogError("Max retries exceeded, sending to DLQ");
                    await SendToDeadLetterQueue(ea, $"Max retries exceeded. Last error: {ex.Message}", cancellationToken);
                    await _channel!.BasicRejectAsync(ea.DeliveryTag, requeue: false, cancellationToken);
                    _messagesRejected.Add(1);
                }
                else
                {
                    // Requeue with delay for exponential backoff
                    if (!messageProcessed)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
                        await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken);
                        _messagesRequeued.Add(1);
                    }
                }
            }
            finally
            {
                // Record processing duration
                var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                _processingDuration.Record(duration);
            }
        }
        
        private async Task RouteEventToHandlerAsync(ITransactionEvent eventData, CancellationToken cancellationToken)
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();

            switch (eventData)
            {
                case CreateTransactionEvent createEvent:
                    var createHandler = scope.ServiceProvider.GetRequiredService<IEventHandler<CreateTransactionEvent>>();
                    await createHandler.HandleAsync(createEvent, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("No handler found for event type: {EventType}", eventData.EventType);
                    throw new InvalidOperationException($"No handler registered for event type: {eventData.EventType}");
            }
        }

        private async Task SendToDeadLetterQueue(BasicDeliverEventArgs ea, string reason, CancellationToken cancellationToken)
        {
            try
            {
                var dlqName = $"{_settings.QueueName}.dlq";
                var dlqMessage = new
                {
                    OriginalMessage = Encoding.UTF8.GetString(ea.Body.ToArray()),
                    Reason = reason,
                    Timestamp = DateTime.UtcNow,
                    OriginalQueue = _settings.QueueName,
                    DeliveryTag = ea.DeliveryTag
                };

                var dlqBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dlqMessage));
                await _channel!.BasicPublishAsync(
                    exchange: "",
                    routingKey: dlqName,
                    body: dlqBody,
                    cancellationToken);

                _logger.LogWarning("Message sent to DLQ: {Reason}", reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to DLQ");
            }
        }

        private bool IsCircuitBreakerOpen()
        {
            if (_consecutiveFailures < _maxConsecutiveFailures)
                return false;

            return DateTime.UtcNow - _lastFailureTime < _circuitBreakerTimeout;
        }

        private static int GetRetryCount(BasicDeliverEventArgs ea)
        {
            // Simple retry count - in production, you might want to use message headers
            // or a more sophisticated approach
            return ea.Redelivered ? 1 : 0;
        }

        // Health Check Implementation
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var isHealthy = _isHealthy && 
                               _connection?.IsOpen == true && 
                               _channel?.IsOpen == true &&
                               !IsCircuitBreakerOpen();

                if (isHealthy)
                {
                    return Task.FromResult(HealthCheckResult.Healthy("Consumer is healthy"));
                }

                var reasons = new List<string>();
                if (!_isHealthy) reasons.Add("Consumer not started");
                if (_connection?.IsOpen != true) reasons.Add("Connection closed");
                if (_channel?.IsOpen != true) reasons.Add("Channel closed");
                if (IsCircuitBreakerOpen()) reasons.Add("Circuit breaker open");

                return Task.FromResult(HealthCheckResult.Unhealthy($"Consumer is unhealthy: {string.Join(", ", reasons)}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"Health check failed: {ex.Message}"));
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                _isHealthy = false;
                
                if (_channel != null)
                    await _channel.DisposeAsync();

                if (_connection != null)
                    await _connection.DisposeAsync();

                _meter?.Dispose();
                
                _logger.LogInformation("RabbitMQ consumer disposed.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during RabbitMQ consumer disposal.");
            }
        }
    }
}