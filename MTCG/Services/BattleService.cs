using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MTCG.Models;
using Npgsql;
using MTCG.Services.Interfaces;
using Newtonsoft.Json;
using MTCG.Database;


namespace MTCG.Services
{
    public class BattleService
    {
        private readonly ICardService _cardService;
        private readonly IRegisterService _registerService;
        private static readonly ConcurrentDictionary<int, object> BattleLocks = new ConcurrentDictionary<int, object>();

        public BattleService(ICardService cardService, IRegisterService registerService)
        {
            _cardService = cardService;
            _registerService = registerService;
        }

        public string StartBattleWithThread(string token1, string token2, int timeoutInSeconds = 30)
        {
            if (string.IsNullOrWhiteSpace(token1) || string.IsNullOrWhiteSpace(token2))
            {
                return "Error: One or both tokens are invalid.";
            }

            string result = string.Empty;
            Exception? threadException = null;

            var user1 = _registerService.GetUserByToken(token1);
            var user2 = _registerService.GetUserByToken(token2);

            if (user1 == null || user2 == null || user1.Id == user2.Id)
            {
                return "Error: One or both users are not authenticated.";
            }

            var lock1 = BattleLocks.GetOrAdd(user1.Id, new object());
            var lock2 = BattleLocks.GetOrAdd(user2.Id, new object());

            Thread battleThread = new Thread(() =>
            {
                try
                {
                    lock (lock1)
                    {
                        lock (lock2)
                        {
                            result = StartBattle(token1, token2);
                        }
                    }
                }
                catch (Exception ex)
                {
                    threadException = ex;
                }
                finally
                {
                    BattleLocks.TryRemove(user1.Id, out _);
                    BattleLocks.TryRemove(user2.Id, out _);
                }
            });

            battleThread.Start();

            if (!battleThread.Join(timeoutInSeconds * 1000))
            {
                battleThread.Interrupt();
                return "Error: Battle timed out.";
            }

            return threadException == null ? result : $"Error: {threadException.Message}";
        }

        public string StartBattle(string token1, string token2)
        {
            var user1 = _registerService.GetUserByToken(token1);
            var user2 = _registerService.GetUserByToken(token2);

            if (user1 == null || user2 == null)
                return "Error: One or both users are not authenticated.";

            if (user1.Id == user2.Id)
                return "Error: Users cannot battle themselves.";

            var deck1 = _cardService.GetDeck(user1.Id);
            var deck2 = _cardService.GetDeck(user2.Id);

            Console.WriteLine($"Deck 1: {string.Join(", ", deck1.Select(c => c.Name))}");
            Console.WriteLine($"Deck 2: {string.Join(", ", deck2.Select(c => c.Name))}");

            if (deck1.Count != 4 || deck2.Count != 4)
                return $"Error: {user1.Username} or {user2.Username} does not have a fully configured deck of 4 cards.";

            var battleLog = new List<object>
    {
        new { Message = $"Battle Start: {user1.Username} vs {user2.Username}" }
    };

            SimulateBattle(user1, user2, deck1, deck2, battleLog);

            // Gewinner und Verlierer ermitteln
            var (winner, loser, isDraw) = DetermineWinner(user1, user2, deck1, deck2);

            // Aktualisierte Statistiken abrufen
            var updatedUser1 = _registerService.GetUserById(user1.Id);
            var updatedUser2 = _registerService.GetUserById(user2.Id);

            var finalStats = GetFinalStats(updatedUser1, updatedUser2);

            // Battlelog aktualisieren
            battleLog.Add(new
            {
                Message = "Battle Over!",
                Winner = winner,
                Loser = loser,
                FinalStats = finalStats
            });

            return JsonConvert.SerializeObject(battleLog, Formatting.Indented);
        }

        private void SimulateBattle(User user1, User user2, List<Card> deck1, List<Card> deck2, List<object> battleLog)
        {
            int rounds = 0;

            while (rounds < 100 && deck1.Count > 0 && deck2.Count > 0)
            {
                rounds++;
                var card1 = deck1[new Random().Next(deck1.Count)];
                var card2 = deck2[new Random().Next(deck2.Count)];

                var result = ResolveRound(card1, card2);

                battleLog.Add(new
                {
                    Round = rounds,
                    Player1 = user1.Username,
                    Card1 = new { card1.Name, card1.Damage, card1.ElementType },
                    Player2 = user2.Username,
                    Card2 = new { card2.Name, card2.Damage, card2.ElementType },
                    Result = result > 0 ? $"{user1.Username} wins the round!" :
                    result < 0 ? $"{user2.Username} wins the round!" : "The round is a draw!"
                });

                if (result > 0)
                {
                    UpdateDeckAndStats(user1.Id, user2.Id, deck1, deck2, card2, false);
                }
                else if (result < 0)
                {
                    UpdateDeckAndStats(user2.Id, user1.Id, deck2, deck1, card1, false);
                }
                else
                {
                    UpdatePlayerStatsAfterRound(user1.Id, user2.Id, true);
                }
            }
        }

