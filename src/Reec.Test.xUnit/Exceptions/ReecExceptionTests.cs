using Reec.Inspection;
using static Reec.Inspection.ReecEnums;

namespace Reec.Test.xUnit.Exceptions
{
    /// <summary>
    /// Tests para ReecException - Sistema de excepciones personalizadas
    /// </summary>
    public class ReecExceptionTests
    {
        [Theory]
        [InlineData(Category.Warning, "Campo requerido")]
        [InlineData(Category.BusinessLogic, "Saldo insuficiente")]
        [InlineData(Category.Unauthorized, "Token inválido")]
        public void Constructor_WithCategoryAndMessage_ShouldCreateException(Category category, string message)
        {
            // Act
            var exception = new ReecException(category, message);

            // Assert
            exception.Should().NotBeNull();
            exception.ReecMessage.Should().NotBeNull();
            exception.ReecMessage.Category.Should().Be(category);
            exception.ReecMessage.Message.Should().ContainSingle(m => m == message);
            exception.Message.Should().Be(message);
        }

        [Fact]
        public void Constructor_WithCategoryAndMessageList_ShouldCreateException()
        {
            // Arrange
            var messages = new List<string> { "Error 1", "Error 2", "Error 3" };

            // Act
            var exception = new ReecException(Category.Warning, messages);

            // Assert
            exception.Should().NotBeNull();
            exception.ReecMessage.Message.Should().HaveCount(3);
            exception.ReecMessage.Message.Should().BeEquivalentTo(messages);
            exception.Message.Should().Be("Error 1|Error 2|Error 3");
        }

        [Fact]
        public void Constructor_WithExceptionMessage_ShouldStoreOriginalException()
        {
            // Arrange
            var userMessage = "Error al procesar la solicitud";
            var exceptionMessage = "NullReferenceException: Object reference not set";

            // Act
            var exception = new ReecException(Category.BusinessLogic, userMessage, exceptionMessage);

            // Assert
            exception.Should().NotBeNull();
            exception.ReecMessage.Message.Should().ContainSingle(m => m == userMessage);
            exception.ExceptionMessage.Should().Be(exceptionMessage);
            exception.Message.Should().Be(exceptionMessage);
        }

        [Fact]
        public void Constructor_WithInnerException_ShouldPreserveInnerException()
        {
            // Arrange
            var userMessage = "Error de conexión";
            var exceptionMessage = "HttpRequestException";
            var innerException = new InvalidOperationException("Connection timeout");

            // Act
            var exception = new ReecException(
                Category.BadGateway,
                userMessage,
                exceptionMessage,
                innerException);

            // Assert
            exception.Should().NotBeNull();
            exception.InnerException.Should().NotBeNull();
            exception.InnerException.Should().BeSameAs(innerException);
            exception.InnerException!.Message.Should().Be("Connection timeout");
        }

        [Theory]
        [InlineData(Category.OK, 200)]
        [InlineData(Category.Warning, 460)]
        [InlineData(Category.BusinessLogic, 465)]
        [InlineData(Category.InternalServerError, 500)]
        public void ReecMessage_ShouldHaveCorrectCategory(Category category, int expectedValue)
        {
            // Act
            var exception = new ReecException(category, "Test message");

            // Assert
            exception.ReecMessage.Category.Should().Be(category);
            ((int)exception.ReecMessage.Category).Should().Be(expectedValue);
        }

        [Fact]
        public void ExceptionMessage_WithoutExplicitException_ShouldBeNull()
        {
            // Act
            var exception = new ReecException(Category.Warning, "Simple message");

            // Assert
            exception.ExceptionMessage.Should().BeNull();
        }

        [Fact]
        public void MultipleMessages_ShouldBeJoinedWithPipe()
        {
            // Arrange
            var messages = new List<string> { "Error A", "Error B" };

            // Act
            var exception = new ReecException(Category.Warning, messages);

            // Assert
            exception.Message.Should().Be("Error A|Error B");
        }

        [Fact]
        public void ReecMessage_ShouldBeMutable()
        {
            // Arrange
            var exception = new ReecException(Category.Warning, "Initial message");

            // Act
            exception.ReecMessage.Path = "/api/test";
            exception.ReecMessage.TraceIdentifier = "trace-123";

            // Assert
            exception.ReecMessage.Path.Should().Be("/api/test");
            exception.ReecMessage.TraceIdentifier.Should().Be("trace-123");
        }
    }
}
