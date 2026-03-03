using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence.Connection;

namespace ZooManager.Infrastructure.Persistence.Repositories
{
    internal class EnclosureRepository
    {
        private readonly DatabaseConnectionManager _connectionManager;

        public EnclosureRepository(DatabaseConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public List<Enclosure> GetAll()
        {
            return _connectionManager.ExecuteWithConnection(connection =>
            {
                var list = new List<Enclosure>();
                using (var command = new SqlCommandBuilder(connection)
                    .WithCommandText("SELECT * FROM Enclosures")
                    .Build())
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Enclosure
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                ClimateType = reader.GetString(2),
                                HasWaterAccess = reader.GetInt32(3) == 1,
                                TotalArea = reader.GetDouble(4),
                                MaxCapacity = reader.GetInt32(5)
                            });
                        }
                    }
                }
                return list;
            });
        }

        public void Save(IEnumerable<Enclosure> enclosures)
        {
            _connectionManager.ExecuteWithConnection(connection =>
            {
                foreach (var enc in enclosures)
                {
                    SaveSingle(connection, enc);
                }
            });
        }

        public void Delete(int enclosureId)
        {
            _connectionManager.ExecuteWithConnection(connection =>
            {
                EnableForeignKeys(connection);
                new SqlCommandBuilder(connection)
                    .WithCommandText("DELETE FROM Enclosures WHERE Id = @id")
                    .AddParameter("@id", enclosureId)
                    .ExecuteNonQuery();
            });
        }

        private void SaveSingle(SqliteConnection connection, Enclosure enc)
        {
            var builder = new SqlCommandBuilder(connection);

            if (enc.Id <= 0)
            {
                builder.WithCommandText("INSERT INTO Enclosures (Name, ClimateType, HasWaterAccess, TotalArea, MaxCapacity) VALUES (@n, @c, @w, @a, @m)");
            }
            else
            {
                builder.WithCommandText("INSERT OR REPLACE INTO Enclosures (Id, Name, ClimateType, HasWaterAccess, TotalArea, MaxCapacity) VALUES (@id, @n, @c, @w, @a, @m)")
                    .AddParameter("@id", enc.Id);
            }

            builder.AddParameter("@n", enc.Name)
                .AddParameter("@c", enc.ClimateType)
                .AddParameter("@w", enc.HasWaterAccess ? 1 : 0)
                .AddParameter("@a", enc.TotalArea)
                .AddParameter("@m", enc.MaxCapacity)
                .ExecuteNonQuery();

            if (enc.Id <= 0)
            {
                enc.Id = new SqlCommandBuilder(connection)
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