using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using MTCG.Models;
using System.Runtime.InteropServices;

namespace MTCG.Services
{
    public class Login
    {
        private Dictionary<string, string> tokenStore = new Dictionary<string, string>();
        private Dictionary<string, User> users;

        public Login(Dictionary<string, User> users)
        {
            this.users = users;
        }

        public string LoginUser(string username, string password)
        {
            // Eingabevalidierung
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return "{\"error\": \"Username and password cannot be empty.\"}";
            }

            // Überprüfen, ob der Benutzername existiert und das Passwort korrekt ist
            if (users.ContainsKey(username) && users[username].Password == password)
            {
                string token = GenerateToken(username);
                tokenStore[username] = token;  // Token für diesen Benutzer speichern
                return "{\"token\": \"" + token + "\"}";
            }

            return "{\"error\": \"Invalid username or password.\"}";
        }
        private static string GenerateToken(string username) 
        {
           return Guid.NewGuid().ToString();
        }
    }
}
