using Dapper;
using JavScraper.Tools.Entities;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class DatabaseManager
{
    private readonly string _connectionString;

    public DatabaseManager(string dbPath)
    {
        _connectionString = $"Data Source={dbPath};";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var createActorsTable = @"
            CREATE TABLE IF NOT EXISTS actors (
                actor_id INTEGER PRIMARY KEY AUTOINCREMENT,
                birth_place TEXT NOT NULL CHECK(length(birth_place) >= 2),
                birth_year INTEGER NOT NULL CHECK(birth_year BETWEEN 1800 AND strftime('%Y', 'now') + 1),
                birth_date TEXT CHECK(birth_date IS NULL OR (length(birth_date) = 10 AND substr(birth_date,5,1) = '-' AND substr(birth_date,8,1) = '-' AND CAST(substr(birth_date,1,4) AS INTEGER) = birth_year AND CAST(substr(birth_date,6,2) AS INTEGER) BETWEEN 1 AND 12 AND CAST(substr(birth_date,9,2) AS INTEGER) BETWEEN 1 AND 31)),
                gender TEXT NOT NULL CHECK(gender IN ('男', '女', '其他')),
                nationality TEXT NOT NULL CHECK(length(nationality) >= 2),
                profile TEXT,
                height INTEGER CHECK(height BETWEEN 50 AND 250),
                weight INTEGER CHECK(weight BETWEEN 10 AND 300)
            );";

        var createActorNamesTable = @"
            CREATE TABLE IF NOT EXISTS actor_names (
                name_id INTEGER PRIMARY KEY AUTOINCREMENT,
                actor_id INTEGER NOT NULL,
                name TEXT NOT NULL CHECK(length(name) >= 1),
                language_code TEXT NOT NULL CHECK(length(language_code) = 2),
                name_type TEXT NOT NULL CHECK(name_type IN ('primary', 'alias', 'former', 'stage')),
                is_primary BOOLEAN NOT NULL DEFAULT 0 CHECK(is_primary IN (0,1)),
                FOREIGN KEY (actor_id) REFERENCES actors(actor_id) ON DELETE CASCADE,
                UNIQUE (actor_id, language_code, is_primary) WHERE is_primary = 1
            );";

        connection.Execute(createActorsTable);
        connection.Execute(createActorNamesTable);
    }

    public async Task InsertVideoInfo(JavVideo video)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync("INSERT INTO videos (Title, Url, Number, Date, Runtime, Maker, Studio, Genres) VALUES (@Title, @Url, @Number, @Date, @Runtime, @Maker, @Studio, @Genres)", video);
    }

    public async Task InsertActorInfo(Actor actor)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync("INSERT INTO actors (birth_place, birth_year, birth_date, gender, nationality, profile, height, weight) VALUES (@BirthPlace, @BirthYear, @BirthDate, @Gender, @Nationality, @Profile, @Height, @Weight)", actor);
    }
} 