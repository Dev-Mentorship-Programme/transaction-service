using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionService.Domain.Interfaces;
using TransactionService.Infrastructure.Interfaces;
using TransactionService.Infrastructure.Messaging;

namespace TransactionService.Infrastructure.Factories
{
    public class TransactionEventPublisherFactory(IOptions<RabbitMqSettings> options, ILogger<TransactionEventPublisher> logger) : ITransactionEventPublisherFactory
    {
        private readonly IOptions<RabbitMqSettings> _options = options;
        private readonly ILogger<TransactionEventPublisher> _logger = logger;

        public async Task<ITransactionEventPublisher> CreateAsync()
        {
            var publisher = new TransactionEventPublisher(_options, _logger);
            await publisher.InitializeAsync();
            return publisher;
        }
    }
}