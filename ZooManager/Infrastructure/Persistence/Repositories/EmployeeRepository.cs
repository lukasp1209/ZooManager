using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence.Connection;

namespace ZooManager.Infrastructure.Persistence.Repositories
{
    internal class EmployeeRepository
    {
        private readonly DatabaseConnectionManager _connectionManager;

        public EmployeeRepository(DatabaseConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public List<Employee> GetAll()
        {
            return _connectionManager.ExecuteWithConnection(connection =>
            {
                var employees = LoadEmployeesBasicInfo(connection);
                foreach (var emp in employees)
                {
                    LoadEmployeeQualifications(connection, emp);
                }
                return employees;
            });
        }

        public void Save(IEnumerable<Employee> employees)
        {
            _connectionManager.ExecuteWithConnection(connection =>
            {
                foreach (var emp in employees)
                {
                    SaveSingle(connection, emp);
                    SaveQualifications(connection, emp.Id, emp.QualifiedSpeciesIds);
                }
            });
        }

        public void Delete(int employeeId)
        {
            _connectionManager.ExecuteWithConnection(connection =>
            {
                EnableForeignKeys(connection);
                new SqlCommandBuilder(connection)
                    .WithCommandText("DELETE FROM Employees WHERE Id = @id")
                    .AddParameter("@id", employeeId)
                    .ExecuteNonQuery();
            });
        }

        public void SaveQualifications(int employeeId, List<int> speciesIds)
        {
            _connectionManager.ExecuteWithConnection(connection =>
            {
                SaveQualifications(connection, employeeId, speciesIds);
            });
        }

        private List<Employee> LoadEmployeesBasicInfo(SqliteConnection connection)
        {
            var employees = new List<Employee>();
            using (var command = new SqlCommandBuilder(connection)
                .WithCommandText("SELECT Id, FirstName, LastName FROM Employees")
                .Build())
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        employees.Add(new Employee
                        {
                            Id = reader.GetInt32(0),
                            FirstName = reader.GetString(1),
                            LastName = reader.GetString(2)
                        });
                    }
                }
            }
            return employees;
        }

        private void LoadEmployeeQualifications(SqliteConnection connection, Employee emp)
        {
            using (var command = new SqlCommandBuilder(connection)
                .WithCommandText("SELECT SpeciesId FROM EmployeeQualifications WHERE EmployeeId = @id")
                .AddParameter("@id", emp.Id)
                .Build())
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        emp.QualifiedSpeciesIds.Add(reader.GetInt32(0));
                    }
                }
            }
        }

        private void SaveSingle(SqliteConnection connection, Employee emp)
        {
            var builder = new SqlCommandBuilder(connection);

            if (emp.Id <= 0)
            {
                builder.WithCommandText("INSERT INTO Employees (FirstName, LastName) VALUES (@f, @l)");
            }
            else
            {
                builder.WithCommandText("INSERT OR REPLACE INTO Employees (Id, FirstName, LastName) VALUES (@id, @f, @l)")
                    .AddParameter("@id", emp.Id);
            }

            builder.AddParameter("@f", emp.FirstName)
                .AddParameter("@l", emp.LastName)
                .ExecuteNonQuery();

            if (emp.Id <= 0)
            {
                emp.Id = new SqlCommandBuilder(connection)
                    .WithCommandText("SELECT last_insert_rowid()")
                    .ExecuteScalar<int>();
            }
        }

        private void SaveQualifications(SqliteConnection connection, int employeeId, List<int> speciesIds)
        {
            new SqlCommandBuilder(connection)
                .WithCommandText("DELETE FROM EmployeeQualifications WHERE EmployeeId = @id")
                .AddParameter("@id", employeeId)
                .ExecuteNonQuery();

            foreach (var sid in speciesIds)
            {
                new SqlCommandBuilder(connection)
                    .WithCommandText("INSERT INTO EmployeeQualifications (EmployeeId, SpeciesId) VALUES (@eid, @sid)")
                    .AddParameter("@eid", employeeId)
                    .AddParameter("@sid", sid)
                    .ExecuteNonQuery();
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