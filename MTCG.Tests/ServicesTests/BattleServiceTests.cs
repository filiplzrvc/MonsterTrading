using NUnit.Framework;
using Moq;
using MTCG.Services;
using MTCG.Database;
using MTCG.Models;
using MTCG.Services.Interfaces;
using System.Collections.Generic;

namespace MTCG.Tests.ServicesTests
{
    [TestFixture]
    public class BattleServiceTests
    {
        private Mock<ICardService> _cardServiceMock;
        private Mock<IRegisterService> _registerServiceMock;
        private BattleService _battleService;

        [SetUp]
        public void Setup()
        {
            _cardServiceMock = new Mock<ICardService>();
            _registerServiceMock = new Mock<IRegisterService>();
            _battleService = new BattleService(_cardServiceMock.Object, _registerServiceMock.Object);
        }

        [Test]
        public void StartBattle_ShouldReturnError_WhenTokenIsInvalid()
        {
            // Arrange
            string invalidToken1 = null;
            string invalidToken2 = " ";

            // Act
            string result1 = _battleService.StartBattleWithThread(invalidToken1, "validToken");
            string result2 = _battleService.StartBattleWithThread(invalidToken2, "validToken");

            // Assert
            Assert.That(result1, Is.EqualTo("Error: One or both tokens are invalid."));
            Assert.That(result2, Is.EqualTo("Error: One or both tokens are invalid."));
        }

        [Test]
        public void StartBattle_ShouldReturnError_WhenUserIsNotAuthenticated()
        {
            // Arrange
            _registerServiceMock.Setup(r => r.GetUserByToken(It.IsAny<string>())).Returns((User)null);

            // Act
            string result = _battleService.StartBattleWithThread("token1", "token2");

            // Assert
            Assert.That(result, Is.EqualTo("Error: One or both users are not authenticated."));
        }

        [Test]
        public void StartBattle_ShouldReturnError_WhenDeckIsIncomplete()
        {
            // Arrange
            var user1 = new User { Id = 1, Username = "Player1" };
            var user2 = new User { Id = 2, Username = "Player2" };
            _registerServiceMock.Setup(r => r.GetUserByToken("token1")).Returns(user1);
            _registerServiceMock.Setup(r => r.GetUserByToken("token2")).Returns(user2);
            _cardServiceMock.Setup(c => c.GetDeck(user1.Id)).Returns(new List<Card>
            {
                new MonsterCard("Card1", 10, "Fire")
            });
            _cardServiceMock.Setup(c => c.GetDeck(user2.Id)).Returns(new List<Card>
            {
                new MonsterCard("Card1", 10, "Water")
            });

            // Act
            string result = _battleService.StartBattleWithThread("token1", "token2");

            // Assert
            Assert.That(result, Does.Contain("does not have a fully configured deck of 4 cards."));
        }

        [Test]
        public void StartBattle_ShouldReturnError_WhenSameUserBattlesThemselves()
        {
            // Arrange
            var user = new User { Id = 1, Username = "Player1" };
            _registerServiceMock.Setup(r => r.GetUserByToken(It.IsAny<string>())).Returns(user);

            // Act
            string result = _battleService.StartBattleWithThread("token1", "token1");

            // Assert
            Assert.That(result, Is.EqualTo("Error: One or both users are not authenticated."));
        }

    }
}
