using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence.Connection;

namespace ZooManager.Infrastructure.Persistence.Repositories
{
    internal class EventRepository
    {
        private readonly DatabaseConnectionManager _connectionManager;

        public EventRepository(DatabaseConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public List<ZooEvent> GetAll()
        {
            return _connectionManager.ExecuteWithConnection(connection =>
            {
                var list = new List<ZooEvent>();
                using (var command = new SqlCommandBuilder(connection)
                    .WithCommandText("SELECT Title, Description, Start FROM ZooEvents ORDER BY Start ASC")
                    .Build())
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new ZooEvent
                            {
                                Title = reader.GetString(reader.GetOrdinal("Title")),
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                Start = DateTime.Parse(reader.GetString(reader.GetOrdinal("Start")))
                            });
                        }
                    }
                }
                return list;
            });
        }

        public void Save(IEnumerable<ZooEvent> events)
        {
            _connectionManager.ExecuteWithConnection(connection =>
            {
                foreach (var ev in events)
                {
                    new SqlCommandBuilder(connection)
                        .WithCommandText("INSERT INTO ZooEvents (Title, Description, Start) VALUES (@t, @d, @s)")
                        .AddParameter("@t", ev.Title)
                        .AddParameter("@d", ev.Description)
                        .AddParameter("@s", ev.Start.ToString("o"))
                        .ExecuteNonQuery();
                }
            });
        }

        public void Delete(string title, DateTime start)
        {
            _connectionManager.ExecuteWithConnection(connection =>
            {
                EnableForeignKeys(connection);
                new SqlCommandBuilder(connection)
                    .WithCommandText("DELETE FROM ZooEvents WHERE Title = @t AND Start = @s")
                    .AddParameter("@t", title)
                    .AddParameter("@s", start.ToString("o"))
                    .ExecuteNonQuery();
            });
        }

        private void EnableForeignKeys(SqliteConnection connection)
        {
            new SqlCommandBuilder(connection)
                .WithCommandText("PRAGMA foreign_keys = ON;")
                .ExecuteNonQuery();
        }
    }
}