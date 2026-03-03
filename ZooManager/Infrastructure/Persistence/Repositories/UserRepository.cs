using Microsoft.Data.Sqlite;
using System;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence.Connection;
using ZooManager.Infrastructure.Persistence.Extensions;

namespace ZooManager.Infrastructure.Persistence.Repositories
{
    internal class UserRepository
    {
        private readonly DatabaseConnectionManager _connectionManager;

        public UserRepository(DatabaseConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public User GetByUsername(string username)
        {
            return _connectionManager.ExecuteWithConnection(connection =>
            {
                using (var command = new SqlCommandBuilder(connection)
                    .WithCommandText("SELECT Id, Username, PasswordHash, Role, EmployeeId, CreatedAt, IsActive FROM Users WHERE Username = @username")
                    .AddParameter("@username", username)
                    .Build())
                {
                    using (var reader = command.ExecuteReader())
                    {
                        return reader.Read() ? MapFromReader(reader) : null;
                    }
                }
            });
        }

        public User GetById(int userId)
        {
            return _connectionManager.ExecuteWithConnection(connection =>
            {
                using (var command = new SqlCommandBuilder(connection)
                    .WithCommandText("SELECT Id, Username, PasswordHash, Role, EmployeeId, CreatedAt, IsActive FROM Users WHERE Id = @id")
                    .AddParameter("@id", userId)
                    .Build())
                {
                    using (var reader = command.ExecuteReader())
                    {
                        return reader.Read() ? MapFromReader(reader) : null;
                    }
                }
            });
        }

        public bool Save(User user)
        {
            try
            {
                _connectionManager.ExecuteWithConnection(connection =>
                {
                    var builder = new SqlCommandBuilder(connection);

                    if (user.Id <= 0)
                    {
                        builder.WithCommandText(@"
                            INSERT INTO Users (Username, PasswordHash, Role, EmployeeId, CreatedAt, IsActive) 
                            VALUES (@username, @hash, @role, @empId, @created, @active)")
                            .AddParameter("@created", user.CreatedAt.ToString("o"));
                    }
                    else
                    {
                        builder.WithCommandText(@"
                            UPDATE Users 
                            SET Username = @username, PasswordHash = @hash, Role = @role, EmployeeId = @empId, IsActive = @active 
                            WHERE Id = @id")
                            .AddParameter("@id", user.Id);
                    }

                    builder.AddParameter("@username", user.Username)
                        .AddParameter("@hash", user.PasswordHash)
                        .AddParameter("@role", (int)user.Role)
                        .AddParameter("@empId", user.EmployeeId)
                        .AddParameter("@active", user.IsActive ? 1 : 0)
                        .ExecuteNonQuery();
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private User MapFromReader(SqliteDataReader reader)
        {
            return new User
            {
                Id = reader.GetInt32Safe("Id"),
                Username = reader.GetStringSafe("Username"),
                PasswordHash = reader.GetStringSafe("PasswordHash"),
                Role = (UserRole)reader.GetInt32Safe("Role"),
                EmployeeId = reader.GetNullableInt32("EmployeeId"),
                CreatedAt = reader.GetDateTime("CreatedAt"),
                IsActive = reader.GetBoolean("IsActive")
            };
        }
    }
}