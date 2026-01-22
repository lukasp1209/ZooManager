using System;
using System.Collections.Generic;
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
                var cmd = connection.CreateCommand();
                
                // 1. Tabellen erstellen (wie gehabt)
                cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS Species (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, RequiredClimate TEXT, NeedsWater INTEGER, MinSpacePerAnimal REAL);
        CREATE TABLE IF NOT EXISTS Enclosures (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, ClimateType TEXT, HasWaterAccess INTEGER, TotalArea REAL, MaxCapacity INTEGER);
        CREATE TABLE IF NOT EXISTS Animals (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, SpeciesId INTEGER, EnclosureId INTEGER, NextFeedingTime TEXT);
        CREATE TABLE IF NOT EXISTS AnimalEvents (AnimalId INTEGER, EventDate TEXT, EventType TEXT, Description TEXT);
        CREATE TABLE IF NOT EXISTS Employees (Id INTEGER PRIMARY KEY AUTOINCREMENT, FirstName TEXT, LastName TEXT);
        CREATE TABLE IF NOT EXISTS EmployeeQualifications (EmployeeId INTEGER, SpeciesId INTEGER, PRIMARY KEY (EmployeeId, SpeciesId));
        CREATE TABLE IF NOT EXISTS ZooEvents (Id INTEGER PRIMARY KEY AUTOINCREMENT, Title TEXT, Description TEXT, Start TEXT);
        CREATE TABLE IF NOT EXISTS SpeciesFieldDefinitions (Id INTEGER PRIMARY KEY AUTOINCREMENT, SpeciesId INTEGER, FieldName TEXT);
        CREATE TABLE IF NOT EXISTS AnimalAttributes (AnimalId INTEGER, FieldDefinitionId INTEGER, ValueText TEXT, PRIMARY KEY(AnimalId, FieldDefinitionId));
    ";
                cmd.ExecuteNonQuery();

                // 2. Testdaten nur einfügen, wenn die Tabellen noch leer sind
                var checkCmd = new SqliteCommand("SELECT COUNT(*) FROM Species", connection);
                long count = (long)checkCmd.ExecuteScalar();

                if (count == 0)
                {
                    var insertCmd = connection.CreateCommand();
                    insertCmd.CommandText = @"
                        INSERT INTO Species (Name, RequiredClimate, NeedsWater, MinSpacePerAnimal) VALUES ('Löwe', 'Trocken', 0, 50.0);
                        INSERT INTO Species (Name, RequiredClimate, NeedsWater, MinSpacePerAnimal) VALUES ('Pinguin', 'Polar', 1, 10.0);

                        INSERT INTO Enclosures (Name, ClimateType, HasWaterAccess, TotalArea, MaxCapacity) VALUES ('Savanne A1', 'Trocken', 1, 500.0, 5);
                        INSERT INTO Enclosures (Name, ClimateType, HasWaterAccess, TotalArea, MaxCapacity) VALUES ('Eishalle', 'Polar', 1, 200.0, 20);

                        INSERT INTO Animals (Name, SpeciesId, EnclosureId, NextFeedingTime) VALUES ('Simba', 1, 1, datetime('now'));
                        INSERT INTO Animals (Name, SpeciesId, EnclosureId, NextFeedingTime) VALUES ('Pingu', 2, 2, datetime('now'));

                        INSERT INTO Employees (FirstName, LastName) VALUES ('Max', 'Mustermann');
                        INSERT INTO EmployeeQualifications (EmployeeId, SpeciesId) VALUES (1, 1);
                    ";
                    insertCmd.ExecuteNonQuery();
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
                    // Events laden
                    var evCmd = new SqliteCommand("SELECT EventDate, EventType, Description FROM AnimalEvents WHERE AnimalId = @id", connection);
                    evCmd.Parameters.AddWithValue("@id", animal.Id);
                    using (var r = evCmd.ExecuteReader())
                    {
                        while (r.Read())
                            animal.Events.Add(new AnimalEvent { Date = DateTime.Parse(r.GetString(0)), Type = r.GetString(1), Description = r.GetString(2) });
                    }

                    // Attribute laden
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

        public void SaveAnimals(IEnumerable<Animal> animals)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                foreach (var animal in animals)
                {
                    var cmd = new SqliteCommand(@"
                        INSERT OR REPLACE INTO Animals (Id, Name, SpeciesId, EnclosureId, NextFeedingTime) 
                        VALUES (@id, @n, @sid, @eid, @ft)", connection);
                    cmd.Parameters.AddWithValue("@id", animal.Id);
                    cmd.Parameters.AddWithValue("@n", animal.Name);
                    cmd.Parameters.AddWithValue("@sid", animal.SpeciesId);
                    cmd.Parameters.AddWithValue("@eid", (object)animal.EnclosureId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ft", animal.NextFeedingTime.ToString("o"));
                    cmd.ExecuteNonQuery();

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
                    var cmd = new SqliteCommand("INSERT OR REPLACE INTO Employees (Id, FirstName, LastName) VALUES (@id, @f, @l)", connection);
                    cmd.Parameters.AddWithValue("@id", emp.Id);
                    cmd.Parameters.AddWithValue("@f", emp.FirstName);
                    cmd.Parameters.AddWithValue("@l", emp.LastName);
                    cmd.ExecuteNonQuery();
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
                    var cmd = new SqliteCommand("INSERT OR REPLACE INTO Species (Id, Name, RequiredClimate, NeedsWater, MinSpacePerAnimal) VALUES (@id, @n, @c, @w, @s)", connection);
                    cmd.Parameters.AddWithValue("@id", s.Id);
                    cmd.Parameters.AddWithValue("@n", s.Name);
                    cmd.Parameters.AddWithValue("@c", (object)s.RequiredClimate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@w", s.NeedsWater ? 1 : 0);
                    cmd.Parameters.AddWithValue("@s", s.MinSpacePerAnimal);
                    cmd.ExecuteNonQuery();
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
                    var cmd = new SqliteCommand("INSERT OR REPLACE INTO Enclosures (Id, Name, ClimateType, HasWaterAccess, TotalArea, MaxCapacity) VALUES (@id, @n, @c, @w, @a, @m)", connection);
                    cmd.Parameters.AddWithValue("@id", enc.Id);
                    cmd.Parameters.AddWithValue("@n", enc.Name);
                    cmd.Parameters.AddWithValue("@c", enc.ClimateType);
                    cmd.Parameters.AddWithValue("@w", enc.HasWaterAccess ? 1 : 0);
                    cmd.Parameters.AddWithValue("@a", enc.TotalArea);
                    cmd.Parameters.AddWithValue("@m", enc.MaxCapacity);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public IEnumerable<ZooEvent> LoadEvents()
        {
            var list = new List<ZooEvent>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqliteCommand("SELECT * FROM ZooEvents ORDER BY Start ASC", connection);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) list.Add(new ZooEvent { Title = r.GetString(0), Description = r.GetString(1), Start = DateTime.Parse(r.GetString(2)) });
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

        public void AddAnimalEvent(int animalId, AnimalEvent ev)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqliteCommand("INSERT INTO AnimalEvents (AnimalId, EventDate, EventType, Description) VALUES (@id, @d, @t, @desc)", connection);
                cmd.Parameters.AddWithValue("@id", animalId);
                cmd.Parameters.AddWithValue("@d", ev.Date.ToString("o"));
                cmd.Parameters.AddWithValue("@t", ev.Type);
                cmd.Parameters.AddWithValue("@desc", ev.Description);
                cmd.ExecuteNonQuery();
            }
        }
    }
}