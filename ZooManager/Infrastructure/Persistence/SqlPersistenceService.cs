using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Linq;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;

namespace ZooManager.Infrastructure.Persistence
{
    public class MySqlPersistenceService : IPersistenceService
    {
        private readonly string _connectionString;

        public MySqlPersistenceService(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IEnumerable<T> Load<T>(string tableName) where T : new()
        {
            var result = new List<T>();
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var command = new MySqlCommand($"SELECT * FROM {tableName}", connection);
                using (var reader = command.ExecuteReader())
                {
                    var props = typeof(T).GetProperties();
                    while (reader.Read())
                    {
                        var item = new T();
                        foreach (var prop in props)
                        {
                            try 
                            {
                                int ordinal = reader.GetOrdinal(prop.Name);
                                if (!reader.IsDBNull(ordinal))
                                {
                                    prop.SetValue(item, reader.GetValue(ordinal));
                                }
                            }
                            catch (IndexOutOfRangeException) { /* Eigenschaft existiert nicht in DB */ }
                        }
                        result.Add(item);
                    }
                }
            }
            return result;
        }

        //TODO ... Implementierung von Save analog mit MySqlCommand ...
        
        public IEnumerable<Animal> LoadAnimals()
        {
            var animals = new List<Animal>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                
                // JOIN hinzugefügt, um den Namen der Art zu erhalten
                var cmd = new MySqlCommand(
                    @"
                SELECT a.*, s.Name AS SpeciesName, e.Name AS EnclosureName 
                FROM Animals a 
                LEFT JOIN Species s ON a.SpeciesId = s.Id 
                LEFT JOIN Enclosures e ON a.EnclosureId = e.Id", connection);
                
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var animal = new Animal
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = reader["Name"].ToString(),
                            SpeciesId = Convert.ToInt32(reader["SpeciesId"]),
                            SpeciesName = reader["SpeciesName"] != DBNull.Value ? reader["SpeciesName"].ToString() : "Unbekannte Art",
                            EnclosureId = reader["EnclosureId"] != DBNull.Value ? Convert.ToInt32(reader["EnclosureId"]) : null,
                            EnclosureName = reader["EnclosureName"] != DBNull.Value ? reader["EnclosureName"].ToString() : "Kein Gehege",
                            NextFeedingTime = Convert.ToDateTime(reader["NextFeedingTime"])
                        };
                        animals.Add(animal);
                    }
                }

                foreach (var animal in animals)
                {
                    var eventCmd = new MySqlCommand(
                        "SELECT EventDate, EventType, Description FROM AnimalEvents WHERE AnimalId = @id ORDER BY EventDate DESC", connection);
                    eventCmd.Parameters.AddWithValue("@id", animal.Id);
                    
                    using (var reader = eventCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            animal.Events.Add(new AnimalEvent
                            {
                                Date = reader.GetDateTime("EventDate"),
                                Type = reader.GetString("EventType"),
                                Description = reader.GetString("Description")
                            });
                        }
                    }
                    
