using NUnit.Framework;
using Moq;
using MTCG.Services;
using MTCG.Database;
using MTCG.Models;
using Npgsql;

namespace MTCG.Tests.ServicesTests
{
    [TestFixture]
    public class CardServiceTests
    {
        private CardService _cardService;
        private Mock<Datalayer> _mockDatalayer;

        [SetUp]
        public void SetUp()
        {
            _mockDatalayer = new Mock<Datalayer>();
            _cardService = new CardService(_mockDatalayer.Object);
        }

        [Test]
        public void InsertCard_ShouldSucceed_WithValidCard()
        {
            // Arrange
            var card = new MonsterCard("Fire Goblin", 25.5, "Fire");

            // Act
            var result = _cardService.InsertCard(card);

            // Assert
            Assert.That(result, Is.EqualTo("Card inserted successfully"));
        }

        

        [Test]
        public void InsertCard_ShouldFail_WhenDuplicateCard()
        {
            // Arrange
            var card = new MonsterCard("Duplicate Card", 15, "Neutral");

            var mockConnection = new Mock<NpgsqlConnection>();
            var mockCommand = new Mock<NpgsqlCommand>();

            mockCommand.Setup(cmd => cmd.ExecuteScalar()).Returns(1); // Duplikat vorhanden
            mockConnection.Setup(conn => conn.CreateCommand()).Returns(mockCommand.Object);
            _mockDatalayer.Setup(db => db.GetConnection()).Returns(mockConnection.Object);

            // Act
            var result = _cardService.InsertCard(card);

            // Assert
            Assert.That(result, Is.EqualTo("There are Duplicate Cards"));
        }

        [Test]
        public void DeleteAllCards_ShouldRemoveAllCardsFromDatabase()
        {
            // Arrange
            var mockConnection = new Mock<NpgsqlConnection>();
            var mockCommand = new Mock<NpgsqlCommand>();

            mockCommand.Setup(cmd => cmd.ExecuteNonQuery()).Returns(1); // 1 Karte gelöscht
            mockConnection.Setup(conn => conn.CreateCommand()).Returns(mockCommand.Object);
            _mockDatalayer.Setup(db => db.GetConnection()).Returns(mockConnection.Object);

            // Act
            Assert.DoesNotThrow(() => _cardService.DeleteAllCards());
        }
    }
}
