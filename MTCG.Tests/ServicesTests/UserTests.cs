using Moq;
using MTCG.Database;
using MTCG.Models;
using MTCG.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Tests.ServicesTests
{
    [TestFixture]
    public class UserTests
    {
        private User _user;

        [SetUp]
        public void SetUp()
        {
            _user = new User("TestUser", "TestPassword");
        }

        [Test]
        public void AddCardToDeck_ShouldAddCard_WhenDeckHasLessThanFourCards()
        {
            // Arrange
            var card = new MonsterCard("Dragon",50, "Fire");

            // Act
            _user.AddCardToDeck(card);

            // Assert
            Assert.That(_user.Deck.Count, Is.EqualTo(1));
            Assert.That(_user.Deck[0], Is.EqualTo(card));
        }


        [Test]
        public void UpdateElo_ShouldIncreaseEloAndWins_WhenUserWins()
        {
            // Act
            _user.UpdateElo(true);

            // Assert
            Assert.That(_user.Elo, Is.EqualTo(103));
            Assert.That(_user.Wins, Is.EqualTo(3));
        }

        [Test]
        public void UpdateElo_ShouldDecreaseEloAndIncreaseLosses_WhenUserLoses()
        {
            // Act
            _user.UpdateElo(false);

            // Assert
            Assert.That(_user.Elo, Is.EqualTo(95));
            Assert.That(_user.Losses, Is.EqualTo(1));
        }

        [Test]
        public void BuyCardPackage_ShouldAddCardsToUserStack_WhenUserHasEnoughCoins()
        {
            // Arrange
            var package = new List<Card>
        {
            new MonsterCard("Card1",10, "Water"),
            new MonsterCard("Card2", 15, "Fire")
        };

            // Act
            _user.BuyCardPackage(package);

            // Assert
            Assert.That(_user.Coins, Is.EqualTo(15));
            Assert.That(_user.UserStack.Cards.Count, Is.EqualTo(2));
        }

        [Test]
        public void BuyCardPackage_ShouldNotAddCardsToUserStack_WhenUserHasInsufficientCoins()
        {
            // Arrange
            _user.Coins = 4;
            var package = new List<Card>
            {
            new MonsterCard("Card2", 55, "Fire")
            };

            // Act
            _user.BuyCardPackage(package);

            // Assert
            Assert.That(_user.Coins, Is.EqualTo(4));
            Assert.That(_user.UserStack.Cards.Count, Is.EqualTo(0));
        }

        [Test]
        public void User_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var user = new User("SimpleUser", "SimplePassword");

            // Assert
            Assert.That(user.Username, Is.EqualTo("SimpleUser"));
            Assert.That(user.Password, Is.EqualTo("SimplePassword"));
            Assert.That(user.Coins, Is.EqualTo(20));
            Assert.That(user.Elo, Is.EqualTo(100));
            Assert.That(user.GamesPlayed, Is.EqualTo(0));
            Assert.That(user.Wins, Is.EqualTo(0));
            Assert.That(user.Losses, Is.EqualTo(0));
            Assert.That(user.Deck.Count, Is.EqualTo(0));
            Assert.That(user.UserStack.Cards.Count, Is.EqualTo(0));
        }
    }

}
