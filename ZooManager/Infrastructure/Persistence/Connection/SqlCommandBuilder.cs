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
        private SqliteTransaction _transaction;

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

        public SqlCommandBuilder WithTransaction(SqliteTransaction transaction)
        {
            _transaction = transaction;
            return this;
        }

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
                if (result == null || result == DBNull.Value)
                    return default(T);
                
                // Handle conversion for common types
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