using Microsoft.Data.Sqlite;
using ZooManager.Core.Interfaces;

namespace ZooManager.Infrastructure.Persistence.Data
{
    public class DbSchema : IDbSchema
    {
        public void CreateTables(SqliteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Species (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        RequiredClimate TEXT,
                        NeedsWater INTEGER,
                        MinSpacePerAnimal REAL
                    );
                    
                    CREATE TABLE IF NOT EXISTS Enclosures (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        ClimateType TEXT,
                        HasWaterAccess INTEGER,
                        TotalArea REAL,
                        MaxCapacity INTEGER
                    );
                    
                    CREATE TABLE IF NOT EXISTS Animals (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        SpeciesId INTEGER,
                        EnclosureId INTEGER,
                        NextFeedingTime TEXT
                    );
                    
                    CREATE TABLE IF NOT EXISTS AnimalEvents (
                        AnimalId INTEGER,
                        EventDate TEXT,
                        EventType TEXT,
                        Description TEXT,
                        FOREIGN KEY(AnimalId) REFERENCES Animals(Id) ON DELETE CASCADE
                    );
                    
                    CREATE TABLE IF NOT EXISTS Employees (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FirstName TEXT,
                        LastName TEXT
                    );
                    
                    CREATE TABLE IF NOT EXISTS EmployeeQualifications (
                        EmployeeId INTEGER,
                        SpeciesId INTEGER,
                        PRIMARY KEY (EmployeeId, SpeciesId),
                        FOREIGN KEY(EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE,
                        FOREIGN KEY(SpeciesId) REFERENCES Species(Id) ON DELETE CASCADE
                    );
                    
                    CREATE TABLE IF NOT EXISTS ZooEvents (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT,
                        Description TEXT,
                        Start TEXT
                    );
                    
                    CREATE TABLE IF NOT EXISTS SpeciesFieldDefinitions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        SpeciesId INTEGER,
                        FieldName TEXT
                    );
                    
                    CREATE TABLE IF NOT EXISTS AnimalAttributes (
                        AnimalId INTEGER,
                        FieldDefinitionId INTEGER,
                        ValueText TEXT,
                        PRIMARY KEY(AnimalId, FieldDefinitionId)
                    );
                    
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT UNIQUE NOT NULL,
                        PasswordHash TEXT NOT NULL,
                        Role INTEGER NOT NULL,
                        EmployeeId INTEGER,
                        CreatedAt TEXT NOT NULL,
                        IsActive INTEGER DEFAULT 1,
                        FOREIGN KEY(EmployeeId) REFERENCES Employees(Id) ON DELETE SET NULL
                    );
                ";
                cmd.ExecuteNonQuery();
            }
        }
    }
}