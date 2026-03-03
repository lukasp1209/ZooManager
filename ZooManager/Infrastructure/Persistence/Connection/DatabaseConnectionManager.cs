using Microsoft.Data.Sqlite;
using System;

namespace ZooManager.Infrastructure.Persistence.Connection
{
    /// <summary>
    /// Manages SQLite database connections and ensures proper opening and disposal.
    /// </summary>
    internal class DatabaseConnectionManager : IDisposable
    {
        private readonly string _connectionString;
        private SqliteConnection _connection; // Optional reusable connection

        public DatabaseConnectionManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Returns an open connection (reuses existing one if possible).
        /// </summary>
        public SqliteConnection GetConnection()
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            {
                _connection = new SqliteConnection(_connectionString);
                _connection.Open();
            }

            return _connection;
        }

        /// <summary>
        /// Executes a database operation with a temporary connection and returns a result.
        /// </summary>
        public T ExecuteWithConnection<T>(Func<SqliteConnection, T> operation)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                return operation(connection);
            }
        }

        /// <summary>
        /// Executes a database operation with a temporary connection (no return value).
        /// </summary>
        public void ExecuteWithConnection(Action<SqliteConnection> operation)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                operation(connection);
            }
        }

        /// <summary>
        /// Disposes the reusable connection.
        /// </summary>
        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}