using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.IO;
using System.Threading.Tasks;
using TransactionService.Infrastructure.Factories;
using TransactionService.Infrastructure.Messaging;
using Xunit;

namespace TransactionService.Infrastructure.Tests.Factories
{
    public class TransactionEventPublisherFactoryTests
    {
        [Fact]
        public async Task CreateAsync_ShouldReturnInitializedPublisher()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var rabbitMqSettings = configuration.GetSection("RabbitMq").Get<RabbitMqSettings>();

            if (rabbitMqSettings is null)
                throw new InvalidOperationException("RabbitMq settings could not be loaded from configuration.");

            var options = Options.Create(rabbitMqSettings);


            var loggerMock = new Mock<ILogger<TransactionEventPublisher>>();

            var factory = new TransactionEventPublisherFactory(options, loggerMock.Object);

            // Act
            var publisher = await factory.CreateAsync();

            // Assert
            Assert.NotNull(publisher);
            Assert.IsType<TransactionEventPublisher>(publisher);
        }
    }
}
