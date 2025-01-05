using System;
using MTCG.Services.Interfaces;
using Npgsql;

namespace MTCG.Database
{
    public class Datalayer : IDatalayer
    {
        private readonly string _connectionString;

        public Datalayer()
        {
            // Verbindungszeichenfolge - sollte aus Umgebungsvariablen oder einer Konfigurationsdatei geladen werden
            _connectionString = "Host=localhost;Port=5432;Username=admin;Password=admin123;Database=mtcg";
        }

        // Gibt eine neue PostgreSQL-Verbindung zurück
        public virtual NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        // Initialisiert die Datenbank mit allen erforderlichen Tabellen und Sequenzen
        public void InitializeDatabase()
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                // Lösche bestehende Tabellen und Sequenzen, falls vorhanden
                using (var dropCmd = new NpgsqlCommand(@"
                    DROP TABLE IF EXISTS TradingDeals CASCADE;
                    DROP TABLE IF EXISTS UserCards CASCADE;
                    DROP TABLE IF EXISTS Cards CASCADE;
                    DROP TABLE IF EXISTS Packages CASCADE;
                    DROP TABLE IF EXISTS PackageCards CASCADE;
                    DROP TABLE IF EXISTS Users CASCADE;
                    DROP SEQUENCE IF EXISTS package_number_seq;
                ", connection))
                {
                    dropCmd.ExecuteNonQuery();
                }

                // Erstelle Tabellen und Sequenzen
                using (var createCmd = new NpgsqlCommand(@"
                    -- Tabelle für Benutzer
                    CREATE TABLE IF NOT EXISTS Users (
                        id SERIAL PRIMARY KEY,
                        username VARCHAR(255) NOT NULL UNIQUE,
                        original_username VARCHAR(255),
                        password VARCHAR(255) NOT NULL,
                        coins INT DEFAULT 20,
                        elo INT DEFAULT 100,
                        games_played INT DEFAULT 0,
                        wins INT DEFAULT 0,
                        losses INT DEFAULT 0,
                        auth_token VARCHAR(255),
                        bio VARCHAR(255),
                        image VARCHAR(255) 
                    );

                    -- Tabelle für Karten
                    CREATE TABLE IF NOT EXISTS Cards (
                        id UUID PRIMARY KEY,
                        name VARCHAR(255) NOT NULL,
                        damage FLOAT NOT NULL,
                        element_type VARCHAR(50),
                        type VARCHAR(50) NOT NULL  -- Monster oder Spell
                    );

                    -- Verknüpfung von Benutzern und Karten
                    CREATE TABLE IF NOT EXISTS UserCards (
                        id SERIAL PRIMARY KEY,
                        user_id INT REFERENCES Users(id) ON DELETE CASCADE,
                        card_id UUID REFERENCES Cards(id),
                        is_in_deck BOOLEAN DEFAULT FALSE
                    );

                    -- Sequenz für Paketnummern
                    CREATE SEQUENCE IF NOT EXISTS package_number_seq START 1;

                    -- Tabelle für Pakete
                    CREATE TABLE IF NOT EXISTS Packages (
                        id UUID PRIMARY KEY,
                        package_number INT NOT NULL UNIQUE DEFAULT nextval('package_number_seq')
                    );

                    -- Verknüpfung von Paketen und Karten
                    CREATE TABLE IF NOT EXISTS PackageCards (
                        id SERIAL PRIMARY KEY,
                        package_id UUID REFERENCES Packages(id) ON DELETE CASCADE,
                        card_id UUID REFERENCES Cards(id),
                        damage FLOAT NOT NULL,
                        element_type VARCHAR(50) DEFAULT 'Neutral',
                        type VARCHAR(50) NOT NULL  -- Monster oder Spell
                    );

                    -- Tabelle für Handelsangebote
                    CREATE TABLE IF NOT EXISTS TradingDeals (
                        id UUID PRIMARY KEY,
                        user_id INT REFERENCES Users(id) ON DELETE CASCADE,
                        card_to_trade UUID REFERENCES Cards(id) ON DELETE CASCADE,
                        type VARCHAR(50),
                        minimum_damage FLOAT
                    );
                ", connection))
                {
                    createCmd.ExecuteNonQuery();
                }
            }
        }
    }
}
