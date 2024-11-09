using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace MTCG.Services
{
    public class PasswordHashService
    {
        // Methode zum Hashen des Passworts
        public static string HashPassword(string password)
        {
            using (var rfc2898 = new Rfc2898DeriveBytes(password, 16, 10000, HashAlgorithmName.SHA256))
            {
                byte[] salt = rfc2898.Salt;
                byte[] hash = rfc2898.GetBytes(32);

                // Salt und Hash kombinieren, um sie zusammen zu speichern
                byte[] hashBytes = new byte[48];
                Array.Copy(salt, 0, hashBytes, 0, 16);
                Array.Copy(hash, 0, hashBytes, 16, 32);

                return Convert.ToBase64String(hashBytes);
            }
        }

        // Methode zum Überprüfen eines Passworts
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            byte[] hashBytes = Convert.FromBase64String(hashedPassword);
            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);

            using (var rfc2898 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
            {
                byte[] hash = rfc2898.GetBytes(32);
                for (int i = 0; i < 32; i++)
                {
                    if (hashBytes[i + 16] != hash[i])
                    {
                        return false; // Passwort falsch
                    }
                }
                return true; // Passwort korrekt
            }
        }
    }
}
