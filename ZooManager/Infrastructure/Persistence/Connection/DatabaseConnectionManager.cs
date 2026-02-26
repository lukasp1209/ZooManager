using Microsoft.Data.Sqlite;
using System;

namespace ZooManager.Infrastructure.Persistence.Connection
{
    internal class DatabaseConnectionManager : IDisposable
    {
        private readonly string _connectionString;
        private SqliteConnection _connection;

        public DatabaseConnectionManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SqliteConnection GetConnection()
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            {
                _connection = new SqliteConnection(_connectionString);
                _connection.Open();
            }
            return _connection;
        }

        public T ExecuteWithConnection<T>(Func<SqliteConnection, T> operation)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                return operation(connection);
            }
        }

        public void ExecuteWithConnection(Action<SqliteConnection> operation)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                operation(connection);
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}