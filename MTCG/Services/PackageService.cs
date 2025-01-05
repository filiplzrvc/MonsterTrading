using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MTCG.Models;
using MTCG.Database;
using MTCG.Models.Cards.MonsterCards;
using MTCG.Models.Cards.SpellCards;
using Npgsql;
using MTCG.Services.Interfaces;

namespace MTCG.Services
{
    public class PackageService
    {
        private readonly IDatalayer _db;
        private readonly IRegisterService _registerService;

        public PackageService(IDatalayer db, IRegisterService registerService)
        {
            _db = db;
            _registerService = registerService;
        }

        // Speichert ein neues Paket basierend auf den übergebenen Karten
        public string CreateAndSavePackage(string authToken, List<Card> cards)
        {
            // Admin-Check
            var user = _registerService.GetUserByToken(authToken);
            if (user == null || user.Username != "admin")
            {
                return "Error: Only the user 'admin' is allowed to create packages.";
            }

            if (cards == null || cards.Count == 0)
            {
                return "Error: Package must contain at least one card.";
            }

            using (var connection = _db.GetConnection())
            {
                connection.Open();

                // Package ID erstellen
                string packageId = Guid.NewGuid().ToString();

                // Package in der Datenbank speichern
                using (var insertPackageCmd = new NpgsqlCommand(
                    "INSERT INTO Packages (id, package_number) VALUES (@id, DEFAULT)", connection))
                {
                    insertPackageCmd.Parameters.Add("id", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(packageId);
                    insertPackageCmd.ExecuteNonQuery();
                }

                foreach (var card in cards)
                {
                    // Überprüfen, ob die Karte in der Datenbank existiert
                    using (var checkCardCmd = new NpgsqlCommand("SELECT id FROM Cards WHERE id = @id", connection))
                    {
                        checkCardCmd.Parameters.Add("id", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(card.Id);
                        var result = checkCardCmd.ExecuteScalar();

                        if (result == null)
                        {
                            // Karte hinzufügen, falls sie nicht existiert
                            using (var insertCardCmd = new NpgsqlCommand(@"
                                INSERT INTO Cards (id, name, damage, element_type, type) 
                                VALUES (@id, @name, @damage, @elementType, @type)", connection))
                            {
                                insertCardCmd.Parameters.Add("id", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(card.Id);
                                insertCardCmd.Parameters.Add("name", NpgsqlTypes.NpgsqlDbType.Varchar).Value = card.Name;
                                insertCardCmd.Parameters.Add("damage", NpgsqlTypes.NpgsqlDbType.Double).Value = card.Damage;
                                insertCardCmd.Parameters.Add("elementType", NpgsqlTypes.NpgsqlDbType.Varchar).Value = card.ElementType ?? (object)DBNull.Value;
                                insertCardCmd.Parameters.Add("type", NpgsqlTypes.NpgsqlDbType.Varchar).Value = card.GetCardType();
                                insertCardCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // Karte dem Paket hinzufügen
                    using (var insertPackageCardCmd = new NpgsqlCommand(@"
                        INSERT INTO PackageCards (package_id, card_id, damage, element_type, type) 
                        VALUES (@packageId, @cardId, @damage, @elementType, @type)", connection))
                    {
                        insertPackageCardCmd.Parameters.Add("packageId", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(packageId);
                        insertPackageCardCmd.Parameters.Add("cardId", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(card.Id);
                        insertPackageCardCmd.Parameters.Add("damage", NpgsqlTypes.NpgsqlDbType.Double).Value = card.Damage;
                        insertPackageCardCmd.Parameters.Add("elementType", NpgsqlTypes.NpgsqlDbType.Varchar).Value = card.ElementType ?? "Neutral";
                        insertPackageCardCmd.Parameters.Add("type", NpgsqlTypes.NpgsqlDbType.Varchar).Value = card.GetCardType();
                        insertPackageCardCmd.ExecuteNonQuery();
                    }
                }

                return $"Package with ID {packageId} was successfully created.";
            }
        }




        // Pakete aus der Datenbank abrufen
        public List<List<Card>> GetAvailablePackages()
        {
            var packages = new List<List<Card>>();
            using (var connection = _db.GetConnection())
            {
                connection.Open();

                // Paket-IDs abrufen
                var packageIds = new List<int>();
                using (var packageCmd = new NpgsqlCommand("SELECT id FROM Packages ORDER BY package_number", connection))
                using (var packageReader = packageCmd.ExecuteReader())
                {
                    while (packageReader.Read())
                    {
                        packageIds.Add(packageReader.GetInt32(0));
                    }
                }

                // Für jede Paket-ID die zugehörigen Karten abrufen
                foreach (var packageId in packageIds)
                {
                    var package = new List<Card>();

                    using (var cardCmd = new NpgsqlCommand(@"
                        SELECT card_name, damage, element_type, type 
                        FROM PackageCards WHERE package_id = @packageId", connection))
                    {
                        cardCmd.Parameters.AddWithValue("packageId", packageId);
                        using (var cardReader = cardCmd.ExecuteReader())
                        {
                            while (cardReader.Read())
                            {
                                string cardName = cardReader.GetString(0);
                                double damage = cardReader.GetDouble(1);
                                string? elementType = cardReader.IsDBNull(2) ? null : cardReader.GetString(2);
                                string type = cardReader.GetString(3);

                                // Karte erstellen basierend auf Typ
                                Card card = type switch
                                {
                                    "Monster" => new MonsterCard(cardName, damage, elementType),
                                    "Spell" => new SpellCard(cardName, damage, elementType),
                                    _ => throw new InvalidOperationException("Unknown card type")
                                };

                                package.Add(card);
                            }
                        }
                    }

                    packages.Add(package);
                }
            }
            return packages;
        }


        // Methode zum Kauf eines spezifischen Pakets
        public string PurchaseRandomPackage(User user)
        {
            if (user.Coins < 5)
            {
                return "Not enough coins to buy a package.";
            }

            using (var connection = _db.GetConnection())
            {
                connection.Open();

                Guid packageId;
                using (var selectFirstPackageCmd = new NpgsqlCommand(
                    "SELECT id FROM Packages ORDER BY package_number ASC LIMIT 1", connection))
                {
                    var result = selectFirstPackageCmd.ExecuteScalar();
                    if (result == null)
                    {
                        return "No packages available.";
                    }
                    packageId = (Guid)result;
                }

                var packageCards = new List<Card>();
                using (var selectCardsCmd = new NpgsqlCommand(@"
                    SELECT pc.card_id, c.name, pc.damage, pc.element_type, pc.type 
                    FROM PackageCards pc
                    INNER JOIN Cards c ON pc.card_id = c.id
                    WHERE pc.package_id = @packageId", connection))
                {
                    selectCardsCmd.Parameters.AddWithValue("packageId", packageId);
                    using (var reader = selectCardsCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Guid cardId = reader.GetGuid(0);
                            string cardName = reader.GetString(1);
                            double damage = reader.GetDouble(2);
                            string? elementType = reader.IsDBNull(3) ? null : reader.GetString(3);
                            string type = reader.GetString(4);

                            Card card = type switch
                            {
                                "Monster" => new MonsterCard(cardName, damage, elementType) { Id = cardId.ToString() },
                                "Spell" => new SpellCard(cardName, damage, elementType) { Id = cardId.ToString() },
                                _ => throw new InvalidOperationException("Unknown card type.")
                            };

                            packageCards.Add(card);
                        }
                    }
                }

                foreach (var card in packageCards)
                {
                    Guid cardId = Guid.NewGuid();

                    using (var insertCardCmd = new NpgsqlCommand(@"
                        INSERT INTO Cards (id, name, damage, element_type, type) 
                        VALUES (@id, @name, @damage, @elementType, @type)", connection))
                    {
                        insertCardCmd.Parameters.Add("id", NpgsqlTypes.NpgsqlDbType.Uuid).Value = cardId;
                        insertCardCmd.Parameters.Add("name", NpgsqlTypes.NpgsqlDbType.Varchar).Value = card.Name;
                        insertCardCmd.Parameters.Add("damage", NpgsqlTypes.NpgsqlDbType.Double).Value = card.Damage;
                        insertCardCmd.Parameters.Add("elementType", NpgsqlTypes.NpgsqlDbType.Varchar).Value = card.ElementType ?? (object)DBNull.Value;
                        insertCardCmd.Parameters.Add("type", NpgsqlTypes.NpgsqlDbType.Varchar).Value = card.GetCardType();
                        insertCardCmd.ExecuteNonQuery();
                    }

                    using (var insertUserCardCmd = new NpgsqlCommand(@"
                        INSERT INTO UserCards (user_id, card_id, is_in_deck)
                        VALUES (@userId, @cardId, FALSE)", connection))
                    {
                        insertUserCardCmd.Parameters.Add("userId", NpgsqlTypes.NpgsqlDbType.Integer).Value = user.Id;
                        insertUserCardCmd.Parameters.Add("cardId", NpgsqlTypes.NpgsqlDbType.Uuid).Value = Guid.Parse(card.Id);
                        insertUserCardCmd.ExecuteNonQuery();
                    }

                    user.UserStack.AddCard(card);
                }

                user.Coins -= 5;
                using (var updateCoinsCmd = new NpgsqlCommand(
                    "UPDATE Users SET coins = @coins WHERE id = @userId", connection))
                {
                    updateCoinsCmd.Parameters.Add("coins", NpgsqlTypes.NpgsqlDbType.Integer).Value = user.Coins;
                    updateCoinsCmd.Parameters.Add("userId", NpgsqlTypes.NpgsqlDbType.Integer).Value = user.Id;
                    updateCoinsCmd.ExecuteNonQuery();
                }

                using (var deletePackageCmd = new NpgsqlCommand(
                    "DELETE FROM Packages WHERE id = @packageId", connection))
                {
                    deletePackageCmd.Parameters.Add("packageId", NpgsqlTypes.NpgsqlDbType.Uuid).Value = packageId;
                    deletePackageCmd.ExecuteNonQuery();
                }
            }

            return "Package successfully purchased.";
        }



    }
}
