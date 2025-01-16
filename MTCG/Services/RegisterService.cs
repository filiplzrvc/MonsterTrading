using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using MTCG.Models;
using Npgsql;
using MTCG.Database;
using MTCG.Services.Interfaces;



namespace MTCG.Services
{
    public class RegisterService : IRegisterService
    {
        private readonly IDatalayer _db;

        public RegisterService(IDatalayer db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public IDatalayer DatabaseConnection { get { return _db; } }


        // Methode zur Benutzerregistrierung
        public string RegisterUser(string username, string password)
        {
            // Eingabevalidierung
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return GenerateResponse("error", "Username and password cannot be empty.");
            }

            // Passwort hashen bevor es gespeichert wird
            string hashedPassword = PasswordHashService.HashPassword(password);
            int userId;

            // Überprüfen, ob der Benutzername bereits existiert
            using (var connection = _db.GetConnection())
            {
                connection.Open();

                using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM Users WHERE username = @username", connection))
                {
                    checkCmd.Parameters.AddWithValue("username", username);

                    var result = checkCmd.ExecuteScalar();
                    long userCount = result != null ? (long)result : 0;// Gibt die Anzahl der Benutzer mit diesem Benutzernamen zurück

                    if (userCount > 0)
                    {
                        return GenerateResponse("error", "Username already exists!");
                    }
                }

                // Wenn der Benutzer nicht existiert, Benutzer registrieren
                using (var insertCmd = new NpgsqlCommand(
                    "INSERT INTO Users (username, password, original_username) VALUES (@username, @password, @originalUsername) RETURNING id", connection))
                {
                    insertCmd.Parameters.AddWithValue("username", username);
                    insertCmd.Parameters.AddWithValue("password", hashedPassword);
                    insertCmd.Parameters.AddWithValue("originalUsername", username);

                    var insertResult = insertCmd.ExecuteScalar();
                    userId = insertResult != null ? (int)insertResult : -1;
                }
            }

            var newUser = new User
            {
                Id = userId,
                Username = username,
                Password = hashedPassword,
                Coins = 20,
                Elo = 100,
                UserStack = new Stack(),
                OriginalUsername = username
            };

            return GenerateResponse("message", "User registered successfully!");
        }

        public User GetUserById(int userId)
        {
            using (var connection = new Datalayer().GetConnection())
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT id, username, password, coins, elo, games_played, wins, losses
                    FROM Users
                    WHERE id = @userId;", connection))
                {
                    cmd.Parameters.AddWithValue("userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Password = reader.GetString(2),
                                Coins = reader.GetInt32(3),
                                Elo = reader.GetInt32(4),
                                GamesPlayed = reader.GetInt32(5),
                                Wins = reader.GetInt32(6),
                                Losses = reader.GetInt32(7)
                            };
                        }
                    }
                }
            }

            throw new Exception($"User with ID {userId} not found.");
        }


        public User? GetUserByUsername(string username)
        {
            using (var connection = _db.GetConnection())
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand("SELECT id, username, password, coins, elo, auth_token FROM Users WHERE username = @username", connection))
                {
                    cmd.Parameters.AddWithValue("username", username);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var authToken = reader.IsDBNull(5) ? null : reader.GetString(5);
                            return new User
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Password = reader.GetString(2),
                                Coins = reader.GetInt32(3),
                                Elo = reader.GetInt32(4),
                                AuthToken = authToken
                            };
                        }
                    }
                }
            }
            return null;
        }

        public User? GetUserByToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null; // Kein Benutzer, wenn Token ungültig ist

            using (var connection = _db.GetConnection())
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand("SELECT id, username, password, coins, elo, auth_token, original_username FROM Users WHERE auth_token = @authToken", connection))
                {
                    cmd.Parameters.AddWithValue("authToken", token);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Password = reader.GetString(2),
                                Coins = reader.GetInt32(3),
                                Elo = reader.GetInt32(4),
                                AuthToken = reader.GetString(5),
                                OriginalUsername = reader.GetString(6)
                            };
                        }
                    }
                }
            }
            return null; // Kein Benutzer gefunden
        }




        public User? GetRandomOpponent(User currentUser)
        {
            using (var connection = _db.GetConnection())
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand("SELECT id, username, auth_token FROM Users WHERE id != @currentUserId ORDER BY RANDOM() LIMIT 1", connection))
                {
                    cmd.Parameters.AddWithValue("currentUserId", currentUser.Id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var authToken = reader.IsDBNull(2) ? null : reader.GetString(2);
                            if (!string.IsNullOrEmpty(authToken))
                            {
                                return new User
                                {
                                    Id = reader.GetInt32(0),
                                    Username = reader.GetString(1),
                                    AuthToken = authToken
                                };
                            }
                        }
                    }
                }
            }
            return null;
        }

        // Hilfsmethode zum Erstellen der JSON-Antwort
        private string GenerateResponse(string key, string message)
        {
            return $"{{\"{key}\": \"{message}\"}}";
        }
    }
}
