using NUnit;
using Moq;
using MTCG.Services;
using MTCG.Database;

namespace MTCG.Tests.ServicesTests
{
    public class PasswordHashServiceTests
    {
        [Test]
        public void HashPassword_ShouldReturnHashedString()
        {
            // Arrange
            string password = "SecurePassword123!";

            // Act
            string hashedPassword = PasswordHashService.HashPassword(password);

            // Assert
            Assert.That(hashedPassword, Is.Not.Empty);
            Assert.That(hashedPassword.Length, Is.GreaterThan(0));
        }

        [Test]
        public void VerifyPassword_ShouldReturnTrue_WithValidPassword()
        {
            // Arrange
            string password = "SecurePassword123!";
            string hashedPassword = PasswordHashService.HashPassword(password);

            // Act
            bool result = PasswordHashService.VerifyPassword(password, hashedPassword);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void VerifyPassword_ShouldReturnFalse_WithInvalidPassword()
        {
            // Arrange
            string password = "SecurePassword123!";
            string hashedPassword = PasswordHashService.HashPassword(password);

            // Act
            bool result = PasswordHashService.VerifyPassword("WrongPassword", hashedPassword);

            // Assert
            Assert.That(result, Is.False);
        }


    }
}