        private (string winner, string loser, bool isDraw) DetermineWinner(User user1, User user2, List<Card> deck1, List<Card> deck2)
        {
            if (deck1.Count > 0 && deck2.Count == 0)
                return (user1.Username, user2.Username, false);

            if (deck2.Count > 0 && deck1.Count == 0)
                return (user2.Username, user1.Username, false);

            // Decks sind gleich: Statistiken berücksichtigen
            var updatedUser1 = _registerService.GetUserById(user1.Id);
            var updatedUser2 = _registerService.GetUserById(user2.Id);

            if (updatedUser1.Elo != updatedUser2.Elo)
                return updatedUser1.Elo > updatedUser2.Elo
                    ? (updatedUser1.Username, updatedUser2.Username, false)
                    : (updatedUser2.Username, updatedUser1.Username, false);

            if (updatedUser1.Wins != updatedUser2.Wins)
                return updatedUser1.Wins > updatedUser2.Wins
                    ? (updatedUser1.Username, updatedUser2.Username, false)
                    : (updatedUser2.Username, updatedUser1.Username, false);

            return ("No one! It's a draw.", "No one! It's a draw.", true);
        }

        private Dictionary<string, object> GetFinalStats(User updatedUser1, User updatedUser2)
        {
            return new Dictionary<string, object>
            {
                [updatedUser1.Username] = new
                {
                    Wins = updatedUser1.Wins,
                    Losses = updatedUser1.Losses,
                    Elo = updatedUser1.Elo
                },
                [updatedUser2.Username] = new
                {
                    Wins = updatedUser2.Wins,
                    Losses = updatedUser2.Losses,
                    Elo = updatedUser2.Elo
                }
            };
        }

        private void UpdateDeckAndStats(int winnerId, int loserId, List<Card> winnerDeck, List<Card> loserDeck, Card transferredCard, bool isDraw)
        {
            loserDeck.Remove(transferredCard);
            winnerDeck.Add(transferredCard);
            UpdateCardOwnership(loserId, winnerId, Guid.Parse(transferredCard.Id));
            UpdatePlayerStatsAfterRound(winnerId, loserId, isDraw);
        }


        private void UpdatePlayerStatsAfterRound(int user1Id, int user2Id, bool isDraw)
        {
            using (var connection = new Datalayer().GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        if (isDraw)
                        {
                            using (var cmd = new NpgsqlCommand(@"
                                UPDATE Users
                                SET games_played = games_played + 1
                                WHERE id = @userId;", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("userId", user1Id);
                                cmd.ExecuteNonQuery();

                                cmd.Parameters["userId"].Value = user2Id;
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (var cmd = new NpgsqlCommand(@"
                                UPDATE Users
                                SET games_played = games_played + 1, 
                                    wins = wins + 1,
                                    elo = elo + 3
                                WHERE id = @winnerId;", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("winnerId", user1Id);
                                cmd.ExecuteNonQuery();
                            }

                            using (var cmd = new NpgsqlCommand(@"
                                UPDATE Users
                                SET games_played = games_played + 1, 
                                    losses = losses + 1,
                                    elo = elo - 5
                                WHERE id = @loserId;", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("loserId", user2Id);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error updating player stats after round: {ex.Message}");
                        throw;
                    }
                }
            }
        }


        private int ResolveRound(Card card1, Card card2)
        {
            if (card1.Name == "Goblin" && card2.Name == "Dragon") return -1;
            if (card1.Name == "Dragon" && card2.Name == "FireElf") return -1;
            if (card1.Name == "Knight" && card2 is SpellCard && card2.ElementType == "Water") return -1;
            if (card1.Name == "Kraken" && card2 is SpellCard) return 1;
            if (card1.Name == "FireElf" && card2.Name == "Dragon") return 1;

            if (card2.Name == "Goblin" && card1.Name == "Dragon") return 1;
            if (card2.Name == "Dragon" && card1.Name == "FireElf") return 1;
            if (card2.Name == "Knight" && card1 is SpellCard && card1.ElementType == "Water") return 1;
            if (card2.Name == "Kraken" && card1 is SpellCard) return -1;
            if (card2.Name == "FireElf" && card1.Name == "Dragon") return -1;

            if (card1 is SpellCard || card2 is SpellCard)
            {
                return CompareDamageWithEffect(card1, card2);
            }

            return CompareDamage(card1, card2);
        }

        private int CompareDamage(Card card1, Card card2)
        {
            return card1.Damage.CompareTo(card2.Damage);
        }

        private int CompareDamageWithEffect(Card attacker, Card defender)
        {
            var effectiveness = GetEffectiveness(attacker.ElementType, defender.ElementType);
            var attackerDamage = attacker.Damage * effectiveness;
            return attackerDamage.CompareTo(defender.Damage);
        }

        private double GetEffectiveness(string attacker, string defender)
        {
            if (attacker == "Water" && defender == "Fire") return 2.0;
            if (attacker == "Fire" && defender == "Normal") return 2.0;
            if (attacker == "Normal" && defender == "Water") return 2.0;
            if (defender == "Water" && attacker == "Fire") return 0.5;
            if (defender == "Fire" && attacker == "Normal") return 0.5;
            if (defender == "Normal" && attacker == "Water") return 0.5;
            return 1.0;
        }

        private void UpdateCardOwnership(int losingUserId, int winningUserId, Guid cardId)
        {
            using (var connection = _registerService.DatabaseConnection.GetConnection())
            {
                connection.Open();

                using (var cmd = new NpgsqlCommand("DELETE FROM UserCards WHERE user_id = @losingUserId AND card_id = @cardId", connection))
                {
                    cmd.Parameters.AddWithValue("losingUserId", losingUserId);
                    cmd.Parameters.AddWithValue("cardId", cardId);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new NpgsqlCommand("INSERT INTO UserCards (user_id, card_id, is_in_deck) VALUES (@winningUserId, @cardId, FALSE)", connection))
                {
                    cmd.Parameters.AddWithValue("winningUserId", winningUserId);
                    cmd.Parameters.AddWithValue("cardId", cardId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
