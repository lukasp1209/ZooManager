using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;

namespace ZooManager.Infrastructure.Persistence
{
    public class SqlitePersistenceService : IPersistenceService
    {
        private readonly string _connectionString;

        public SqlitePersistenceService(string dbFileName = "zoo.db")
        {
            if (dbFileName.Contains(";")) 
            {
                dbFileName = "zoo.db"; 
            }
            _connectionString = $"Data Source={dbFileName}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
{
    using (var connection = new SqliteConnection(_connectionString))
    {
        connection.Open();
        
        var pragmaCmd = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
        pragmaCmd.ExecuteNonQuery();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Species (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, RequiredClimate TEXT, NeedsWater INTEGER, MinSpacePerAnimal REAL);
            CREATE TABLE IF NOT EXISTS Enclosures (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, ClimateType TEXT, HasWaterAccess INTEGER, TotalArea REAL, MaxCapacity INTEGER);
            CREATE TABLE IF NOT EXISTS Animals (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, SpeciesId INTEGER, EnclosureId INTEGER, NextFeedingTime TEXT);
            CREATE TABLE IF NOT EXISTS AnimalEvents (
                AnimalId INTEGER,
                EventDate TEXT,
                EventType TEXT,
                Description TEXT,
                FOREIGN KEY(AnimalId) REFERENCES Animals(Id) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS Employees (Id INTEGER PRIMARY KEY AUTOINCREMENT, FirstName TEXT, LastName TEXT);
            CREATE TABLE IF NOT EXISTS EmployeeQualifications (
                EmployeeId INTEGER,
                SpeciesId INTEGER,
                PRIMARY KEY (EmployeeId, SpeciesId),
                FOREIGN KEY(EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE,
                FOREIGN KEY(SpeciesId) REFERENCES Species(Id) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS ZooEvents (Id INTEGER PRIMARY KEY AUTOINCREMENT, Title TEXT, Description TEXT, Start TEXT);
            CREATE TABLE IF NOT EXISTS SpeciesFieldDefinitions (Id INTEGER PRIMARY KEY AUTOINCREMENT, SpeciesId INTEGER, FieldName TEXT);
            CREATE TABLE IF NOT EXISTS AnimalAttributes (AnimalId INTEGER, FieldDefinitionId INTEGER, ValueText TEXT, PRIMARY KEY(AnimalId, FieldDefinitionId));
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

        // Testdaten nur einfügen, wenn die Tabellen noch leer sind
        var checkCmd = new SqliteCommand("SELECT COUNT(*) FROM Species", connection);
        long count = (long)checkCmd.ExecuteScalar();

        if (count == 0)
        {
            // Temporarily disable foreign keys for test data insertion
            var disableFkCmd = new SqliteCommand("PRAGMA foreign_keys = OFF;", connection);
            disableFkCmd.ExecuteNonQuery();

            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO Species (Name, RequiredClimate, NeedsWater, MinSpacePerAnimal) VALUES ('Löwe', 'Trocken', 0, 50.0);
                INSERT INTO Species (Name, RequiredClimate, NeedsWater, MinSpacePerAnimal) VALUES ('Pinguin', 'Polar', 1, 10.0);

                INSERT INTO Enclosures (Name, ClimateType, HasWaterAccess, TotalArea, MaxCapacity) VALUES ('Savanne A1', 'Trocken', 1, 500.0, 5);
                INSERT INTO Enclosures (Name, ClimateType, HasWaterAccess, TotalArea, MaxCapacity) VALUES ('Eishalle', 'Polar', 1, 200.0, 20);

                INSERT INTO Animals (Name, SpeciesId, EnclosureId, NextFeedingTime) VALUES ('Simba', 1, 1, datetime('now'));
                INSERT INTO Animals (Name, SpeciesId, EnclosureId, NextFeedingTime) VALUES ('Pingu', 2, 2, datetime('now'));

                INSERT INTO Employees (FirstName, LastName) VALUES ('Max', 'Mustermann');
                INSERT INTO Employees (FirstName, LastName) VALUES ('Anna', 'Schmidt');
                INSERT INTO EmployeeQualifications (EmployeeId, SpeciesId) VALUES (1, 1);
                INSERT INTO EmployeeQualifications (EmployeeId, SpeciesId) VALUES (2, 2);
            ";
            insertCmd.ExecuteNonQuery();
            
            // KORREKTER Hash für "password" mit Salt "ZooManagerSalt"
            string correctPasswordHash = GeneratePasswordHash("password");
            
            var userCmd = connection.CreateCommand();
            userCmd.CommandText = @"
                INSERT INTO Users (Username, PasswordHash, Role, EmployeeId, CreatedAt, IsActive) 
                VALUES (@username1, @hash, 1, NULL, datetime('now'), 1);
                INSERT INTO Users (Username, PasswordHash, Role, EmployeeId, CreatedAt, IsActive) 
                VALUES (@username2, @hash, 2, 1, datetime('now'), 1);
                INSERT INTO Users (Username, PasswordHash, Role, EmployeeId, CreatedAt, IsActive) 
                VALUES (@username3, @hash, 2, 2, datetime('now'), 1);
            ";
            userCmd.Parameters.AddWithValue("@username1", "manager");
            userCmd.Parameters.AddWithValue("@username2", "max.mustermann");
            userCmd.Parameters.AddWithValue("@username3", "anna.schmidt");
            userCmd.Parameters.AddWithValue("@hash", correctPasswordHash);
            userCmd.ExecuteNonQuery();
            
            var enableFkCmd = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
            enableFkCmd.ExecuteNonQuery();
        }
    }
}
        
private string GeneratePasswordHash(string password)
{
    using (var sha256 = System.Security.Cryptography.SHA256.Create())
    {
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "ZooManagerSalt"));
        return Convert.ToBase64String(hashedBytes);
    }
}

        public User? GetUserByUsername(string username)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqliteCommand("SELECT Id, Username, PasswordHash, Role, EmployeeId, CreatedAt, IsActive FROM Users WHERE Username = @username", connection);
                cmd.Parameters.AddWithValue("@username", username);
                
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Id = reader.GetInt32("Id"),
                            Username = reader.GetString("Username"),
                            PasswordHash = reader.GetString("PasswordHash"),
                            Role = (UserRole)reader.GetInt32("Role"),
                            EmployeeId = reader.IsDBNull("EmployeeId") ? null : reader.GetInt32("EmployeeId"),
                            CreatedAt = DateTime.Parse(reader.GetString("CreatedAt")),
                            IsActive = reader.GetInt32("IsActive") == 1
                        };
                    }
                }
            }
            return null;
        }

        public User? GetUserById(int userId)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqliteCommand("SELECT Id, Username, PasswordHash, Role, EmployeeId, CreatedAt, IsActive FROM Users WHERE Id = @id", connection);
                cmd.Parameters.AddWithValue("@id", userId);
                
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Id = reader.GetInt32("Id"),
                            Username = reader.GetString("Username"),
                            PasswordHash = reader.GetString("PasswordHash"),
                            Role = (UserRole)reader.GetInt32("Role"),
                            EmployeeId = reader.IsDBNull("EmployeeId") ? null : reader.GetInt32("EmployeeId"),
                            CreatedAt = DateTime.Parse(reader.GetString("CreatedAt")),
                            IsActive = reader.GetInt32("IsActive") == 1
                        };
                    }
                }
            }
            return null;
        }

        public bool SaveUser(User user)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                SqliteCommand cmd;
                
                if (user.Id <= 0) 
                {
                    cmd = new SqliteCommand(@"
                        INSERT INTO Users (Username, PasswordHash, Role, EmployeeId, CreatedAt, IsActive) 
                        VALUES (@username, @hash, @role, @empId, @created, @active)", connection);
                    cmd.Parameters.AddWithValue("@created", user.CreatedAt.ToString("o"));
                }
                else 
                {
                    cmd = new SqliteCommand(@"
                        UPDATE Users 
                        SET Username = @username, PasswordHash = @hash, Role = @role, EmployeeId = @empId, IsActive = @active 
                        WHERE Id = @id", connection);
                    cmd.Parameters.AddWithValue("@id", user.Id);
                }
                
                cmd.Parameters.AddWithValue("@username", user.Username);
                cmd.Parameters.AddWithValue("@hash", user.PasswordHash);
                cmd.Parameters.AddWithValue("@role", (int)user.Role);
                cmd.Parameters.AddWithValue("@empId", (object)user.EmployeeId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@active", user.IsActive ? 1 : 0);

                try
                {
                    cmd.ExecuteNonQuery();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public IEnumerable<Animal> LoadAnimals()
        {
            var animals = new List<Animal>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqliteCommand(@"
                    SELECT a.*, s.Name AS SpeciesName, e.Name AS EnclosureName 
                    FROM Animals a 
                    LEFT JOIN Species s ON a.SpeciesId = s.Id 
                    LEFT JOIN Enclosures e ON a.EnclosureId = e.Id", connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        animals.Add(new Animal {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            SpeciesId = reader.GetInt32(2),
                            EnclosureId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                            NextFeedingTime = DateTime.Parse(reader.GetString(4)),
                            SpeciesName = reader.IsDBNull(5) ? "Unbekannt" : reader.GetString(5),
                            EnclosureName = reader.IsDBNull(6) ? "Kein Gehege" : reader.GetString(6)
                        });
                    }
                }

                foreach (var animal in animals)
                {
                    var evCmd = new SqliteCommand("SELECT EventDate, EventType, Description FROM AnimalEvents WHERE AnimalId = @id", connection);
                    evCmd.Parameters.AddWithValue("@id", animal.Id);
                    using (var r = evCmd.ExecuteReader())
                    {
                        while (r.Read())
                            animal.Events.Add(new AnimalEvent { Date = DateTime.Parse(r.GetString(0)), Type = r.GetString(1), Description = r.GetString(2) });
                    }
                    
                    var attrCmd = new SqliteCommand(@"
                        SELECT d.FieldName, a.ValueText 
                        FROM AnimalAttributes a 
                        JOIN SpeciesFieldDefinitions d ON a.FieldDefinitionId = d.Id 
                        WHERE a.AnimalId = @id", connection);
                    attrCmd.Parameters.AddWithValue("@id", animal.Id);
                    using (var r = attrCmd.ExecuteReader())
                    {
                        while (r.Read()) animal.Attributes[r.GetString(0)] = r.GetString(1);
                    }
                }
            }
            return animals;
        }
        
        public IEnumerable<Animal> LoadAnimalsForEmployee(int employeeId)
        {
            var animals = new List<Animal>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqliteCommand(@"
                    SELECT DISTINCT a.*, s.Name AS SpeciesName, enc.Name AS EnclosureName 
                    FROM Animals a 
                    LEFT JOIN Species s ON a.SpeciesId = s.Id 
                    LEFT JOIN Enclosures enc ON a.EnclosureId = enc.Id
                    JOIN EmployeeQualifications eq ON s.Id = eq.SpeciesId
                    WHERE eq.EmployeeId = @empId", connection);
                
                cmd.Parameters.AddWithValue("@empId", employeeId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        animals.Add(new Animal {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            SpeciesId = reader.GetInt32(2),
                            EnclosureId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                            NextFeedingTime = DateTime.Parse(reader.GetString(4)),
                            SpeciesName = reader.IsDBNull(5) ? "Unbekannt" : reader.GetString(5),
                            EnclosureName = reader.IsDBNull(6) ? "Kein Gehege" : reader.GetString(6)
                        });
                    }
                }
            }
            return animals;
        }
        
        public void AddAnimalEvent(int animalId, AnimalEvent ev)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                @"INSERT INTO AnimalEvents (AnimalId, EventDate, EventType, Description)
              VALUES ($animalId, $eventDate, $eventType, $description);";

            cmd.Parameters.AddWithValue("$animalId", animalId);

            // Wichtig: TEXT-Spalte -> ISO-String speichern (passt zu DateTime.Parse beim Laden)
            cmd.Parameters.AddWithValue("$eventDate", ev.Date.ToString("o"));

            cmd.Parameters.AddWithValue("$eventType", ev.Type ?? string.Empty);
            cmd.Parameters.AddWithValue("$description", ev.Description ?? string.Empty);

            cmd.ExecuteNonQuery();
        }

        public void SaveAnimals(IEnumerable<Animal> animals)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                foreach (var animal in animals)
                {
                    SqliteCommand cmd;
                    if (animal.Id <= 0) // Neues Tier
                    {
                        cmd = new SqliteCommand(@"
                            INSERT INTO Animals (Name, SpeciesId, EnclosureId, NextFeedingTime) 
                            VALUES (@n, @sid, @eid, @ft)", connection);
                    }
                    else // Bestehendes Tier aktualisieren
                    {
                        cmd = new SqliteCommand(@"
                            INSERT OR REPLACE INTO Animals (Id, Name, SpeciesId, EnclosureId, NextFeedingTime) 
                            VALUES (@id, @n, @sid, @eid, @ft)", connection);
                        cmd.Parameters.AddWithValue("@id", animal.Id);
                    }
                    
                    cmd.Parameters.AddWithValue("@n", animal.Name);
                    cmd.Parameters.AddWithValue("@sid", animal.SpeciesId);
                    cmd.Parameters.AddWithValue("@eid", (object)animal.EnclosureId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ft", animal.NextFeedingTime.ToString("o"));
                    cmd.ExecuteNonQuery();

                    // Neue ID für neue Tiere abrufen
                    if (animal.Id <= 0)
                    {
                        var idCmd = new SqliteCommand("SELECT last_insert_rowid()", connection);
                        animal.Id = Convert.ToInt32(idCmd.ExecuteScalar());
                    }

                    foreach (var attr in animal.Attributes)
                    {
                        var attrCmd = new SqliteCommand(@"
                            INSERT OR REPLACE INTO AnimalAttributes (AnimalId, FieldDefinitionId, ValueText)
                            SELECT @aid, Id, @val FROM SpeciesFieldDefinitions WHERE FieldName = @fname AND SpeciesId = @sid", connection);
                        attrCmd.Parameters.AddWithValue("@aid", animal.Id);
                        attrCmd.Parameters.AddWithValue("@val", attr.Value.ToString());
                        attrCmd.Parameters.AddWithValue("@fname", attr.Key);
                        attrCmd.Parameters.AddWithValue("@sid", animal.SpeciesId);
                        attrCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public IEnumerable<Employee> LoadEmployees()
        {
            var employees = new List<Employee>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqliteCommand("SELECT Id, FirstName, LastName FROM Employees", connection);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        employees.Add(new Employee { Id = reader.GetInt32(0), FirstName = reader.GetString(1), LastName = reader.GetString(2) });
                    }
                }
                foreach (var emp in employees)
                {
                    var qCmd = new SqliteCommand("SELECT SpeciesId FROM EmployeeQualifications WHERE EmployeeId = @id", connection);
                    qCmd.Parameters.AddWithValue("@id", emp.Id);
                    using (var r = qCmd.ExecuteReader())
                    {
                        while (r.Read()) emp.QualifiedSpeciesIds.Add(r.GetInt32(0));
                    }
                }
            }
            return employees;
        }

        public void SaveEmployees(IEnumerable<Employee> employees)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                foreach (var emp in employees)
                {
                    SqliteCommand cmd;
                    if (emp.Id <= 0) // Neuer Mitarbeiter
                    {
                        cmd = new SqliteCommand("INSERT INTO Employees (FirstName, LastName) VALUES (@f, @l)", connection);
                    }
                    else // Bestehender Mitarbeiter aktualisieren
                    {
                        cmd = new SqliteCommand("INSERT OR REPLACE INTO Employees (Id, FirstName, LastName) VALUES (@id, @f, @l)", connection);
                        cmd.Parameters.AddWithValue("@id", emp.Id);
                    }
                    
                    cmd.Parameters.AddWithValue("@f", emp.FirstName);
                    cmd.Parameters.AddWithValue("@l", emp.LastName);
                    cmd.ExecuteNonQuery();

                    // Neue ID für neue Mitarbeiter abrufen
                    if (emp.Id <= 0)
                    {
                        var idCmd = new SqliteCommand("SELECT last_insert_rowid()", connection);
                        emp.Id = Convert.ToInt32(idCmd.ExecuteScalar());
                    }

                    SaveEmployeeQualifications(emp.Id, emp.QualifiedSpeciesIds);
                }
            }
        }

        public void SaveEmployeeQualifications(int employeeId, List<int> speciesIds)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var del = new SqliteCommand("DELETE FROM EmployeeQualifications WHERE EmployeeId = @id", connection);
                del.Parameters.AddWithValue("@id", employeeId);
                del.ExecuteNonQuery();

                foreach (var sid in speciesIds)
                {
                    var ins = new SqliteCommand("INSERT INTO EmployeeQualifications (EmployeeId, SpeciesId) VALUES (@eid, @sid)", connection);
                    ins.Parameters.AddWithValue("@eid", employeeId);
                    ins.Parameters.AddWithValue("@sid", sid);
                    ins.ExecuteNonQuery();
                }
            }
        }

        public IEnumerable<Species> LoadSpecies()
        {
            var list = new List<Species>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqliteCommand("SELECT * FROM Species", connection);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) list.Add(new Species {
                        Id = r.GetInt32(0), Name = r.GetString(1), RequiredClimate = r.IsDBNull(2) ? null : r.GetString(2),
                        NeedsWater = r.GetInt32(3) == 1, MinSpacePerAnimal = r.GetDouble(4)
                    });
                }
            }
            return list;
        }

        public void SaveSpecies(IEnumerable<Species> speciesList)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                foreach (var s in speciesList)
                {
                    SqliteCommand cmd;
                    if (s.Id <= 0) // Neue Art
                    {
                        cmd = new SqliteCommand("INSERT INTO Species (Name, RequiredClimate, NeedsWater, MinSpacePerAnimal) VALUES (@n, @c, @w, @s)", connection);
                    }
                    else // Bestehende Art aktualisieren
                    {
                        cmd = new SqliteCommand("INSERT OR REPLACE INTO Species (Id, Name, RequiredClimate, NeedsWater, MinSpacePerAnimal) VALUES (@id, @n, @c, @w, @s)", connection);
                        cmd.Parameters.AddWithValue("@id", s.Id);
                    }
                    
                    cmd.Parameters.AddWithValue("@n", s.Name);
                    cmd.Parameters.AddWithValue("@c", (object)s.RequiredClimate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@w", s.NeedsWater ? 1 : 0);
                    cmd.Parameters.AddWithValue("@s", s.MinSpacePerAnimal);
                    cmd.ExecuteNonQuery();

                    // Neue ID für neue Arten abrufen
                    if (s.Id <= 0)
                    {
                        var idCmd = new SqliteCommand("SELECT last_insert_rowid()", connection);
                        s.Id = Convert.ToInt32(idCmd.ExecuteScalar());
                    }
                }
            }
        }

        public IEnumerable<Enclosure> LoadEnclosures()
        {
            var list = new List<Enclosure>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqliteCommand("SELECT * FROM Enclosures", connection);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) list.Add(new Enclosure {
                        Id = r.GetInt32(0), Name = r.GetString(1), ClimateType = r.GetString(2),
                        HasWaterAccess = r.GetInt32(3) == 1, TotalArea = r.GetDouble(4), MaxCapacity = r.GetInt32(5)
                    });
                }
            }
            return list;
        }

        public void SaveEnclosures(IEnumerable<Enclosure> enclosures)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                foreach (var enc in enclosures)
                {
                    SqliteCommand cmd;
                    if (enc.Id <= 0) // Neues Gehege
                    {
                        cmd = new SqliteCommand("INSERT INTO Enclosures (Name, ClimateType, HasWaterAccess, TotalArea, MaxCapacity) VALUES (@n, @c, @w, @a, @m)", connection);
                    }
                    else // Bestehendes Gehege aktualisieren
                    {
                        cmd = new SqliteCommand("INSERT OR REPLACE INTO Enclosures (Id, Name, ClimateType, HasWaterAccess, TotalArea, MaxCapacity) VALUES (@id, @n, @c, @w, @a, @m)", connection);
                        cmd.Parameters.AddWithValue("@id", enc.Id);
                    }
                    
                    cmd.Parameters.AddWithValue("@n", enc.Name);
                    cmd.Parameters.AddWithValue("@c", enc.ClimateType);
                    cmd.Parameters.AddWithValue("@w", enc.HasWaterAccess ? 1 : 0);
                    cmd.Parameters.AddWithValue("@a", enc.TotalArea);
                    cmd.Parameters.AddWithValue("@m", enc.MaxCapacity);
                    cmd.ExecuteNonQuery();

                    // Neue ID für neue Gehege abrufen
                    if (enc.Id <= 0)
                    {
                        var idCmd = new SqliteCommand("SELECT last_insert_rowid()", connection);
                        enc.Id = Convert.ToInt32(idCmd.ExecuteScalar());
                    }
                }
            }
        }

        public IEnumerable<ZooEvent> LoadEvents()
        {
            var list = new List<ZooEvent>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqliteCommand("SELECT Title, Description, Start FROM ZooEvents ORDER BY Start ASC", connection);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new ZooEvent 
                        { 
                            Title = r.GetString(r.GetOrdinal("Title")), 
                            Description = r.GetString(r.GetOrdinal("Description")), 
                            Start = DateTime.Parse(r.GetString(r.GetOrdinal("Start"))) 
                        });
                    }
                }
            }
            return list;
        }

        public void SaveEvents(IEnumerable<ZooEvent> events)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                foreach (var ev in events)
                {
                    var cmd = new SqliteCommand("INSERT INTO ZooEvents (Title, Description, Start) VALUES (@t, @d, @s)", connection);
                    cmd.Parameters.AddWithValue("@t", ev.Title);
                    cmd.Parameters.AddWithValue("@d", ev.Description);
                    cmd.Parameters.AddWithValue("@s", ev.Start.ToString("o"));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteAnimal(int animalId)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                // PRAGMA foreign_keys aktivieren für CASCADE DELETE
                var pragmaCmd = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
                pragmaCmd.ExecuteNonQuery();
                
                var cmd = new SqliteCommand("DELETE FROM Animals WHERE Id = @id", connection);
                cmd.Parameters.AddWithValue("@id", animalId);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteEvent(string title, DateTime start)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqliteCommand("DELETE FROM ZooEvents WHERE Title = @t AND Start = @s", connection);
                cmd.Parameters.AddWithValue("@t", title);
                cmd.Parameters.AddWithValue("@s", start.ToString("o"));
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteEmployee(int employeeId)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var pragmaCmd = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
                pragmaCmd.ExecuteNonQuery();
                
                var cmd = new SqliteCommand("DELETE FROM Employees WHERE Id = @id", connection);
                cmd.Parameters.AddWithValue("@id", employeeId);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteSpecies(int speciesId)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var pragmaCmd = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
                pragmaCmd.ExecuteNonQuery();
                
                var cmd = new SqliteCommand("DELETE FROM Species WHERE Id = @id", connection);
                cmd.Parameters.AddWithValue("@id", speciesId);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteEnclosure(int enclosureId)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var pragmaCmd = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
                pragmaCmd.ExecuteNonQuery();
                
                var cmd = new SqliteCommand("DELETE FROM Enclosures WHERE Id = @id", connection);
                cmd.Parameters.AddWithValue("@id", enclosureId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}