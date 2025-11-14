using Reec.Inspection.Options;

namespace Reec.Test.xUnit.Options
{
    /// <summary>
    /// Tests para ReecExceptionOptions - Validación de configuración
    /// </summary>
    public class ReecExceptionOptionsTests
    {
        [Fact]
        public void DefaultValues_ShouldBeInitialized()
        {
            // Act
            var options = new ReecExceptionOptions();

            // Assert
            options.ApplicationErrorMessage.Should().Be("Ocurrió un error al guardar log en Base de Datos.");
            options.InternalServerErrorMessage.Should().Be("Error no controlado del sistema.");
            options.SystemTimeZoneId.Should().Be("SA Pacific Standard Time");
            options.EnableMigrations.Should().BeTrue();
            options.EnableProblemDetails.Should().BeFalse();
            options.EnableGlobalDbSave.Should().BeTrue();
            options.MinCategory.Should().Be(Reec.Inspection.ReecEnums.Category.Unauthorized);
        }

        [Fact]
        public void ApplicationName_ShouldBeSettable()
        {
            // Arrange
            var options = new ReecExceptionOptions();

            // Act
            options.ApplicationName = "TestApp";

            // Assert
            options.ApplicationName.Should().Be("TestApp");
        }

        [Fact]
        public void SystemTimeZoneId_ShouldBeCustomizable()
        {
            // Arrange
            var options = new ReecExceptionOptions();

            // Act
            options.SystemTimeZoneId = "UTC";

            // Assert
            options.SystemTimeZoneId.Should().Be("UTC");
        }

        [Fact]
        public void EnableProblemDetails_ShouldToggle()
        {
            // Arrange
            var options = new ReecExceptionOptions();

            // Act
            options.EnableProblemDetails = true;

            // Assert
            options.EnableProblemDetails.Should().BeTrue();
        }

        [Fact]
        public void LogHttp_ShouldNotBeNull()
        {
            // Arrange
            var options = new ReecExceptionOptions();

            // Assert
            options.LogHttp.Should().NotBeNull();
            options.LogHttp.Should().BeOfType<LogHttpOption>();
        }

        [Fact]
        public void LogAudit_ShouldNotBeNull()
        {
            // Arrange
            var options = new ReecExceptionOptions();

            // Assert
            options.LogAudit.Should().NotBeNull();
            options.LogAudit.Should().BeOfType<LogAuditOption>();
        }

        [Fact]
        public void LogJob_ShouldNotBeNull()
        {
            // Arrange
            var options = new ReecExceptionOptions();

            // Assert
            options.LogJob.Should().NotBeNull();
            options.LogJob.Should().BeOfType<LogJobOption>();
        }

        [Fact]
        public void LogEndpoint_ShouldNotBeNull()
        {
            // Arrange
            var options = new ReecExceptionOptions();

            // Assert
            options.LogEndpoint.Should().NotBeNull();
            options.LogEndpoint.Should().BeOfType<LogEndpointOption>();
        }

        [Fact]
        public void EnableGlobalDbSave_WhenFalse_ShouldDisablePersistence()
        {
            // Arrange
            var options = new ReecExceptionOptions();

            // Act
            options.EnableGlobalDbSave = false;

            // Assert
            options.EnableGlobalDbSave.Should().BeFalse();
        }

        [Theory]
        [InlineData("SA Pacific Standard Time")]
        [InlineData("UTC")]
        [InlineData("Eastern Standard Time")]
        public void SystemTimeZoneId_WithValidValues_ShouldAccept(string timeZoneId)
        {
            // Arrange
            var options = new ReecExceptionOptions();

            // Act
            options.SystemTimeZoneId = timeZoneId;

            // Assert
            options.SystemTimeZoneId.Should().Be(timeZoneId);
        }

        [Fact]
        public void LogHttpOption_IsSaveDB_ShouldBeConfigurable()
        {
            // Arrange
            var options = new ReecExceptionOptions();

            // Act
            options.LogHttp.IsSaveDB = false;

            // Assert
            options.LogHttp.IsSaveDB.Should().BeFalse();
        }

        [Fact]
        public void LogAuditOption_ExcludePaths_ShouldBeConfigurable()
        {
            // Arrange
            var options = new ReecExceptionOptions();

            // Act
            options.LogAudit.ExcludePaths = new[] { "swagger", "health", "metrics" };

            // Assert
            options.LogAudit.ExcludePaths.Should().HaveCount(3);
            options.LogAudit.ExcludePaths.Should().Contain("swagger");
        }

        [Fact]
        public void CustomErrorMessages_ShouldBeSettable()
        {
            // Arrange
            var options = new ReecExceptionOptions();

            // Act
            options.ApplicationErrorMessage = "Custom DB error";
            options.InternalServerErrorMessage = "Custom server error";

            // Assert
            options.ApplicationErrorMessage.Should().Be("Custom DB error");
            options.InternalServerErrorMessage.Should().Be("Custom server error");
        }
    }
}
