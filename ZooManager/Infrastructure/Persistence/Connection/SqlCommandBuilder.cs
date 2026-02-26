using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace ZooManager.Infrastructure.Persistence.Connection
{
    internal class SqlCommandBuilder
    {
        private readonly SqliteConnection _connection;
        private string _commandText;
        private readonly Dictionary<string, object> _parameters = new Dictionary<string, object>();

        public SqlCommandBuilder(SqliteConnection connection)
        {
            _connection = connection;
        }

        public SqlCommandBuilder WithCommandText(string commandText)
        {
            _commandText = commandText;
            return this;
        }

        public SqlCommandBuilder AddParameter(string name, object value)
        {
            _parameters[name] = value ?? DBNull.Value;
            return this;
        }

        public SqliteCommand Build()
        {
            var command = new SqliteCommand(_commandText, _connection);
            foreach (var param in _parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value);
            }
            return command;
        }

        public int ExecuteNonQuery()
        {
            using (var command = Build())
            {
                return command.ExecuteNonQuery();
            }
        }

        public T ExecuteScalar<T>()
        {
            using (var command = Build())
            {
                var result = command.ExecuteScalar();
                return result != null ? (T)Convert.ChangeType(result, typeof(T)) : default;
            }
        }
    }
}