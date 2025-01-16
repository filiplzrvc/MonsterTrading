using System;
using Moq;
using NUnit.Framework;
using Npgsql;
using MTCG.Models;
using MTCG.Services;
using MTCG.Services.Interfaces;
using System.Data;

namespace MTCG.Tests
{
    [TestFixture]
    public class RegisterServiceTests
    {
        private Mock<IDatalayer> _mockDatalayer;
        private RegisterService _registerService;

        [SetUp]
        public void SetUp()
        {
            _mockDatalayer = new Mock<IDatalayer>();
            _registerService = new RegisterService(_mockDatalayer.Object);
        }

        [Test]
        public void RegisterUser_ShouldReturnError_WhenUsernameOrPasswordIsEmpty()
        {
            // Arrange
            var username = "";
            var password = "";

            // Act
            var result = _registerService.RegisterUser(username, password);

            // Assert
            Assert.That(result, Is.EqualTo("{\"error\": \"Username and password cannot be empty.\"}"));

        }
    }
}
