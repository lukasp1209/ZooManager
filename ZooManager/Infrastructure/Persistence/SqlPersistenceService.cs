using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;

namespace ZooManager.Infrastructure.Persistence
{
    public class SqlPersistenceService : IPersistenceService
    {
        private readonly string _connectionString;

        public SqlPersistenceService(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IEnumerable<T> Load<T>(string tableName) where T : new()
        {
            var result = new List<T>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand($"SELECT * FROM {tableName}", connection);
                using (var reader = command.ExecuteReader())
                {
                    var props = typeof(T).GetProperties();
                    while (reader.Read())
                    {
                        var item = new T();
                        foreach (var prop in props)
                        {
                            if (!reader.IsDBNull(reader.GetOrdinal(prop.Name)))
                            {
                                var value = reader[prop.Name];
                                prop.SetValue(item, value);
                            }
                        }
                        result.Add(item);
                    }
                }
            }
            return result;
        }

        private void Save<T>(IEnumerable<T> items, string tableName)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Für vollständige Funktionalität müssten hier Insert, Update, Delete-Logik implementiert werden.
                // Für eine einfache Implementierung könnten wir z.B. alle vorherigen Einträge löschen und neu einfügen.
                var deleteCommand = new SqlCommand($"DELETE FROM {tableName}", connection);
                deleteCommand.ExecuteNonQuery();

                foreach (var item in items)
                {
                    var props = typeof(T).GetProperties();
                    var columnNames = string.Join(", ", props.Select(p => p.Name));
                    var paramNames = string.Join(", ", props.Select(p => "@" + p.Name));
                    var insertCommand = new SqlCommand($"INSERT INTO {tableName} ({columnNames}) VALUES ({paramNames})", connection);

                    foreach (var prop in props)
                    {
                        insertCommand.Parameters.AddWithValue("@" + prop.Name, prop.GetValue(item) ?? DBNull.Value);
                    }
                    insertCommand.ExecuteNonQuery();
                }
            }
        }

        public IEnumerable<Animal> LoadAnimals() => Load<Animal>("Animals");
        public void SaveAnimals(IEnumerable<Animal> animals) => Save(animals, "Animals");

        public IEnumerable<Species> LoadSpecies() => Load<Species>("Species");
        public void SaveSpecies(IEnumerable<Species> species) => Save(species, "Species");

        public IEnumerable<Enclosure> LoadEnclosures() => Load<Enclosure>("Enclosures");
        public void SaveEnclosures(IEnumerable<Enclosure> enclosures) => Save(enclosures, "Enclosures");

        public IEnumerable<Employee> LoadEmployees() => Load<Employee>("Employees");
        public void SaveEmployees(IEnumerable<Employee> employees) => Save(employees, "Employees");

        public IEnumerable<ZooEvent> LoadEvents() => Load<ZooEvent>("Events");
        public void SaveEvents(IEnumerable<ZooEvent> events) => Save(events, "Events");
    }
}