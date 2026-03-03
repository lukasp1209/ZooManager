
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence.Connection;

namespace ZooManager.Infrastructure.Persistence.Repositories
{
    internal class SpeciesRepository
    {
        private readonly DatabaseConnectionManager _connectionManager;

        public SpeciesRepository(DatabaseConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public List<Species> GetAll()
        {
            return _connectionManager.ExecuteWithConnection(connection =>
            {
                var list = new List<Species>();
                using (var command = new SqlCommandBuilder(connection)
                    .WithCommandText("SELECT * FROM Species")
                    .Build())
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Species
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                RequiredClimate = reader.IsDBNull(2) ? null : reader.GetString(2),
                                NeedsWater = reader.GetInt32(3) == 1,
                                MinSpacePerAnimal = reader.GetDouble(4)
                            });
                        }
                    }
                }
                return list;
            });
        }

        public void Save(IEnumerable<Species> speciesList)
        {
            _connectionManager.ExecuteWithConnection(connection =>
            {
                foreach (var s in speciesList)
                {
                    SaveSingle(connection, s);
                }
            });
        }

        public void Delete(int speciesId)
        {
            _connectionManager.ExecuteWithConnection(connection =>
            {
                EnableForeignKeys(connection);
                new SqlCommandBuilder(connection)
                    .WithCommandText("DELETE FROM Species WHERE Id = @id")
                    .AddParameter("@id", speciesId)
                    .ExecuteNonQuery();
            });
        }

        private void SaveSingle(SqliteConnection connection, Species s)
        {
            var builder = new SqlCommandBuilder(connection);

            if (s.Id <= 0)
            {
                builder.WithCommandText("INSERT INTO Species (Name, RequiredClimate, NeedsWater, MinSpacePerAnimal) VALUES (@n, @c, @w, @s)");
            }
            else
            {
                builder.WithCommandText("INSERT OR REPLACE INTO Species (Id, Name, RequiredClimate, NeedsWater, MinSpacePerAnimal) VALUES (@id, @n, @c, @w, @s)")
                    .AddParameter("@id", s.Id);
            }

            builder.AddParameter("@n", s.Name)
                .AddParameter("@c", s.RequiredClimate)
                .AddParameter("@w", s.NeedsWater ? 1 : 0)
                .AddParameter("@s", s.MinSpacePerAnimal)
                .ExecuteNonQuery();

            if (s.Id <= 0)
            {
                s.Id = new SqlCommandBuilder(connection)
                    .WithCommandText("SELECT last_insert_rowid()")
                    .ExecuteScalar<int>();
            }
        }

        private void EnableForeignKeys(SqliteConnection connection)
        {
            new SqlCommandBuilder(connection)
                .WithCommandText("PRAGMA foreign_keys = ON;")
                .ExecuteNonQuery();
        }
    }
}