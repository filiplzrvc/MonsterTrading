using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MTCG.Models.Cards.MonsterCards;
using MTCG.Models.Cards.SpellCards;
using MTCG.Database;
using MTCG.Models;
using Npgsql;
using MTCG.Services.Interfaces;

namespace MTCG.Services
{
    public class CardService : ICardService
    {
        private readonly Datalayer _db;

        public CardService(Datalayer db)
        {
            _db = db;
        }



        public List<Card> GetDeck(int userId)
        {
            var deck = new List<Card>();
            using (var connection = _db.GetConnection())
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT c.id, c.name, c.damage, c.element_type, c.type
                    FROM UserCards uc
                    INNER JOIN Cards c ON uc.card_id = c.id
                    WHERE uc.user_id = @userId AND uc.is_in_deck = TRUE", connection))
                {
                    cmd.Parameters.AddWithValue("userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Guid id = reader.GetGuid(0);
                            string name = reader.GetString(1);
                            double damage = reader.GetDouble(2);
                            string elementType = reader.IsDBNull(3) ? "Neutral" : reader.GetString(3); // Standardwert "Neutral"
                            string type = reader.GetString(4);

                            Card card = type switch
                            {
                                "Monster" => new MonsterCard(name, damage, elementType) { Id = id.ToString() },
                                "Spell" => new SpellCard(name, damage, elementType) { Id = id.ToString() },
                                _ => throw new InvalidOperationException("Unknown card type.")
                            };

                            deck.Add(card);
                        }
                    }
                }
            }

            // Validierung: Das Deck muss genau 4 Karten enthalten
            if (deck.Count != 4)
            {
                throw new InvalidOperationException($"User {userId} does not have a fully configured deck of 4 cards.");
            }

            return deck;
        }


        public string ConfigureDeck(int userId, List<Guid> cardIds)
        {
            var validCards = new List<Guid>();
            using (var connection = _db.GetConnection())
            {
                connection.Open();

                foreach (var cardId in cardIds)
                {
                    using (var validateCardCmd = new NpgsqlCommand(@"
                        SELECT 1 
                        FROM UserCards 
                        WHERE user_id = @userId AND card_id = @cardId::UUID", connection))
                    {
                        validateCardCmd.Parameters.AddWithValue("userId", userId);
                        validateCardCmd.Parameters.Add("cardId", NpgsqlTypes.NpgsqlDbType.Uuid).Value = cardId;

                        var isValid = validateCardCmd.ExecuteScalar() != null;
                        if (isValid)
                        {
                            validCards.Add(cardId);
                        }
                    }
                }

                if (validCards.Count != 4)
                {
                    return "Error: Deck must contain exactly 4 valid cards.";
                }

                using (var resetCmd = new NpgsqlCommand(@"
                    UPDATE UserCards SET is_in_deck = FALSE WHERE user_id = @userId", connection))
                {
                    resetCmd.Parameters.AddWithValue("userId", userId);
                    resetCmd.ExecuteNonQuery();
                }

                foreach (var validCardId in validCards)
                {
                    using (var updateCmd = new NpgsqlCommand(@"
                        UPDATE UserCards 
                        SET is_in_deck = TRUE 
                        WHERE user_id = @userId AND card_id = @cardId::UUID", connection))
                    {
                        updateCmd.Parameters.AddWithValue("userId", userId);
                        updateCmd.Parameters.AddWithValue("cardId", validCardId);
                        updateCmd.ExecuteNonQuery();
                    }
                }
            }

            return "Deck configured successfully.";
        }

        // Methode zum Einfügen von Karten
        public string InsertCard(Card card)
        {
            using (var connection = _db.GetConnection())
            {
                connection.Open();

                // Überprüfen, ob die Karte bereits existiert (nach Name)
                using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM Cards WHERE name = @name", connection))
                {
                    checkCmd.Parameters.AddWithValue("name", card.Name);
                    var result = checkCmd.ExecuteScalar();

                    var count = result != null ? (long)result : 0;

                    if (count > 0)
                    {
                        return "There are Duplicate Cards";  // Duplikat gefunden
                    }
                }

                // Wenn die Karte nicht existiert, füge sie hinzu
                using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO Cards (id, name, damage, element_type, type) 
                    VALUES (@id, @name, @damage, @elementType, @type) RETURNING id", connection))
                {
                    string generatedId = Guid.NewGuid().ToString();
                    cmd.Parameters.AddWithValue("id", generatedId);
                    cmd.Parameters.AddWithValue("name", card.Name);
                    cmd.Parameters.AddWithValue("damage", card.Damage);
                    cmd.Parameters.AddWithValue("elementType", card.ElementType ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("type", card.GetCardType());

                    var result = cmd.ExecuteScalar();
                    card.Id = result?.ToString();
                }

                return "Card inserted successfully";  // Erfolgreich eingefügt
            }
        }

        // Methode zum Abrufen aller Karten
        public List<Card> GetAllCards()
        {
            var cards = new List<Card>();

            using (var connection = _db.GetConnection())
            {
                connection.Open();

                using (var cmd = new NpgsqlCommand("SELECT name, damage, element_type, type FROM Cards", connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string cardType = reader.GetString(3);  // Monster oder Spell

                            if (cardType == "Monster")
                            {
                                cards.Add(new MonsterCard(reader.GetString(0), reader.GetDouble(1), reader.GetString(2)));
                            }
                            else if (cardType == "Spell")
                            {
                                cards.Add(new SpellCard(reader.GetString(0), reader.GetDouble(1), reader.GetString(2)));
                            }
                        }
                    }
                }
            }

            return cards;
        }

        public void DeleteAllCards()
        {
            using (var connection = _db.GetConnection())
            {
                connection.Open();

                using (var cmd = new NpgsqlCommand("DELETE FROM Cards", connection))
                {
                    var rowsAffected = cmd.ExecuteNonQuery();
                    Console.WriteLine($"{rowsAffected} Cards have been removed from the database");
                }
            }
        }

        public List<Card> GetCardsByUser(int userId)
        {
            var userCards = new List<Card>();

            using (var connection = _db.GetConnection())
            {
                connection.Open();

                using (var cmd = new NpgsqlCommand(@"
                    SELECT c.name, c.damage, c.element_type, c.type
                    FROM UserCards uc
                    INNER JOIN Cards c ON uc.card_id = c.id
                    WHERE uc.user_id = @userId", connection))
                {
                    cmd.Parameters.AddWithValue("userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name = reader.GetString(0);
                            double damage = reader.GetDouble(1);
                            string? elementType = reader.IsDBNull(2) ? null : reader.GetString(2);
                            string type = reader.GetString(3);

                            Card card = type switch
                            {
                                "Monster" => new MonsterCard(name, damage, elementType),
                                "Spell" => new SpellCard(name, damage, elementType),
                                _ => throw new InvalidOperationException("Unknown card type.")
                            };

                            userCards.Add(card);
                        }
                    }
                }
            }

            return userCards;
        }

    }
}
