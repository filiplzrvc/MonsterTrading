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

namespace MTCG.Services
{
    public class CardService
    {
        private readonly Datalayer _db;

        public CardService(Datalayer db)
        {
            _db = db;
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
                    var count = (long)checkCmd.ExecuteScalar();

                    if (count > 0)
                    {
                        return "There are Duplicate Cards";  // Duplikat gefunden
                    }
                }

                // Wenn die Karte nicht existiert, füge sie hinzu
                using (var cmd = new NpgsqlCommand(@"
            INSERT INTO Cards (name, damage, element_type, type) 
            VALUES (@name, @damage, @elementType, @type)", connection))
                {
                    cmd.Parameters.AddWithValue("name", card.Name);
                    cmd.Parameters.AddWithValue("damage", card.Damage);
                    cmd.Parameters.AddWithValue("elementType", (card is MonsterCard monsterCard) ? monsterCard.ElementType : (card as SpellCard)?.ElementType);
                    cmd.Parameters.AddWithValue("type", card.GetCardType());

                    cmd.ExecuteNonQuery();
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
                    Console.WriteLine($"{rowsAffected} Karten wurden aus der Datenbank entfernt.");
                }
            }
        }
    }
}
