using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Npgsql;
using MTCG.Models;
using System.Reflection.PortableExecutable;
using MTCG.Services.Interfaces;
using System.Collections.Concurrent;

namespace MTCG.Services.HTTP
{
    public class RequestHandler : IRequestHandler
    {
        private readonly RegisterService registerService;
        private readonly LoginService loginService;
        private readonly CardService cardService;
        private readonly PackageService _packageService;
        private readonly TradingService tradingService;
        private readonly BattleService battleService;
        private static readonly ConcurrentQueue<User> BattleQueue = new ConcurrentQueue<User>();

        public RequestHandler(RegisterService registerService, LoginService loginService, CardService cardService, PackageService packageService, TradingService tradingService, BattleService battleService)
        {
            this.registerService = registerService;
            this.loginService = loginService;
            this.cardService = cardService;
            _packageService = packageService;
            this.battleService = battleService;
            this.tradingService = tradingService;
        }

        public void HandleRequest(string method, string path, string body, StreamWriter writer, Dictionary<string, string> headers)
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
                    HandleGetUser(segments[2], writer, headers);
                    break;
                case "DELETE" when path == "/cards":
                    HandleDeleteAllCards(writer);
                    break;
                case "GET" when path == "/":
                    HandleRoot(writer);
                    break;
                case "POST" when path == "/packages":
                    HandleCreatePackage(writer, headers, body);
                    break;
                case "GET" when path == "/packages":
                    HandleGetPackages(writer);
                    break;
                case "POST" when path == "/transactions/packages":
                    HandlePurchasePackage(writer, headers);
                    break;
                case "GET" when path == "/cards":
                    HandleGetUserCards(writer, headers);
                    break;
                case "GET" when path.StartsWith("/deck"):
                    HandleGetDeck(writer, headers, path);
                    break;
                case "PUT" when path == "/deck":
                    HandleConfigureDeck(writer, headers, body);
                    break;
                case "PUT" when segments.Length == 3 && segments[1] == "users":
                    HandleEditUser(segments[2], body, writer, headers);
                    break;
                case "GET" when path == "/stats":
                    HandleGetStats(writer, headers);
                    break;
                case "GET" when path == "/scoreboard":
                    HandleGetScoreboard(writer);
                    break;
                case "POST" when path == "/tradings":
                    HandleCreateTradingDeal(body, writer, headers);
                    break;
                case "GET" when path == "/tradings":
                    HandleGetTradingDeals(writer);
                    break;
                case "DELETE" when path.StartsWith("/tradings/"):
                    HandleDeleteTradingDeal(path, writer, headers);
                    break;
                case "POST" when path.StartsWith("/tradings/"):
                    HandleTrade(path, writer, body, headers);
                    break;
                case "POST" when path == "/battles":
                    HandleBattle(writer, headers);
                    break;
                default:
                    SendResponse(writer, "HTTP/1.0 404 Not Found", "Error: Path not found", "text/plain");
                    break;
            }
        }


        private void HandleTrade(string path, StreamWriter writer, string body, Dictionary<string, string> headers)
        {
            if (!headers.TryGetValue("Authorization", out var token) || string.IsNullOrWhiteSpace(token))
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Unauthorized: Missing or invalid token\"}", "application/json");
                return;
            }

            if (token.StartsWith("Bearer "))
            {
                token = token.Substring("Bearer ".Length);
            }

            var user = registerService.GetUserByToken(token);
            if (user == null)
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Invalid token\"}", "application/json");
                return;
            }

            // Extrahiere Trading-Deal-ID aus dem Pfad
            var segments = path.Split('/');
            if (segments.Length < 3 || !Guid.TryParse(segments[2], out var tradingDealId))
            {
                SendResponse(writer, "HTTP/1.0 400 Bad Request", "{\"error\":\"Invalid trading deal ID\"}", "application/json");
                return;
            }

            // Prüfen, ob der Trading-Deal existiert
            var deal = tradingService.GetTradingDeals().FirstOrDefault(d => d.Id == tradingDealId);
            if (deal == null)
            {
                SendResponse(writer, "HTTP/1.0 404 Not Found", "{\"error\":\"Trading deal not found\"}", "application/json");
                return;
            }

            // Prüfen, ob der Benutzer versucht, mit sich selbst zu handeln
            if (deal.UserId == user.Id)
            {
                SendResponse(writer, "HTTP/1.0 400 Bad Request", "{\"error\":\"You cannot trade with yourself\"}", "application/json");
                return;
            }

            // JSON-Body prüfen (Karten-ID)
            if (string.IsNullOrWhiteSpace(body) || !Guid.TryParse(body.Trim('"'), out var offeredCardId))
            {
                SendResponse(writer, "HTTP/1.0 400 Bad Request", "{\"error\":\"Invalid card ID\"}", "application/json");
                return;
            }

            try
            {
                // Handel durchführen
                var result = tradingService.ExecuteTrade(deal, offeredCardId, user.Id);
                if (result)
                {
                    SendResponse(writer, "HTTP/1.0 201 Created", "{\"message\":\"Trade completed successfully\"}", "application/json");
                }
                else
                {
                    SendResponse(writer, "HTTP/1.0 400 Bad Request", "{\"error\":\"Trade could not be completed\"}", "application/json");
                }
            }
            catch (Exception ex)
            {
                SendResponse(writer, "HTTP/1.0 500 Internal Server Error", $"{{\"error\":\"{ex.Message}\"}}", "application/json");
            }
        }


        private void HandleBattle(StreamWriter writer, Dictionary<string, string> headers)
        {
            if (!headers.TryGetValue("Authorization", out var token) || string.IsNullOrWhiteSpace(token))
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Missing or invalid token\"}", "application/json");
                return;
            }

            token = token.StartsWith("Bearer ") ? token.Substring("Bearer ".Length) : token;

            var user = registerService.GetUserByToken(token);
            if (user == null)
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Invalid token\"}", "application/json");
                return;
            }

            // Spieler in die Battle-Queue einreihen oder ein Battle starten
            var response = battleService.QueueBattle(token);

            SendResponse(writer, "HTTP/1.0 200 OK", $"{{\"message\":\"{response}\"}}", "application/json");
        }



        private void HandleGetTradingDeals(StreamWriter writer)
        {
            try
            {
                var deals = tradingService.GetTradingDeals();

                // Wenn keine Deals vorhanden sind, leere Liste zurückgeben
                if (deals.Count == 0)
                {
                    SendResponse(writer, "HTTP/1.0 200 OK", "[]", "application/json");
                    return;
                }

                // Andernfalls die vorhandenen Deals zurückgeben
                var response = JsonConvert.SerializeObject(deals, Formatting.Indented);
                SendResponse(writer, "HTTP/1.0 200 OK", response, "application/json");
            }
            catch (Exception ex)
            {
                // Fehlerhafte interne Verarbeitung melden
                SendResponse(writer, "HTTP/1.0 500 Internal Server Error", $"{{\"error\":\"{ex.Message}\"}}", "application/json");
            }
        }


        private void HandleCreateTradingDeal(string body, StreamWriter writer, Dictionary<string, string> headers)
        {
            if (!headers.TryGetValue("Authorization", out var token) || string.IsNullOrWhiteSpace(token))
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Unauthorized: Missing or invalid token\"}", "application/json");
                return;
            }

            // Entfernen des "Bearer "-Präfixes
            if (token.StartsWith("Bearer "))
            {
                token = token.Substring("Bearer ".Length);
            }

            // Benutzer anhand des Tokens abrufen
            var user = registerService.GetUserByToken(token);
            if (user == null)
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Invalid token\"}", "application/json");
                return;
            }

            // Deserialisieren des TradingDeals aus dem Body
            TradingDeal deal;
            try
            {
                deal = JsonConvert.DeserializeObject<TradingDeal>(body);
                if (deal == null)
                {
                    throw new ArgumentException("Trading deal data is null.");
                }
            }
            catch (Exception ex)
            {
                SendResponse(writer, "HTTP/1.0 400 Bad Request", $"{{\"error\":\"Invalid trading deal data: {ex.Message}\"}}", "application/json");
                return;
            }

            // Validierung der Daten
            if (deal.Id == Guid.Empty) // Wenn keine ID angegeben, neue generieren
            {
                deal.Id = Guid.NewGuid();
            }

            if (deal.UserId != 0) // Überschreiben von UserId verhindern
            {
                SendResponse(writer, "HTTP/1.0 400 Bad Request", "{\"error\":\"UserId should not be provided in the request.\"}", "application/json");
                return;
            }

            if (string.IsNullOrWhiteSpace(deal.CardToTrade.ToString()) || deal.MinimumDamage <= 0)
            {
                SendResponse(writer, "HTTP/1.0 400 Bad Request", "{\"error\":\"Invalid card or minimum damage value.\"}", "application/json");
                return;
            }

            deal.UserId = user.Id;

            // TradingDeal erstellen
            try
            {
                tradingService.CreateTradingDeal(deal);
                SendResponse(writer, "HTTP/1.0 201 Created", "{\"message\":\"Trading deal created successfully.\"}", "application/json");
            }
            catch (Exception ex)
            {
                SendResponse(writer, "HTTP/1.0 500 Internal Server Error", $"{{\"error\":\"{ex.Message}\"}}", "application/json");
            }
        }


        private void HandleDeleteTradingDeal(string path, StreamWriter writer, Dictionary<string, string> headers)
        {
            if (!headers.TryGetValue("Authorization", out var token) || string.IsNullOrWhiteSpace(token))
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Unauthorized: Missing or invalid token\"}", "application/json");
                return;
            }

            if (token.StartsWith("Bearer "))
            {
                token = token.Substring("Bearer ".Length);
            }

            var user = registerService.GetUserByToken(token);
            if (user == null)
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Invalid token\"}", "application/json");
                return;
            }

            try
            {
                var dealId = Guid.Parse(path.Split('/').Last());
                tradingService.DeleteTradingDeal(dealId, user.Id);
                SendResponse(writer, "HTTP/1.0 200 OK", "{\"message\":\"Trading deal deleted successfully.\"}", "application/json");
            }
            catch (FormatException)
            {
                SendResponse(writer, "HTTP/1.0 400 Bad Request", "{\"error\":\"Invalid trading deal ID format.\"}", "application/json");
            }
            catch (Exception ex)
            {
                SendResponse(writer, "HTTP/1.0 500 Internal Server Error", $"{{\"error\":\"{ex.Message}\"}}", "application/json");
            }
        }


        private void HandleGetScoreboard(StreamWriter writer)
        {
            using (var connection = registerService.DatabaseConnection.GetConnection())
            {
                connection.Open();

                using (var cmd = new NpgsqlCommand(@"
                        SELECT username, elo, wins, losses, games_played
                        FROM Users
                        ORDER BY elo DESC, wins DESC, losses ASC
                    ", connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        var scoreboard = new List<object>();

                        while (reader.Read())
                        {
                            scoreboard.Add(new
                            {
                                Username = reader.GetString(0),
                                Elo = reader.GetInt32(1),
                                Wins = reader.GetInt32(2),
                                Losses = reader.GetInt32(3),
                                GamesPlayed = reader.GetInt32(4)
                            });
                        }

                        string jsonResponse = JsonConvert.SerializeObject(scoreboard, Formatting.Indented);
                        SendResponse(writer, "HTTP/1.0 200 OK", jsonResponse, "application/json");
                    }
                }
            }
        }


        private void HandleGetStats(StreamWriter writer, Dictionary<string, string> headers)
        {
            if (!headers.TryGetValue("Authorization", out var token) || string.IsNullOrWhiteSpace(token))
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Unauthorized: Missing or invalid token\"}", "application/json");
                return;
            }

            if (token.StartsWith("Bearer "))
            {
                token = token.Substring("Bearer ".Length);
            }

            var user = registerService.GetUserByToken(token);
            if (user == null)
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Invalid token\"}", "application/json");
                return;
            }

            // Benutzerstatistiken direkt aus der Datenbank holen
            using (var connection = registerService.DatabaseConnection.GetConnection())
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT games_played, wins, losses, elo 
                    FROM Users 
                    WHERE id = @userId", connection))
                {
                    cmd.Parameters.AddWithValue("userId", user.Id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var stats = new
                            {
                                Username = user.Username,
                                GamesPlayed = reader.GetInt32(0),
                                Wins = reader.GetInt32(1),
                                Losses = reader.GetInt32(2),
                                Elo = reader.GetInt32(3)
                            };

                            string jsonResponse = JsonConvert.SerializeObject(stats, Formatting.Indented);
                            SendResponse(writer, "HTTP/1.0 200 OK", jsonResponse, "application/json");
                        }
                        else
                        {
                            SendResponse(writer, "HTTP/1.0 404 Not Found", "{\"error\":\"User stats not found\"}", "application/json");
                        }
                    }
                }
            }
        }


        private void HandleEditUser(string username, string body, StreamWriter writer, Dictionary<string, string> headers)
        {
            // Überprüfen, ob ein Authentifizierungstoken vorhanden ist
            if (!headers.TryGetValue("Authorization", out var token) || string.IsNullOrWhiteSpace(token))
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Unauthorized: Missing or invalid token\"}", "application/json");
                return;
            }

            // Bearer-Token entfernen
            if (token.StartsWith("Bearer "))
            {
                token = token.Substring("Bearer ".Length);
            }

            // Benutzer anhand des Tokens abrufen
            var user = registerService.GetUserByToken(token);
            if (user == null || user.OriginalUsername != username)
            {
                SendResponse(writer, "HTTP/1.0 403 Forbidden", "{\"error\":\"Token and username do not match\"}", "application/json");
                return;
            }

            // Eingabedaten validieren
            var updatedData = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
            if (updatedData == null || !updatedData.ContainsKey("Name") || !updatedData.ContainsKey("Bio") || !updatedData.ContainsKey("Image"))
            {
                SendResponse(writer, "HTTP/1.0 400 Bad Request", "{\"error\":\"Invalid input data\"}", "application/json");
                return;
            }

            // Benutzername und andere Daten aktualisieren
            using (var connection = registerService.DatabaseConnection.GetConnection())
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(@"
                        UPDATE Users 
                        SET username = @name, bio = @bio, image = @image 
                        WHERE original_username = @originalUsername", connection))
                {
                    cmd.Parameters.AddWithValue("name", updatedData["Name"]);
                    cmd.Parameters.AddWithValue("bio", updatedData["Bio"]);
                    cmd.Parameters.AddWithValue("image", updatedData["Image"]);
                    cmd.Parameters.AddWithValue("originalUsername", username);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        SendResponse(writer, "HTTP/1.0 200 OK", "{\"message\":\"User data updated successfully\"}", "application/json");
                    }
                    else
                    {
                        SendResponse(writer, "HTTP/1.0 500 Internal Server Error", "{\"error\":\"Failed to update user data\"}", "application/json");
                    }
                }
            }
        }


        private void HandleGetDeck(StreamWriter writer, Dictionary<string, string> headers, string path)
        {
            if (!headers.TryGetValue("Authorization", out var token) || string.IsNullOrWhiteSpace(token))
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Unauthorized: Missing or invalid token\"}", "application/json");
                return;
            }

            if (token.StartsWith("Bearer "))
            {
                token = token.Substring("Bearer ".Length);
            }

            var user = registerService.GetUserByToken(token);
            if (user == null)
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Invalid token\"}", "application/json");
                return;
            }

            var deck = cardService.GetDeck(user.Id);

            if (deck.Count == 0)
            {
                SendResponse(writer, "HTTP/1.0 200 OK", "[]", "application/json");
                return;
            }

            // Query-Parameter verarbeiten
            var format = "json"; // Standardformat
            var queryIndex = path.IndexOf("?");
            if (queryIndex != -1)
            {
                var query = path.Substring(queryIndex + 1);
                var queryParams = query.Split('&');
                foreach (var param in queryParams)
                {
                    var keyValue = param.Split('=');
                    if (keyValue[0] == "format" && keyValue.Length == 2)
                    {
                        format = keyValue[1];
                        break;
                    }
                }
            }

            if (format == "plain")
            {
                // Plain-Text-Ausgabe
                var plainText = string.Join("\n", deck.Select(card => $"{card.Name} ({card.ElementType}) - {card.Damage} Damage"));
                SendResponse(writer, "HTTP/1.0 200 OK", plainText, "text/plain");
            }
            else
            {
                // JSON-Ausgabe
                var deckData = deck.Select(card => new
                {
                    Name = card.Name,
                    Damage = card.Damage,
                    ElementType = card.ElementType,
                    Type = card.GetCardType()
                });

                string jsonResponse = JsonConvert.SerializeObject(deckData, Formatting.Indented);
                SendResponse(writer, "HTTP/1.0 200 OK", jsonResponse, "application/json");
            }
        }



        private void HandleConfigureDeck(StreamWriter writer, Dictionary<string, string> headers, string body)
        {
            if (!headers.TryGetValue("Authorization", out var token) || string.IsNullOrWhiteSpace(token))
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Unauthorized: Missing or invalid token\"}", "application/json");
                return;
            }

            if (token.StartsWith("Bearer "))
            {
                token = token.Substring("Bearer ".Length);
            }

            var user = registerService.GetUserByToken(token);
            if (user == null)
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Invalid token\"}", "application/json");
                return;
            }

            var cardIds = JsonConvert.DeserializeObject<List<string>>(body);
            try
            {
                var parsedCardIds = cardIds.Select(Guid.Parse).ToList();
                var result = cardService.ConfigureDeck(user.Id, parsedCardIds);
                if (result.StartsWith("Error"))
                {
                    SendResponse(writer, "HTTP/1.0 400 Bad Request", $"{{\"error\":\"{result}\"}}", "application/json");
                }
                else
                {
                    SendResponse(writer, "HTTP/1.0 200 OK", "{\"message\":\"Deck configured successfully.\"}", "application/json");
                }
            }
            catch (FormatException)
            {
                SendResponse(writer, "HTTP/1.0 400 Bad Request", "{\"error\":\"Invalid UUID format in card IDs.\"}", "application/json");
            }
        }



        private void HandleGetUserCards(StreamWriter writer, Dictionary<string, string> headers)
        {
            if (!headers.TryGetValue("Authorization", out var token) || string.IsNullOrWhiteSpace(token))
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Unauthorized: Missing or invalid token\"}", "application/json");
                return;
            }

            if (token.StartsWith("Bearer "))
            {
                token = token.Substring("Bearer ".Length);
            }

            // Benutzer anhand des Tokens abrufen
            var user = registerService.GetUserByToken(token);
            if (user == null)
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Invalid token\"}", "application/json");
                return;
            }

            // Karten des Benutzers abrufen
            var cards = cardService.GetCardsByUser(user.Id);

            if (cards.Count == 0)
            {
                SendResponse(writer, "HTTP/1.0 404 Not Found", "{\"error\":\"No cards found for user.\"}", "application/json");
            }
            else
            {
                var cardsData = cards.Select(card => new
                {
                    Name = card.Name,
                    Damage = card.Damage,
                    ElementType = card.ElementType,
                    Type = card.GetCardType()
                });

                string jsonResponse = JsonConvert.SerializeObject(cardsData, Formatting.Indented);
                SendResponse(writer, "HTTP/1.0 200 OK", jsonResponse, "application/json");
            }
        }


        // Methode zur Verarbeitung des Erstellens von Paketen
        private void HandleCreatePackage(StreamWriter writer, Dictionary<string, string> headers, string body)
        {
            if (!headers.TryGetValue("Authorization", out var token))
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Missing authorization token\"}", "application/json");
                return;
            }

            if (token.StartsWith("Bearer "))
            {
                token = token.Substring("Bearer ".Length);
            }

            // Überprüfung des Tokens und Abrufen des Benutzers
            var user = registerService.GetUserByToken(token);
            if (user == null || user.Username != "admin")
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Only the user 'admin' can create packages\"}", "application/json");
                return;
            }

            List<Card> cards;
            try
            {
                cards = DeserializeCards(body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing cards: {ex.Message}");
                SendResponse(writer, "HTTP/1.0 400 Bad Request", "{\"error\":\"Invalid card data\"}", "application/json");
                return;
            }

            try
            {
                var responseMessage = _packageService.CreateAndSavePackage(token, cards);
                if (responseMessage.StartsWith("Error"))
                {
                    SendResponse(writer, "HTTP/1.0 400 Bad Request", $"{{\"error\":\"{responseMessage}\"}}", "application/json");
                }
                else
                {
                    SendResponse(writer, "HTTP/1.0 201 Created", $"{{\"message\":\"{responseMessage}\"}}", "application/json");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating package: {ex.Message}");
                SendResponse(writer, "HTTP/1.0 500 Internal Server Error", "{\"error\":\"Internal server error.\"}", "application/json");
            }
        }

        // Methode für die Deserialisierung von Karten
        private List<Card> DeserializeCards(string body)
        {
            var rawCards = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(body);
            var cards = new List<Card>();


            foreach (var rawCard in rawCards)
            {
                string id = rawCard["Id"].ToString();
                string name = rawCard["Name"].ToString();
                double damage = Convert.ToDouble(rawCard["Damage"]);

                // Standard-Typ basierend auf dem Namen
                string type = name.ToLower().Contains("spell") ? "Spell" : "Monster";

                // Überprüfe, ob die ID gültig ist
                if (!Guid.TryParse(id, out var parsedId))
                {
                    throw new InvalidOperationException($"Invalid UUID format for card ID: {id}");
                }

                // Karte erstellen
                Card card = type switch
                {
                    "Monster" => new MonsterCard(name, damage, null) { Id = id }, // Null für ElementType
                    "Spell" => new SpellCard(name, damage, null) { Id = id },     // Null für ElementType
                    _ => throw new InvalidOperationException($"Unknown card type: {type}")
                };

                cards.Add(card);
            }

            return cards;
        }


        // Pakete abrufen und anzeigen
        private void HandleGetPackages(StreamWriter writer)
        {
            var packages = _packageService.GetAvailablePackages();
            var packagesData = packages.Select((package, index) => new
            {
                PackageNumber = index + 1,
                Cards = package.Select(card => new
                {
                    card.Name,
                    card.Damage,
                    card.ElementType,
                    Type = card.GetCardType()
                })
            }).ToList();

            string jsonResponse = JsonConvert.SerializeObject(packagesData, Formatting.Indented);
            SendResponse(writer, "HTTP/1.0 200 OK", jsonResponse, "application/json");
        }

        private void HandlePurchasePackage(StreamWriter writer, Dictionary<string, string> headers)
        {
            if (!headers.TryGetValue("Authorization", out var token) || string.IsNullOrEmpty(token))
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Unauthorized\"}", "application/json");
                return;
            }

            if (token.StartsWith("Bearer "))
            {
                token = token.Substring("Bearer ".Length);
            }

            // Benutzer anhand des Tokens abrufen
            var user = registerService.GetUserByToken(token);
            if (user == null)
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Invalid token\"}", "application/json");
                return;
            }

            // Zufälliges Paket kaufen
            var purchaseResult = _packageService.PurchaseRandomPackage(user);
            if (purchaseResult.Contains("successfully"))
            {
                SendResponse(writer, "HTTP/1.0 201 Created", $"{{\"message\":\"{purchaseResult}\"}}", "application/json");
            }
            else
            {
                SendResponse(writer, "HTTP/1.0 400 Bad Request", $"{{\"error\":\"{purchaseResult}\"}}", "application/json");
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
        private void HandleGetUser(string username, StreamWriter writer, Dictionary<string, string> headers)
        {
            // Überprüfen, ob ein Authentifizierungstoken vorhanden ist
            if (!headers.TryGetValue("Authorization", out var token) || string.IsNullOrWhiteSpace(token))
            {
                SendResponse(writer, "HTTP/1.0 401 Unauthorized", "{\"error\":\"Unauthorized: Missing or invalid token\"}", "application/json");
                return;
            }

            // Bearer-Token entfernen
            if (token.StartsWith("Bearer "))
            {
                token = token.Substring("Bearer ".Length);
            }

            // Benutzer anhand des Tokens abrufen
            var user = registerService.GetUserByToken(token);

            Console.WriteLine($"Requested Username: {username}");
            Console.WriteLine($"Token Username: {user?.OriginalUsername}");

            if (user == null || user.OriginalUsername != username)
            {
                SendResponse(writer, "HTTP/1.0 403 Forbidden", "{\"error\":\"Token and username do not match\"}", "application/json");
                return;
            }

            // Benutzer aus der Datenbank abrufen
            using (var connection = registerService.DatabaseConnection.GetConnection())
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(@"
                        SELECT username, coins, elo, games_played, wins, losses, bio, image 
                        FROM Users 
                        WHERE original_username = @originalUsername", connection))
                {
                    cmd.Parameters.AddWithValue("originalUsername", username);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var userInfo = new
                            {
                                Username = reader.GetString(0),
                                Coins = reader.GetInt32(1),
                                Elo = reader.GetInt32(2),
                                GamesPlayed = reader.GetInt32(3),
                                Wins = reader.GetInt32(4),
                                Losses = reader.GetInt32(5),
                                Bio = reader.IsDBNull(6) ? null : reader.GetString(6),
                                Image = reader.IsDBNull(7) ? null : reader.GetString(7)
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
