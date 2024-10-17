using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace MTCG.Services
{
    public class Database
    {
        private readonly string _connectionString;

        public Database()
        {
            _connectionString = "Host=localhost;Port=5432;Username=admin;Password=admin123;Database=mtcg";

        }

        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public void InitializeDatabase()
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(@"
                    CREATE TABLE IF NOT EXISTS Users (
                    id SERIAL PRIMARY KEY,
                    username VARCHAR(255) NOT NULL UNIQUE,
                    password VARCHAR(255) NOT NULL,
                    coins INT DEFAULT 20,
                    elo INT DEFAULT 100,
                    games_played INT DEFAULT 0,
                    wins INT DEFAULT 0,
                    losses INT DEFAULT 0,
                    auth_token VARCHAR(255)
                );

                CREATE TABLE IF NOT EXISTS Cards (
                    id SERIAL PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    damage FLOAT NOT NULL,
                    element_type VARCHAR(50),
                    type VARCHAR(50)
                );

                CREATE TABLE IF NOT EXISTS UserCards (
                    id SERIAL PRIMARY KEY,
                    user_id INT REFERENCES Users(id),
                    card_id INT REFERENCES Cards(id),
                    is_in_deck BOOLEAN DEFAULT FALSE
                );
               ", connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
