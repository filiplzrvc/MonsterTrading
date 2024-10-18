using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Npgsql;
using MTCG.Models;

namespace MTCG.Services.HTTP
{
    public class RequestHandler
    {
        private readonly Register registerService;
        private readonly Login loginService;
        private readonly CardService cardService;

        public RequestHandler(Register registerService, Login loginService, CardService cardService)
        {
            this.registerService = registerService;
            this.loginService = loginService;
            this.cardService = cardService;
        }

        public void HandleRequest(string method, string path, string body, StreamWriter writer)
        {
            var segments = path.Split('/');

            switch (method)
            {
                case "POST" when path == "/users":
                    HandleRegister(body, writer);
                    break;
                case "POST" when path == "/sessions":
                    HandleLogin(body, writer);
                    break;
                case "GET" when segments.Length == 3 && segments[1] == "users":
                    HandleGetUser(segments[2], writer);
                    break;
                case "DELETE" when segments.Length == 3 && segments[1] == "users":
                    HandleDeleteUser(segments[2], writer);
                    break;
                case "POST" when path == "/cards":
                    HandleInsertCard(body, writer);
                    break;
                case "GET" when path == "/cards":
                    HandleGetAllCards(writer);
                    break;
                case "DELETE" when path == "/cards":
                    HandleDeleteAllCards(writer);
                    break;
                case "GET" when path == "/":
                    HandleRoot(writer);
                    break;
                default:
                    SendResponse(writer, "HTTP/1.0 404 Not Found", "Error: Path not found", "text/plain");
                    break;
            }
        }

        private void HandleRegister(string body, StreamWriter writer)
        {
            var userData = JsonConvert.DeserializeObject<UserData>(body);

            if (userData == null || string.IsNullOrEmpty(userData.Username) || string.IsNullOrEmpty(userData.Password))
            {
                SendResponse(writer, "HTTP/1.0 400 Bad Request", "{\"error\": \"Invalid user data\"}", "application/json");
                return;
            }

            string responseMessage = registerService.RegisterUser(userData.Username, userData.Password);

            string statusCode = responseMessage.Contains("User registered successfully")
                ? "HTTP/1.0 201 Created"
                : "HTTP/1.0 409 Conflict";

            SendResponse(writer, statusCode, responseMessage, "application/json");
        }

        private void HandleLogin(string body, StreamWriter writer)
        {
            var userData = JsonConvert.DeserializeObject<UserData>(body);

            if (userData == null || string.IsNullOrEmpty(userData.Username) || string.IsNullOrEmpty(userData.Password))
            {
                SendResponse(writer, "HTTP/1.0 400 Bad Request", "{\"error\": \"Invalid user data\"}", "application/json");
                return;
            }

            string token = loginService.LoginUser(userData.Username, userData.Password);
            string statusCode = token.Contains("token")
                ? "HTTP/1.0 200 OK"
                : "HTTP/1.0 401 Unauthorized";

            SendResponse(writer, statusCode, token, "application/json");
        }

        private void HandleRoot(StreamWriter writer)
        {
            string responseBody = "<html><body><h1>Willkommen zum MTCG-Server</h1><p>Der Server funktioniert.</p></body></html>";
            SendResponse(writer, "HTTP/1.0 200 OK", responseBody, "text/html");
        }

        // Neuen Benutzer abrufen
        private void HandleGetUser(string username, StreamWriter writer)
        {
            using (var connection = registerService.DatabaseConnection.GetConnection())
            {
                connection.Open();

                using (var cmd = new NpgsqlCommand("SELECT username, coins, elo, games_played, wins, losses FROM Users WHERE username = @username", connection))
                {
                    cmd.Parameters.AddWithValue("username", username);

                    using(var reader = cmd.ExecuteReader())
                    {
                        if(reader.Read())
                        {
                            var userInfo = new
                            {
                                Username = reader.GetString(0),
                                Coins = reader.GetInt32(1),
                                Elo = reader.GetInt32(2),
                                GamesPlayed = reader.GetInt32(3),
                                Wins = reader.GetInt32(4),
                                Losses = reader.GetInt32(5)
                            };

                            string jsonResponse = JsonConvert.SerializeObject(userInfo);
                            SendResponse(writer, "HTTP/1.1 200 OK", jsonResponse, "application/json");
                        }
                        else
                        {
                            SendResponse(writer, "HTTP/1.1 404 Not Found", "{\"error\":\"User not found\"}", "application/json");
                        }
                    }
                }
            }
        }

