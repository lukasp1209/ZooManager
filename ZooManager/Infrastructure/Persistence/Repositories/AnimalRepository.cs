using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence.Connection;
using ZooManager.Infrastructure.Persistence.Extensions;

namespace ZooManager.Infrastructure.Persistence.Repositories
{
    internal class AnimalRepository
    {
        private readonly DatabaseConnectionManager _connectionManager;

        public AnimalRepository(DatabaseConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public List<Animal> GetAll()
        {
            return _connectionManager.ExecuteWithConnection(connection =>
            {
                var animals = LoadAnimalsBasicInfo(connection);
                foreach (var animal in animals)
                {
                    LoadAnimalEvents(connection, animal);
                    LoadAnimalAttributes(connection, animal);
                }
                return animals;
            });
        }

        public List<Animal> GetForEmployee(int employeeId)
        {
            return _connectionManager.ExecuteWithConnection(connection =>
            {
                var animals = new List<Animal>();
                using (var command = new SqlCommandBuilder(connection)
                    .WithCommandText(@"
                        SELECT DISTINCT a.*, s.Name AS SpeciesName, enc.Name AS EnclosureName 
                        FROM Animals a 
                        LEFT JOIN Species s ON a.SpeciesId = s.Id 
                        LEFT JOIN Enclosures enc ON a.EnclosureId = enc.Id
                        JOIN EmployeeQualifications eq ON s.Id = eq.SpeciesId
                        WHERE eq.EmployeeId = @empId")
                    .AddParameter("@empId", employeeId)
                    .Build())
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            animals.Add(MapFromReader(reader));
                        }
                    }
                }
                return animals;
            });
        }

        public void Save(IEnumerable<Animal> animals)
        {
            _connectionManager.ExecuteWithConnection(connection =>
            {
                foreach (var animal in animals)
                {
                    SaveSingle(connection, animal);
                    SaveAttributes(connection, animal);
                }
            });
        }

        public void Delete(int animalId)
        {
            _connectionManager.ExecuteWithConnection(connection =>
            {
                EnableForeignKeys(connection);
                new SqlCommandBuilder(connection)
                    .WithCommandText("DELETE FROM Animals WHERE Id = @id")
                    .AddParameter("@id", animalId)
                    .ExecuteNonQuery();
            });
        }

