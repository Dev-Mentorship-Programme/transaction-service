using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionService.Infrastructure.Services;
using Xunit;

namespace TransactionService.Infrastructure.Tests.Services
{
    public class CloudinaryServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<CloudinaryService>> _mockLogger;
        private readonly CloudinaryService _service;

        public CloudinaryServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<CloudinaryService>>();
            
            // Setup configuration
            _mockConfiguration.Setup(c => c["Cloudinary:CloudName"]).Returns("test-cloud");
            _mockConfiguration.Setup(c => c["Cloudinary:ApiKey"]).Returns("test-key");
            _mockConfiguration.Setup(c => c["Cloudinary:ApiSecret"]).Returns("test-secret");
            
            _service = new CloudinaryService(_mockConfiguration.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task UploadDocumentAsync_WithValidStream_ShouldReturnDocumentUrlAndPublicId()
        {
            // Arrange
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test content"));
            var fileName = "test-receipt.pdf";
            var folder = "receipts";

            // Act
            var result = await _service.UploadDocumentAsync(stream, fileName, folder);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(result.DocumentUrl);
                Assert.NotEmpty(result.DocumentUrl);
                Assert.NotNull(result.PublicId);
                Assert.NotEmpty(result.PublicId);
                Assert.Contains("test-cloud", result.DocumentUrl);
                Assert.StartsWith($"{folder}/receipt_", result.PublicId);
            });
        }

        [Fact]
        public async Task GenerateSecureUrlAsync_WithValidPublicId_ShouldReturnSecureUrl()
        {
            // Arrange
            var publicId = "receipts/test-receipt";
            var expirationHours = 24;

            // Act
            var result = await _service.GenerateSecureUrlAsync(publicId, expirationHours);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(result);
                Assert.NotEmpty(result);
                Assert.Contains("test-cloud", result);
                Assert.Contains("s--", result);
            });
        }

        [Fact]
        public async Task DeleteDocumentAsync_WithValidPublicId_ShouldReturnTrue()
        {
            // Arrange
            var publicId = "receipts/test-receipt";

            // Act
            var result = await _service.DeleteDocumentAsync(publicId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UploadDocumentAsync_WithDifferentFolder_ShouldUseCorrectFolder()
        {
            // Arrange
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test content"));
            var fileName = "test-statement.pdf";
            var folder = "statements";

            // Act
            var result = await _service.UploadDocumentAsync(stream, fileName, folder);

            // Assert
            Assert.StartsWith($"{folder}/receipt_", result.PublicId);
        }

        [Fact]
        public async Task GenerateSecureUrlAsync_WithDifferentExpirationHours_ShouldAcceptCustomExpiration()
        {
            // Arrange
            var publicId = "receipts/test-receipt";
            var expirationHours = 48;

            // Act
            var result = await _service.GenerateSecureUrlAsync(publicId, expirationHours);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.NotNull(result);
                Assert.NotEmpty(result);
            });
        }
    }
}