        // Benutzer löschen
        private void HandleDeleteUser(string username, StreamWriter writer)
        {
            using (var connection = registerService.DatabaseConnection.GetConnection())
            {
                connection.Open();

                using (var cmd = new NpgsqlCommand("DELETE FROM Users WHERE username = @username", connection))
                {
                    cmd.Parameters.AddWithValue("username", username);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        SendResponse(writer, "HTTP/1.1 200 OK", "{\"message\":\"User deleted successfully\"}", "application/json");
                    }
                    else
                    {
                        SendResponse(writer, "HTTP/1.1 404 Not Found", "{\"error\":\"User not found\"}", "application/json");
                    }
                }
            }
        }

        private void HandleInsertCard(string body, StreamWriter writer)
        {
            var cardDataArray = JsonConvert.DeserializeObject<List<dynamic>>(body); // Ein Array von Karten

            if (cardDataArray == null || cardDataArray.Count == 0)
            {
                SendResponse(writer, "HTTP/1.0 400 Bad Request", "{\"error\":\"Invalid card data\"}", "application/json");
                return;
            }

            foreach (var cardData in cardDataArray)
            {
                if (string.IsNullOrEmpty((string)cardData.Name) || cardData.Damage <= 0)
                {
                    SendResponse(writer, "HTTP/1.0 400 Bad Request", "{\"error\":\"Invalid card data\"}", "application/json");
                    return;
                }

                // Bestimmen, ob es sich um eine MonsterCard oder SpellCard handelt
                string cardType = cardData.Type;
                Card card;

                if (cardType == "Monster")
                {
                    card = new MonsterCard((string)cardData.Name, (double)cardData.Damage, (string)cardData.ElementType);
                }
                else if (cardType == "Spell")
                {
                    card = new SpellCard((string)cardData.Name, (double)cardData.Damage, (string)cardData.ElementType);
                }
                else
                {
                    SendResponse(writer, "HTTP/1.0 400 Bad Request", "{\"error\":\"Invalid card type\"}", "application/json");
                    return;
                }

                // Karte in die Datenbank einfügen und Ergebnis verarbeiten
                string result = cardService.InsertCard(card);

                if (result == "There are Duplicate Cards")
                {
                    SendResponse(writer, "HTTP/1.0 409 Conflict", "{\"error\":\"There are Duplicate Cards\"}", "application/json");
                    return;
                }
            }

            // Wenn alle Karten erfolgreich hinzugefügt wurden
            SendResponse(writer, "HTTP/1.0 201 Created", "{\"message\":\"All cards inserted successfully\"}", "application/json");
        }


        // Funktion, um alle Karten aus der Datenbank abzurufen und zurückzugeben
        private void HandleGetAllCards(StreamWriter writer)
        {
            var cards = cardService.GetAllCards();  // Abrufen aller Karten

            if (cards.Count == 0)
            {
                SendResponse(writer, "HTTP/1.0 404 Not Found", "{\"error\":\"No cards found\"}", "application/json");
            }
            else
            {
                // Erstellen der Liste von Objekten mit der gewünschten Reihenfolge
                var cardsData = cards.Select(card => new
                {
                    Name = card.Name,
                    Damage = card.Damage,
                    ElementType = card.ElementType,
                    Type = card.GetCardType()
                }).ToList();

                string jsonResponse = JsonConvert.SerializeObject(cardsData, Formatting.Indented);
                SendResponse(writer, "HTTP/1.0 200 OK", jsonResponse, "application/json");
            }
        }

        private void HandleDeleteAllCards(StreamWriter writer)
        {
            cardService.DeleteAllCards();  // Alle Karten löschen

            SendResponse(writer, "HTTP/1.0 200 OK", "{\"message\":\"All cards deleted successfully\"}", "application/json");
        }


        private void SendResponse(StreamWriter writer, string statusCode, string responseBody, string contentType)
        {
            writer.WriteLine(statusCode);
            writer.WriteLine($"Content-Type: {contentType}");
            writer.WriteLine($"Content-Length: {responseBody.Length}");
            writer.WriteLine();
            writer.WriteLine(responseBody);
        }
    }

    public class UserData
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
