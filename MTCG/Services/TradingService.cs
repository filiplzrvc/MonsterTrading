using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using MTCG.Database;
using MTCG.Models;

namespace MTCG.Services
{
    public class TradingService
    {
        private readonly Datalayer _db;

        public TradingService(Datalayer db)
        {
            _db = db;
        }

        public void CreateTradingDeal(TradingDeal deal)
        {
            using (var connection = _db.GetConnection())
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO TradingDeals (id, user_id, card_to_trade, type, minimum_damage) 
                    VALUES (@id, @userId, @cardToTrade, @type, @minimumDamage)", connection))
                {
                    cmd.Parameters.AddWithValue("id", deal.Id);
                    cmd.Parameters.AddWithValue("userId", deal.UserId);
                    cmd.Parameters.AddWithValue("cardToTrade", deal.CardToTrade);
                    cmd.Parameters.AddWithValue("type", deal.Type);
                    cmd.Parameters.AddWithValue("minimumDamage", deal.MinimumDamage);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<TradingDeal> GetTradingDeals()
        {
            var deals = new List<TradingDeal>();
            using (var connection = _db.GetConnection())
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand("SELECT id, user_id, card_to_trade, type, minimum_damage FROM TradingDeals", connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        deals.Add(new TradingDeal
                        {
                            Id = reader.GetGuid(0),
                            UserId = reader.GetInt32(1),
                            CardToTrade = reader.GetGuid(2),
                            Type = reader.GetString(3),
                            MinimumDamage = reader.GetDouble(4)
                        });
                    }
                }
            }
            return deals;
        }

        public void DeleteTradingDeal(Guid dealId, int userId)
        {
            using (var connection = _db.GetConnection())
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(@"
                    DELETE FROM TradingDeals 
                    WHERE id = @id AND user_id = @userId", connection))
                {
                    cmd.Parameters.AddWithValue("id", dealId);
                    cmd.Parameters.AddWithValue("userId", userId);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        throw new InvalidOperationException("Trading deal not found or not authorized to delete.");
                    }
                }
            }
        }

        public bool ExecuteTrade(TradingDeal deal, Guid offeredCardId, int userId)
        {
            using (var connection = _db.GetConnection())
            {
                connection.Open();

                // Überprüfen, ob die Karte gültig ist und den Anforderungen entspricht
                using (var validateCardCmd = new NpgsqlCommand(@"
                    SELECT c.id, c.damage 
                    FROM UserCards uc
                    INNER JOIN Cards c ON uc.card_id = c.id
                    WHERE uc.user_id = @userId AND c.id = @cardId::UUID", connection))
                {
                    validateCardCmd.Parameters.AddWithValue("userId", userId);
                    validateCardCmd.Parameters.AddWithValue("cardId", offeredCardId);

                    using (var reader = validateCardCmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return false; // Karte nicht gefunden
                        }

                        var damage = reader.GetDouble(1);
                        if (damage < deal.MinimumDamage)
                        {
                            return false; // Karte erfüllt nicht die Mindestanforderungen
                        }
                    }
                }

                // Karte des Anbieters und Karte des Käufers tauschen
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Entferne Karte des Anbieters
                        using (var deleteCmd = new NpgsqlCommand(@"
                            DELETE FROM UserCards 
                            WHERE user_id = @dealUserId AND card_id = @dealCardId::UUID", connection))
                        {
                            deleteCmd.Parameters.AddWithValue("dealUserId", deal.UserId);
                            deleteCmd.Parameters.AddWithValue("dealCardId", deal.CardToTrade);
                            deleteCmd.ExecuteNonQuery();
                        }

                        // Fügt Karte des Käufers zum Anbieter hinzu
                        using (var insertCmd = new NpgsqlCommand(@"
                            INSERT INTO UserCards (user_id, card_id) 
                            VALUES (@dealUserId, @offeredCardId::UUID)", connection))
                        {
                            insertCmd.Parameters.AddWithValue("dealUserId", deal.UserId);
                            insertCmd.Parameters.AddWithValue("offeredCardId", offeredCardId);
                            insertCmd.ExecuteNonQuery();
                        }

                        // Fügt Karte des Anbieters zum Käufer hinzu
                        using (var addCmd = new NpgsqlCommand(@"
                            INSERT INTO UserCards (user_id, card_id) 
                            VALUES (@userId, @dealCardId::UUID)", connection))
                        {
                            addCmd.Parameters.AddWithValue("userId", userId);
                            addCmd.Parameters.AddWithValue("dealCardId", deal.CardToTrade);
                            addCmd.ExecuteNonQuery();
                        }

                        // Handel löschen
                        using (var deleteDealCmd = new NpgsqlCommand(@"
                            DELETE FROM TradingDeals WHERE id = @dealId", connection))
                        {
                            deleteDealCmd.Parameters.AddWithValue("dealId", deal.Id);
                            deleteDealCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }


    }
}
