using NUnit.Framework;
using Moq;
using MTCG.Services;
using MTCG.Database;
using Npgsql;

namespace MTCG.Tests.ServicesTests
{
    [TestFixture]
    public class LoginServiceTests
    {
        private Mock<Datalayer> _mockDatalayer;
        private LoginService _loginService;

        [SetUp]
        public void SetUp()
        {
            // Initialisiere Mock-Datalayer und LoginService
            _mockDatalayer = new Mock<Datalayer>();
            _loginService = new LoginService(_mockDatalayer.Object);
        }

        [Test]
        public void LoginUser_ShouldReturnError_ForEmptyUsernameOrPassword()
        {
            // Act
            var result = _loginService.LoginUser("", "Password123!");

            // Assert
            Assert.That(result, Is.EqualTo("{\"error\": \"Username and password cannot be empty.\"}"));
        }

        [Test]
        public void LoginUser_ShouldReturnError_ForEmptyPassword()
        {
            // Act
            var result = _loginService.LoginUser("validUsername", "");

            // Assert
            Assert.That(result, Is.EqualTo("{\"error\": \"Username and password cannot be empty.\"}"));
        }

        [Test]
        public void LoginUser_ShouldReturnError_WhenPasswordIsNull()
        {
            // Act
            var result = _loginService.LoginUser("validUsername", null);

            // Assert
            Assert.That(result, Is.EqualTo("{\"error\": \"Username and password cannot be empty.\"}"));
        }

        //[Test]
        //public void LoginUser_ShouldReturnError_WhenUserDoesNotExist()
        //{
        //    // Arrange
        //    var mockConnection = new Mock<NpgsqlConnection>();
        //    var mockCommand = new Mock<NpgsqlCommand>();
        //    var mockReader = new Mock<NpgsqlDataReader>();

        //    mockReader.Setup(r => r.Read()).Returns(false); // Benutzer nicht gefunden
        //    mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(mockReader.Object);
        //    mockConnection.Setup(conn => conn.CreateCommand()).Returns(mockCommand.Object);
        //    _mockDatalayer.Setup(db => db.GetConnection()).Returns(mockConnection.Object);

        //    // Act
        //    var result = _loginService.LoginUser("nonexistentUser", "Password123!");

        //    // Assert
        //    Assert.That(result, Is.EqualTo("{\"error\": \"Invalid username or password.\"}"));
        //}


    }
}
    