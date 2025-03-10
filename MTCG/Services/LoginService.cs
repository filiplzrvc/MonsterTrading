﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using MTCG.Models;
using System.Runtime.InteropServices;
using Npgsql;
using MTCG.Database;



namespace MTCG.Services
{
    public class LoginService
    {
        private readonly Datalayer _db;

        public LoginService(Datalayer db)
        {
            _db = db;
        }
        

        // Methode zur Benutzeranmeldung
        public string LoginUser(string username, string password)
        {
            // Eingabevalidierung
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return "{\"error\": \"Username and password cannot be empty.\"}";
            }

            // Benutzername und Passwort in der Datenbank überprüfen
            using(var connection = _db.GetConnection())
            {
                connection.Open();

                using(var cmd = new NpgsqlCommand("SELECT id, password, original_username FROM Users WHERE username = @username", connection))
                {
                    cmd.Parameters.AddWithValue("username", username);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if(reader.Read())
                        {
                            string storedHashedPassword = reader.GetString(1);
                            string originalUsername = reader.GetString(2);
                            if (PasswordHashService.VerifyPassword(password, storedHashedPassword))
                            {
                                // Token generieren
                                string token = $"{originalUsername}-mtcgToken";

                                // Token in der Datenbank speichern
                                SaveAuthToken(reader.GetInt32(0), token);

                                return "{\"token\": \"" + token + "\"}";
                            }
                            else
                            {
                                return "{\"error\": \"Invalid username or password.\"}";
                            }
                        }
                        else
                        {
                            return "{\"error\": \"Invalid username or password.\"}";
                        }
                    }
                }
            }
        }

        private void SaveAuthToken(int userId, string token)
        {
            using (var connection = _db.GetConnection())
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand("UPDATE Users SET auth_token = @token WHERE id = @id", connection))
                {
                    cmd.Parameters.AddWithValue("token", token);
                    cmd.Parameters.AddWithValue("id", userId);

                    try
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            Console.WriteLine("Auth token successfully saved for user ID " + userId);
                        }
                        else
                        {
                            Console.WriteLine("Failed to save auth token for user ID " + userId);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error while saving auth token: " + ex.Message);
                    }
                }
            }
        }     
    }
}