                    var attrCmd = new MySqlCommand(
                        @"SELECT d.FieldName, a.ValueText 
                          FROM AnimalAttributes a 
                          JOIN SpeciesFieldDefinitions d ON a.FieldDefinitionId = d.Id 
                          WHERE a.AnimalId = @id", connection);
                    attrCmd.Parameters.AddWithValue("@id", animal.Id);

                    using (var reader = attrCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string key = reader.GetString("FieldName");
                            string val = reader.GetString("ValueText");
                            animal.Attributes[key] = val;
                        }
                    }
                }
            }
            return animals;
        }
        public IEnumerable<Employee> LoadEmployees()
        {
            var employees = new List<Employee>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                
                var empCmd = new MySqlCommand("SELECT Id, FirstName, LastName FROM Employees", connection);
                using (var reader = empCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        employees.Add(new Employee
                        {
                            Id = reader.GetInt32("Id"),
                            FirstName = reader.GetString("FirstName"),
                            LastName = reader.GetString("LastName")
                        });
                    }
                }
                
                foreach (var employee in employees)
                {
                    var qualCmd = new MySqlCommand(
                        "SELECT SpeciesId FROM EmployeeQualifications WHERE EmployeeId = @empId", connection);
                    qualCmd.Parameters.AddWithValue("@empId", employee.Id);

                    using (var reader = qualCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            employee.QualifiedSpeciesIds.Add(reader.GetInt32("SpeciesId"));
                        }
                    }
                }
            }
            return employees;
        }

        public void SaveEmployeeQualifications(int employeeId, List<int> speciesIds)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var deleteCmd = new MySqlCommand(
                        "DELETE FROM EmployeeQualifications WHERE EmployeeId = @empId", connection, transaction);
                    deleteCmd.Parameters.AddWithValue("@empId", employeeId);
                    deleteCmd.ExecuteNonQuery();
                    
                    foreach (var sId in speciesIds)
                    {
                        var insertCmd = new MySqlCommand(
                            "INSERT INTO EmployeeQualifications (EmployeeId, SpeciesId) VALUES (@empId, @sId)", 
                            connection, transaction);
                        insertCmd.Parameters.AddWithValue("@empId", employeeId);
                        insertCmd.Parameters.AddWithValue("@sId", sId);
                        insertCmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
            }
        }
        public IEnumerable<Enclosure> LoadEnclosures()
        {
            var enclosures = new List<Enclosure>();
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new MySqlCommand("SELECT * FROM Enclosures", connection);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        enclosures.Add(new Enclosure
                        {
                            Id = reader.GetInt32("Id"),
                            Name = reader.GetString("Name"),
                            ClimateType = reader.GetString("ClimateType"),
                            HasWaterAccess = reader.GetBoolean("HasWaterAccess"),
                            TotalArea = reader.GetDouble("TotalArea"),
                            MaxCapacity = reader.GetInt32("MaxCapacity")
                        });
                    }
                }
            }
            return enclosures;
        }

        public IEnumerable<Species> LoadSpecies()
        {
            var speciesList = new List<Species>();
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new MySqlCommand("SELECT * FROM Species", connection);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        speciesList.Add(new Species
                        {
                            Id = reader.GetInt32("Id"),
                            Name = reader.GetString("Name"),
                            RequiredClimate = reader.IsDBNull(reader.GetOrdinal("RequiredClimate")) ? null : reader.GetString("RequiredClimate"),
                            NeedsWater = reader.GetBoolean("NeedsWater"),
                            MinSpacePerAnimal = reader.GetDouble("MinSpacePerAnimal")
                        });
                    }
                }
            }
            return speciesList;
        }

        public IEnumerable<ZooEvent> LoadEvents()
        {
            var events = new List<ZooEvent>();
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new MySqlCommand("SELECT * FROM ZooEvents ORDER BY Start ASC", connection);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        events.Add(new ZooEvent
                        {
                            Title = reader.GetString("Title"),
                            Description = reader.GetString("Description"),
                            Start = reader.GetDateTime("Start")
                        });
                    }
                }
            }
            return events;
        }

        public void SaveEvents(IEnumerable<ZooEvent> events)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                foreach (var ev in events)
                {
                    var cmd = new MySqlCommand(
                        @"INSERT INTO ZooEvents (Title, Description, Start) 
                          VALUES (@t, @d, @s)", connection);
                    cmd.Parameters.AddWithValue("@t", ev.Title);
                    cmd.Parameters.AddWithValue("@d", ev.Description);
                    cmd.Parameters.AddWithValue("@s", ev.Start);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SaveEmployees(IEnumerable<Employee> employees)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                foreach (var emp in employees)
                {
                    var cmd = new MySqlCommand(
                        @"INSERT INTO Employees (Id, FirstName, LastName) 
                          VALUES (@id, @fn, @ln) 
                          ON DUPLICATE KEY UPDATE FirstName=@fn, LastName=@ln", connection);
                    cmd.Parameters.AddWithValue("@id", emp.Id);
                    cmd.Parameters.AddWithValue("@fn", emp.FirstName);
                    cmd.Parameters.AddWithValue("@ln", emp.LastName);
                    cmd.ExecuteNonQuery();
                    
                    SaveEmployeeQualifications(emp.Id, emp.QualifiedSpeciesIds);
                }
            }
        }

        public void SaveEnclosures(IEnumerable<Enclosure> enclosures)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                foreach (var enc in enclosures)
                {
                    var cmd = new MySqlCommand(
                        @"INSERT INTO Enclosures (Id, Name, ClimateType, HasWaterAccess, TotalArea, MaxCapacity) 
                          VALUES (@id, @n, @c, @w, @a, @cap) 
                          ON DUPLICATE KEY UPDATE Name=@n, ClimateType=@c, HasWaterAccess=@w, TotalArea=@a, MaxCapacity=@cap", connection);
                    cmd.Parameters.AddWithValue("@id", enc.Id);
                    cmd.Parameters.AddWithValue("@n", enc.Name);
                    cmd.Parameters.AddWithValue("@c", enc.ClimateType);
                    cmd.Parameters.AddWithValue("@w", enc.HasWaterAccess);
                    cmd.Parameters.AddWithValue("@a", enc.TotalArea);
                    cmd.Parameters.AddWithValue("@cap", enc.MaxCapacity);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SaveSpecies(IEnumerable<Species> speciesList)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                foreach (var s in speciesList)
                {
                    var cmd = new MySqlCommand(
                        @"INSERT INTO Species (Id, Name, RequiredClimate, NeedsWater, MinSpacePerAnimal) 
                          VALUES (@id, @n, @c, @w, @s) 
                          ON DUPLICATE KEY UPDATE Name=@n, RequiredClimate=@c, NeedsWater=@w, MinSpacePerAnimal=@s", connection);
                    cmd.Parameters.AddWithValue("@id", s.Id);
                    cmd.Parameters.AddWithValue("@n", s.Name);
                    cmd.Parameters.AddWithValue("@c", s.RequiredClimate);
                    cmd.Parameters.AddWithValue("@w", s.NeedsWater);
                    cmd.Parameters.AddWithValue("@s", s.MinSpacePerAnimal);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void AddAnimalEvent(int animalId, AnimalEvent ev)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new MySqlCommand(
                    "INSERT INTO AnimalEvents (AnimalId, EventDate, EventType, Description) VALUES (@aid, @d, @t, @desc)", connection);
            cmd.Parameters.AddWithValue("@aid", animalId);
                cmd.Parameters.AddWithValue("@d", ev.Date);
                cmd.Parameters.AddWithValue("@t", ev.Type);
                cmd.Parameters.AddWithValue("@desc", ev.Description);
                cmd.ExecuteNonQuery();
            }
        }

        public void SaveAnimals(IEnumerable<Animal> animals)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                foreach (var animal in animals)
                {
                    var cmd = new MySqlCommand(
                            @"INSERT INTO Animals (Id, Name, SpeciesId, EnclosureId, NextFeedingTime) 
                              VALUES (@id, @n, @sid, @eid, @ft) 
                              ON DUPLICATE KEY UPDATE Name=@n, SpeciesId=@sid, EnclosureId=@eid, NextFeedingTime=@ft", connection);
                    cmd.Parameters.AddWithValue("@id", animal.Id);
                    cmd.Parameters.AddWithValue("@n", animal.Name);
                    cmd.Parameters.AddWithValue("@sid", animal.SpeciesId);
                    cmd.Parameters.AddWithValue("@eid", animal.EnclosureId);
                    cmd.Parameters.AddWithValue("@ft", animal.NextFeedingTime);
                    cmd.ExecuteNonQuery();
                    
                    foreach (var attr in animal.Attributes)
                    {
                        var attrCmd = new MySqlCommand(
                            @"INSERT INTO AnimalAttributes (AnimalId, FieldDefinitionId, ValueText) 
                              SELECT @aid, Id, @val FROM SpeciesFieldDefinitions WHERE FieldName = @fname AND SpeciesId = @sid
                              ON DUPLICATE KEY UPDATE ValueText=@val", connection);
                        attrCmd.Parameters.AddWithValue("@aid", animal.Id);
                        attrCmd.Parameters.AddWithValue("@val", attr.Value.ToString());
                        attrCmd.Parameters.AddWithValue("@fname", attr.Key);
                        attrCmd.Parameters.AddWithValue("@sid", animal.SpeciesId);
                        attrCmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}