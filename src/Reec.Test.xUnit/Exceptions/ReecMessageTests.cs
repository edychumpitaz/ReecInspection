using Reec.Inspection;
using static Reec.Inspection.ReecEnums;

namespace Reec.Test.xUnit.Exceptions
{
    /// <summary>
    /// Tests para ReecMessage - Estructura de mensajes de respuesta
    /// </summary>
    public class ReecMessageTests
    {
        [Fact]
        public void Constructor_WithCategoryAndSingleMessage_ShouldCreateMessage()
        {
            // Arrange
            var category = Category.Warning;
            var message = "Campo requerido";

            // Act
            var reecMessage = new ReecMessage(category, message);

            // Assert
            reecMessage.Should().NotBeNull();
            reecMessage.Category.Should().Be(category);
            reecMessage.CategoryDescription.Should().Be(nameof(Category.Warning));
            reecMessage.Message.Should().ContainSingle(m => m == message);
        }

        [Fact]
        public void Constructor_WithCategoryAndMessageList_ShouldCreateMessage()
        {
            // Arrange
            var category = Category.BusinessLogic;
            var messages = new List<string> { "Error 1", "Error 2" };

            // Act
            var reecMessage = new ReecMessage(category, messages);

            // Assert
            reecMessage.Message.Should().HaveCount(2);
            reecMessage.Message.Should().BeEquivalentTo(messages);
        }

        [Fact]
        public void Constructor_WithCategoryMessageAndPath_ShouldSetAllProperties()
        {
            // Arrange
            var category = Category.Unauthorized;
            var message = "Token inválido";
            var path = "/api/secure";

            // Act
            var reecMessage = new ReecMessage(category, message, path);

            // Assert
            reecMessage.Category.Should().Be(category);
            reecMessage.Message.Should().ContainSingle(m => m == message);
            reecMessage.Path.Should().Be(path);
        }

        [Theory]
        [InlineData(Category.OK, "O K")]
        [InlineData(Category.Warning, "Warning")]
        [InlineData(Category.BusinessLogic, "Business Logic")]
        [InlineData(Category.InternalServerError, "Internal Server Error")]
        public void CategoryDescription_ShouldMatchCategoryName(Category category, string expectedDescription)
        {
            // Act
            var reecMessage = new ReecMessage(category, "Test");

            // Assert
            reecMessage.CategoryDescription.Should().Be(expectedDescription);
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var reecMessage = new ReecMessage(Category.Warning, "Test");

            // Act
            reecMessage.Id = 123;
            reecMessage.Path = "/api/test";
            reecMessage.TraceIdentifier = "trace-456";

            // Assert
            reecMessage.Id.Should().Be(123);
            reecMessage.Path.Should().Be("/api/test");
            reecMessage.TraceIdentifier.Should().Be("trace-456");
        }

        [Fact]
        public void DefaultValues_ShouldBeInitialized()
        {
            // Act
            var reecMessage = new ReecMessage(Category.Warning, "Test");

            // Assert
            reecMessage.Id.Should().Be(0);
            reecMessage.Path.Should().BeNull();
            reecMessage.TraceIdentifier.Should().BeNull();
        }

        [Fact]
        public void MultipleMessages_ShouldBeStored()
        {
            // Arrange
            var messages = new List<string>
            {
                "Nombre es requerido",
                "Email es inválido",
                "Edad debe ser mayor a 18"
            };

            // Act
            var reecMessage = new ReecMessage(Category.Warning, messages);

            // Assert
            reecMessage.Message.Should().HaveCount(3);
            reecMessage.Message[0].Should().Be("Nombre es requerido");
            reecMessage.Message[1].Should().Be("Email es inválido");
            reecMessage.Message[2].Should().Be("Edad debe ser mayor a 18");
        }
    }
}
