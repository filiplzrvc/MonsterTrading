using NUnit.Framework;
using Moq;
using MTCG.Services;
using MTCG.Database;
using MTCG.Models;
using MTCG.Models.Cards.MonsterCards;
using MTCG.Models.Cards.SpellCards;
using System.Collections.Generic;
using MTCG.Services.Interfaces;
using Npgsql;

namespace MTCG.Tests.ServicesTests
{
    [TestFixture]
    public class PackageServiceTests
    {
        private Mock<IDatalayer> _dbMock;
        private Mock<IRegisterService> _registerServiceMock;
        private PackageService _packageService;

        [SetUp]
        public void Setup()
        {
            _dbMock = new Mock<IDatalayer>();
            _registerServiceMock = new Mock<IRegisterService>();
            _packageService = new PackageService(_dbMock.Object, _registerServiceMock.Object);
        }

        [Test]
        public void CreateAndSavePackage_ShouldFail_WhenNotAdmin()
        {
            // Arrange
            var user = new User { Id = 1, Username = "TestUser" };
            _registerServiceMock.Setup(r => r.GetUserByToken("invalidToken")).Returns(user);

            var cards = new List<Card>
            {
                new MonsterCard("Goblin", 10, "Fire"),
                new SpellCard("Fireball", 15, "Fire")
            };

            // Act
            string result = _packageService.CreateAndSavePackage("invalidToken", cards);

            // Assert
            Assert.That(result, Is.EqualTo("Error: Only the user 'admin' is allowed to create packages."));
        }

        [Test]
        public void CreateAndSavePackage_ShouldFail_WhenPackageIsEmpty()
        {
            // Arrange
            var user = new User { Id = 1, Username = "admin" };
            _registerServiceMock.Setup(r => r.GetUserByToken("adminToken")).Returns(user);

            // Act
            string result = _packageService.CreateAndSavePackage("adminToken", null);

            // Assert
            Assert.That(result, Is.EqualTo("Error: Package must contain at least one card."));
        }


        [Test]
        public void PurchaseRandomPackage_ShouldFail_WhenNotEnoughCoins()
        {
            // Arrange
            var user = new User { Id = 1, Username = "TestUser", Coins = 4 };

            // Act
            string result = _packageService.PurchaseRandomPackage(user);

            // Assert
            Assert.That(result, Is.EqualTo("Not enough coins to buy a package."));
        }
    }
}