        public void AddEvent(int animalId, AnimalEvent ev)
        {
            _connectionManager.ExecuteWithConnection(connection =>
            {
                new SqlCommandBuilder(connection)
                    .WithCommandText(@"
                        INSERT INTO AnimalEvents (AnimalId, EventDate, EventType, Description)
                        VALUES (@animalId, @eventDate, @eventType, @description)")
                    .AddParameter("@animalId", animalId)
                    .AddParameter("@eventDate", ev.Date.ToString("o"))
                    .AddParameter("@eventType", ev.Type ?? string.Empty)
                    .AddParameter("@description", ev.Description ?? string.Empty)
                    .ExecuteNonQuery();
            });
        }

        public void DeleteEvent(int animalId, AnimalEvent ev)
        {
            _connectionManager.ExecuteWithConnection(connection =>
            {
                new SqlCommandBuilder(connection)
                    .WithCommandText(@"
                        DELETE FROM AnimalEvents
                        WHERE AnimalId = @animalId AND EventDate = @eventDate 
                          AND EventType = @eventType AND Description = @description")
                    .AddParameter("@animalId", animalId)
                    .AddParameter("@eventDate", ev.Date.ToString("o"))
                    .AddParameter("@eventType", ev.Type ?? string.Empty)
                    .AddParameter("@description", ev.Description ?? string.Empty)
                    .ExecuteNonQuery();
            });
        }

        public void UpdateEvent(int animalId, AnimalEvent oldEvent, AnimalEvent newEvent)
        {
            _connectionManager.ExecuteWithConnection(connection =>
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        new SqlCommandBuilder(connection)
                            .WithCommandText(@"
                                DELETE FROM AnimalEvents
                                WHERE AnimalId = @animalId AND EventDate = @eventDate 
                                  AND EventType = @eventType AND Description = @description")
                            .AddParameter("@animalId", animalId)
                            .AddParameter("@eventDate", oldEvent.Date.ToString("o"))
                            .AddParameter("@eventType", oldEvent.Type ?? string.Empty)
                            .AddParameter("@description", oldEvent.Description ?? string.Empty)
                            .ExecuteNonQuery();

                        new SqlCommandBuilder(connection)
                            .WithCommandText(@"
                                INSERT INTO AnimalEvents (AnimalId, EventDate, EventType, Description)
                                VALUES (@animalId, @eventDate, @eventType, @description)")
                            .AddParameter("@animalId", animalId)
                            .AddParameter("@eventDate", newEvent.Date.ToString("o"))
                            .AddParameter("@eventType", newEvent.Type ?? string.Empty)
                            .AddParameter("@description", newEvent.Description ?? string.Empty)
                            .ExecuteNonQuery();

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            });
        }

        private List<Animal> LoadAnimalsBasicInfo(SqliteConnection connection)
        {
            var animals = new List<Animal>();
            using (var command = new SqlCommandBuilder(connection)
                .WithCommandText(@"
                    SELECT a.*, s.Name AS SpeciesName, e.Name AS EnclosureName 
                    FROM Animals a 
                    LEFT JOIN Species s ON a.SpeciesId = s.Id 
                    LEFT JOIN Enclosures e ON a.EnclosureId = e.Id")
                .Build())
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        animals.Add(MapFromReader(reader));
                    }
                }
            }
            return animals;
        }

        private void LoadAnimalEvents(SqliteConnection connection, Animal animal)
        {
            using (var command = new SqlCommandBuilder(connection)
                .WithCommandText("SELECT EventDate, EventType, Description FROM AnimalEvents WHERE AnimalId = @id")
                .AddParameter("@id", animal.Id)
                .Build())
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        animal.Events.Add(new AnimalEvent
                        {
                            Date = DateTime.Parse(reader.GetString(0)),
                            Type = reader.GetString(1),
                            Description = reader.GetString(2)
                        });
                    }
                }
            }
        }

        private void LoadAnimalAttributes(SqliteConnection connection, Animal animal)
        {
            using (var command = new SqlCommandBuilder(connection)
                .WithCommandText(@"
                    SELECT d.FieldName, a.ValueText 
                    FROM AnimalAttributes a 
                    JOIN SpeciesFieldDefinitions d ON a.FieldDefinitionId = d.Id 
                    WHERE a.AnimalId = @id")
                .AddParameter("@id", animal.Id)
                .Build())
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        animal.Attributes[reader.GetString(0)] = reader.GetString(1);
                    }
                }
            }
        }

        private void SaveSingle(SqliteConnection connection, Animal animal)
        {
            var builder = new SqlCommandBuilder(connection);

            if (animal.Id <= 0)
            {
                builder.WithCommandText(@"
                    INSERT INTO Animals (Name, SpeciesId, EnclosureId, NextFeedingTime) 
                    VALUES (@n, @sid, @eid, @ft)");
            }
            else
            {
                builder.WithCommandText(@"
                    INSERT OR REPLACE INTO Animals (Id, Name, SpeciesId, EnclosureId, NextFeedingTime) 
                    VALUES (@id, @n, @sid, @eid, @ft)")
                    .AddParameter("@id", animal.Id);
            }

            builder.AddParameter("@n", animal.Name)
                .AddParameter("@sid", animal.SpeciesId)
                .AddParameter("@eid", animal.EnclosureId)
                .AddParameter("@ft", animal.NextFeedingTime.ToString("o"))
                .ExecuteNonQuery();

            if (animal.Id <= 0)
            {
                animal.Id = new SqlCommandBuilder(connection)
                    .WithCommandText("SELECT last_insert_rowid()")
                    .ExecuteScalar<int>();
            }
        }

        private void SaveAttributes(SqliteConnection connection, Animal animal)
        {
            foreach (var attr in animal.Attributes)
            {
                new SqlCommandBuilder(connection)
                    .WithCommandText(@"
                        INSERT OR REPLACE INTO AnimalAttributes (AnimalId, FieldDefinitionId, ValueText)
                        SELECT @aid, Id, @val FROM SpeciesFieldDefinitions WHERE FieldName = @fname AND SpeciesId = @sid")
                    .AddParameter("@aid", animal.Id)
                    .AddParameter("@val", attr.Value.ToString())
                    .AddParameter("@fname", attr.Key)
                    .AddParameter("@sid", animal.SpeciesId)
                    .ExecuteNonQuery();
            }
        }

        private Animal MapFromReader(SqliteDataReader reader)
        {
            return new Animal
            {
                Id = reader.GetInt32Safe("Id"),
                Name = reader.GetStringSafe("Name"),
                SpeciesId = reader.GetInt32Safe("SpeciesId"),
                EnclosureId = reader.GetNullableInt32("EnclosureId"),
                NextFeedingTime = reader.GetDateTime("NextFeedingTime"),
                SpeciesName = reader.GetStringOrNull("SpeciesName") ?? "Unbekannt",
                EnclosureName = reader.GetStringOrNull("EnclosureName") ?? "Kein Gehege"
            };
        }

        private void EnableForeignKeys(SqliteConnection connection)
        {
            new SqlCommandBuilder(connection)
                .WithCommandText("PRAGMA foreign_keys = ON;")
                .ExecuteNonQuery();
        }
    }
}