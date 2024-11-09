using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using MTCG.Models;
using Npgsql;
using MTCG.Database;



namespace MTCG.Services
{
    public class RegisterService
    {
        private readonly Datalayer _db;

        public RegisterService(Datalayer db)
        {
            _db = db;
        }

        public Datalayer DatabaseConnection { get { return _db; } }


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
                    "INSERT INTO Users (username, password) VALUES (@username, @password)", connection))
                {
                    insertCmd.Parameters.AddWithValue("username", username);
                    insertCmd.Parameters.AddWithValue("password", hashedPassword);


                    try
                    {
                        insertCmd.ExecuteNonQuery();
                        return GenerateResponse("message", "User registered successfully!");
                    }
                    catch (Exception ex)
                    {
                        return GenerateResponse("error", "An error occurred while registering the user: " + ex.Message);
                    }
                }

            }
        }

        // Methode, um alle registrierten Benutzer aus der Datenbank abzurufen
        public Dictionary<string, User> GetUsers()
        {
            var users = new Dictionary<string, User>();

            using (var connection = _db.GetConnection())
            {
                connection.Open();

                using (var cmd = new NpgsqlCommand("SELECT username, password, coins, elo, games_played, wins, losses, auth_token FROM Users", connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var user = new User(
                                reader.GetString(0),  // Username
                                reader.GetString(1))  // Password
                            {
                                Coins = reader.GetInt32(2),
                                Elo = reader.GetInt32(3),
                                GamesPlayed = reader.GetInt32(4),
                                Wins = reader.GetInt32(5),
                                Losses = reader.GetInt32(6),
                                AuthToken = reader.IsDBNull(7) ? null : reader.GetString(7)
                            };
                            users.Add(user.Username, user);
                        }
                    }
                }
            }

            return users;
        }

        // Hilfsmethode zum Erstellen der JSON-Antwort
        private string GenerateResponse(string key, string message)
        {
            return $"{{\"{key}\": \"{message}\"}}";
        }
    }
}
