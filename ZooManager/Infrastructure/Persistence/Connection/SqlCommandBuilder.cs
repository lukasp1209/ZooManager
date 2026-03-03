using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace ZooManager.Infrastructure.Persistence.Connection
{
    /// <summary>
    /// Fluent builder for creating and executing parameterized SQLite commands.
    /// </summary>
    internal class SqlCommandBuilder
    {
        private readonly SqliteConnection _connection;
        private string _commandText;
        private readonly Dictionary<string, object> _parameters = new Dictionary<string, object>();
        private SqliteTransaction _transaction;

        public SqlCommandBuilder(SqliteConnection connection)
        {
            _connection = connection;
        }

        // Sets the SQL command text
        public SqlCommandBuilder WithCommandText(string commandText)
        {
            _commandText = commandText;
            return this;
        }

        // Adds a parameter (null values converted to DBNull)
        public SqlCommandBuilder AddParameter(string name, object value)
        {
            _parameters[name] = value ?? DBNull.Value;
            return this;
        }

        // Assigns an optional transaction
        public SqlCommandBuilder WithTransaction(SqliteTransaction transaction)
        {
            _transaction = transaction;
            return this;
        }

        /// <summary>
        /// Builds the configured SqliteCommand instance.
        /// </summary>
        public SqliteCommand Build()
        {
            var command = new SqliteCommand(_commandText, _connection);

            if (_transaction != null)
            {
                command.Transaction = _transaction;
            }

            foreach (var param in _parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value);
            }

            return command;
        }

        // Executes command without result set
        public int ExecuteNonQuery()
        {
            using (var command = Build())
            {
                return command.ExecuteNonQuery();
            }
        }

        // Executes command and returns a single value
        public T ExecuteScalar<T>()
        {
            using (var command = Build())
            {
                var result = command.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                    return default(T);

                // Basic type conversion handling
                if (typeof(T) == typeof(int))
                    return (T)(object)Convert.ToInt32(result);
                if (typeof(T) == typeof(long))
                    return (T)(object)Convert.ToInt64(result);
                if (typeof(T) == typeof(string))
                    return (T)(object)result.ToString();

                return (T)result;
            }
        }
    }
}