using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using MTCG.Models;


namespace MTCG.Services
{
    public class Register
    {
        private readonly Dictionary<string, User> registeredUsers = new Dictionary<string, User>();

        // Methode zur Benutzerregistrierung
        public string RegisterUser(string username, string password)
        {
            // Eingabevalidierung
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return GenerateResponse("error", "Username and password cannot be empty.");
            }

            // Überprüfen, ob der Benutzername bereits existiert
            if (!registeredUsers.ContainsKey(username))
            {
                registeredUsers[username] = new User(username, password);
                return GenerateResponse("message", "User registered successfully!");
            }

            return GenerateResponse("error", "Username already exists!");
        }

        // Methode, um alle registrierten Benutzer abzurufen
        public Dictionary<string, User> GetUsers()
        {
            return registeredUsers;
        }

        // Hilfsmethode zum Erstellen der JSON-Antwort
        private string GenerateResponse(string key, string message)
        {
            return $"{{\"{key}\": \"{message}\"}}";
        }
    }
}
