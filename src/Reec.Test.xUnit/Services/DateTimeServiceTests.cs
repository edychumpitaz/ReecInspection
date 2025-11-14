using Reec.Inspection.Options;
using Reec.Inspection.Services;

namespace Reec.Test.xUnit.Services
{
    /// <summary>
    /// Tests para DateTimeService - Manejo de zonas horarias
    /// </summary>
    public class DateTimeServiceTests
    {
        [Fact]
        public void Constructor_WithValidTimeZone_ShouldCreateInstance()
        {
            // Arrange
            var options = new ReecExceptionOptions
            {
                SystemTimeZoneId = "SA Pacific Standard Time"
            };

            // Act
            var service = new DateTimeService(options);

            // Assert
            service.Should().NotBeNull();
            service.TimeZoneInfo.Should().NotBeNull();
            service.TimeZoneInfo.Id.Should().Be("SA Pacific Standard Time");
        }

        [Fact]
        public void Constructor_WithInvalidTimeZone_ShouldThrowException()
        {
            // Arrange
            var options = new ReecExceptionOptions
            {
                SystemTimeZoneId = "Invalid/TimeZone"
            };

            // Act
            Action act = () => new DateTimeService(options);

            // Assert
            act.Should().Throw<TimeZoneNotFoundException>();
        }

        [Theory]
        [InlineData("SA Pacific Standard Time")]
        [InlineData("UTC")]
        [InlineData("Eastern Standard Time")]
        [InlineData("Central European Standard Time")]
        public void Now_WithDifferentTimeZones_ShouldReturnLocalTime(string timeZoneId)
        {
            // Arrange
            var options = new ReecExceptionOptions { SystemTimeZoneId = timeZoneId };
            var service = new DateTimeService(options);

            // Act
            var now = service.Now;
            var utcNow = service.UtcNow;

            // Assert
            now.Should().NotBe(default);
            utcNow.Should().NotBe(default);

            // Verificar que la diferencia entre Now y UtcNow sea razonable (menos de 1 día)
            var difference = Math.Abs((now - utcNow).TotalHours);
            difference.Should().BeLessThan(24);
        }

        [Fact]
        public void UtcNow_ShouldReturnUtcTime()
        {
            // Arrange
            var options = new ReecExceptionOptions
            {
                SystemTimeZoneId = "SA Pacific Standard Time"
            };
            var service = new DateTimeService(options);

            // Act
            var utcNow = service.UtcNow;
            var systemUtcNow = DateTime.UtcNow;

            // Assert
            utcNow.Kind.Should().Be(DateTimeKind.Utc);

            // Verificar que la diferencia con DateTime.UtcNow sea mínima (menos de 1 segundo)
            var difference = Math.Abs((utcNow - systemUtcNow).TotalSeconds);
            difference.Should().BeLessThan(1);
        }

        [Fact]
        public void Now_ShouldBeConsistentWithTimeZoneConversion()
        {
            // Arrange
            var options = new ReecExceptionOptions
            {
                SystemTimeZoneId = "SA Pacific Standard Time"
            };
            var service = new DateTimeService(options);

            // Act
            var now = service.Now;
            var utcNow = service.UtcNow;
            var manualConversion = TimeZoneInfo.ConvertTime(utcNow, service.TimeZoneInfo);

            // Assert
            // La diferencia entre Now y la conversión manual debe ser mínima
            var difference = Math.Abs((now - manualConversion).TotalSeconds);
            difference.Should().BeLessThan(1);
        }

        [Fact]
        public void TimeZoneInfo_ShouldReturnConfiguredTimeZone()
        {
            // Arrange
            var expectedTimeZoneId = "Pacific Standard Time";
            var options = new ReecExceptionOptions
            {
                SystemTimeZoneId = expectedTimeZoneId
            };
            var service = new DateTimeService(options);

            // Act
            var timeZone = service.TimeZoneInfo;

            // Assert
            timeZone.Should().NotBeNull();
            timeZone.Id.Should().Be(expectedTimeZoneId);
        }

        [Fact]
        public void MultipleInstances_ShouldHaveIndependentTimeZones()
        {
            // Arrange
            var options1 = new ReecExceptionOptions { SystemTimeZoneId = "UTC" };
            var options2 = new ReecExceptionOptions { SystemTimeZoneId = "Eastern Standard Time" };

            // Act
            var service1 = new DateTimeService(options1);
            var service2 = new DateTimeService(options2);

            // Assert
            service1.TimeZoneInfo.Id.Should().Be("UTC");
            service2.TimeZoneInfo.Id.Should().Be("Eastern Standard Time");
            service1.TimeZoneInfo.Should().NotBeSameAs(service2.TimeZoneInfo);
        }
    }
}
