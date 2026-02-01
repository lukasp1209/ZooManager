using Microsoft.Data.Sqlite;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence.Data;

namespace ZooManager.Infrastructure.Persistence.Connection
{
    public class SqlitePersistenceService : IPersistenceService
    {
        private readonly string _connectionString;
        private readonly IDbSchema _dbSchema;
        private readonly ITestDataProvider _testDataProvider;

        public SqlitePersistenceService(
            string dbFileName = "zoo.db",
            IDbSchema dbSchema = null,
            ITestDataProvider testDataProvider = null)
        {
            if (dbFileName.Contains(";"))
                dbFileName = "zoo.db";

            _connectionString = $"Data Source={dbFileName}";
            _dbSchema = dbSchema ?? new DbSchema();
            _testDataProvider = testDataProvider ?? new TestDataProvider();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                EnableForeignKeys(connection);
                _dbSchema.CreateTables(connection);

                if (IsInitialSetup(connection))
                {
                    InsertTestData(connection);
                }
            }
        }

        private bool IsInitialSetup(SqliteConnection connection)
        {
            var cmd = new SqliteCommand("SELECT COUNT(*) FROM Species", connection);
            return (long)cmd.ExecuteScalar() == 0;
        }

        private void InsertTestData(SqliteConnection connection)
        {
            DisableForeignKeys(connection);

            _testDataProvider.InsertSpecies(connection);
            _testDataProvider.InsertEnclosures(connection);
            _testDataProvider.InsertEmployees(connection);
            _testDataProvider.InsertAnimals(connection);
            _testDataProvider.InsertEmployeeQualifications(connection);
            _testDataProvider.InsertAnimalEvents(connection);
            _testDataProvider.InsertZooEvents(connection);
            _testDataProvider.InsertUsers(connection, GeneratePasswordHash);

            EnableForeignKeys(connection);
        }

        private void EnableForeignKeys(SqliteConnection connection)
        {
            ExecuteNonQuery(connection, "PRAGMA foreign_keys = ON;");
        }

        private void DisableForeignKeys(SqliteConnection connection)
        {
            ExecuteNonQuery(connection, "PRAGMA foreign_keys = OFF;");
        }

        private static void ExecuteNonQuery(SqliteConnection connection, string commandText)
        {
            using (var cmd = new SqliteCommand(commandText, connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private string GeneratePasswordHash(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(
                    System.Text.Encoding.UTF8.GetBytes(password + "ZooManagerSalt"));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public User? GetUserByUsername(string username) => 
            ExecuteUserQuery("SELECT Id, Username, PasswordHash, Role, EmployeeId, CreatedAt, IsActive FROM Users WHERE Username = @username",
                cmd => cmd.Parameters.AddWithValue("@username", username));

        public User? GetUserById(int userId) =>
            ExecuteUserQuery("SELECT Id, Username, PasswordHash, Role, EmployeeId, CreatedAt, IsActive FROM Users WHERE Id = @id",
                cmd => cmd.Parameters.AddWithValue("@id", userId));

        private User? ExecuteUserQuery(string sql, Action<SqliteCommand> addParameters)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new SqliteCommand(sql, connection))
                {
                    addParameters(cmd);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapUserFromReader(reader);
                        }
                    }
                }
            }
            return null;
        }

        private User MapUserFromReader(SqliteDataReader reader)
        {
            return new User
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                Role = (UserRole)reader.GetInt32(reader.GetOrdinal("Role")),
                EmployeeId = reader.IsDBNull(reader.GetOrdinal("EmployeeId")) ? null : reader.GetInt32(reader.GetOrdinal("EmployeeId")),
                CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
                IsActive = reader.GetInt32(reader.GetOrdinal("IsActive")) == 1
            };
        }

        public bool SaveUser(User user)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                try
                {
                    using (var cmd = new SqliteCommand())
                    {
                        cmd.Connection = connection;

                        if (user.Id <= 0)
                        {
                            cmd.CommandText = @"
                                INSERT INTO Users (Username, PasswordHash, Role, EmployeeId, CreatedAt, IsActive) 
                                VALUES (@username, @hash, @role, @empId, @created, @active)";
                            cmd.Parameters.AddWithValue("@created", user.CreatedAt.ToString("o"));
                        }
                        else
                        {
                            cmd.CommandText = @"
                                UPDATE Users 
                                SET Username = @username, PasswordHash = @hash, Role = @role, EmployeeId = @empId, IsActive = @active 
                                WHERE Id = @id";
                            cmd.Parameters.AddWithValue("@id", user.Id);
                        }

                        cmd.Parameters.AddWithValue("@username", user.Username);
                        cmd.Parameters.AddWithValue("@hash", user.PasswordHash);
                        cmd.Parameters.AddWithValue("@role", (int)user.Role);
                        cmd.Parameters.AddWithValue("@empId", (object)user.EmployeeId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@active", user.IsActive ? 1 : 0);

                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        public IEnumerable<Animal> LoadAnimals() => 
            LoadEntities(LoadAnimalsWithDetails);

        public IEnumerable<Animal> LoadAnimalsForEmployee(int employeeId)
        {
            var animals = new List<Animal>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqliteCommand(@"
                    SELECT DISTINCT a.*, s.Name AS SpeciesName, enc.Name AS EnclosureName 
                    FROM Animals a 
                    LEFT JOIN Species s ON a.SpeciesId = s.Id 
                    LEFT JOIN Enclosures enc ON a.EnclosureId = enc.Id
                    JOIN EmployeeQualifications eq ON s.Id = eq.SpeciesId
                    WHERE eq.EmployeeId = @empId", connection);

                cmd.Parameters.AddWithValue("@empId", employeeId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        animals.Add(MapAnimalFromReader(reader));
                    }
                }
            }
            return animals;
        }

        private List<Animal> LoadAnimalsWithDetails(SqliteConnection connection)
        {
            var animals = new List<Animal>();
            var cmd = new SqliteCommand(@"
                SELECT a.*, s.Name AS SpeciesName, e.Name AS EnclosureName 
                FROM Animals a 
                LEFT JOIN Species s ON a.SpeciesId = s.Id 
                LEFT JOIN Enclosures e ON a.EnclosureId = e.Id", connection);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    animals.Add(MapAnimalFromReader(reader));
                }
            }

            foreach (var animal in animals)
            {
                LoadAnimalEvents(connection, animal);
                LoadAnimalAttributes(connection, animal);
            }

            return animals;
        }

        private void LoadAnimalEvents(SqliteConnection connection, Animal animal)
        {
            var cmd = new SqliteCommand("SELECT EventDate, EventType, Description FROM AnimalEvents WHERE AnimalId = @id", connection);
            cmd.Parameters.AddWithValue("@id", animal.Id);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    animal.Events.Add(new AnimalEvent
                    {
                        Date = DateTime.Parse(reader.GetString(0)),
                        Type = reader.GetString(1),
                        Description = reader.GetString(2)
                    });
                }
            }
        }

        private void LoadAnimalAttributes(SqliteConnection connection, Animal animal)
        {
            var cmd = new SqliteCommand(@"
                SELECT d.FieldName, a.ValueText 
                FROM AnimalAttributes a 
                JOIN SpeciesFieldDefinitions d ON a.FieldDefinitionId = d.Id 
                WHERE a.AnimalId = @id", connection);
            cmd.Parameters.AddWithValue("@id", animal.Id);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    animal.Attributes[reader.GetString(0)] = reader.GetString(1);
                }
            }
        }

        private Animal MapAnimalFromReader(SqliteDataReader reader)
        {
            return new Animal
            {
                Id = reader.GetInt32Safe("Id"),
                Name = reader.GetStringSafe("Name"),
                SpeciesId = reader.GetInt32Safe("SpeciesId"),
                EnclosureId = reader.GetNullableInt32("EnclosureId"),
                NextFeedingTime = DateTime.Parse(reader.GetStringSafe("NextFeedingTime")),
                SpeciesName = reader.GetStringOrNull("SpeciesName") ?? "Unbekannt",
                EnclosureName = reader.GetStringOrNull("EnclosureName") ?? "Kein Gehege"
            };
        }

        public void AddAnimalEvent(int animalId, AnimalEvent ev)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO AnimalEvents (AnimalId, EventDate, EventType, Description)
                        VALUES ($animalId, $eventDate, $eventType, $description)";

                    cmd.Parameters.AddWithValue("$animalId", animalId);
                    cmd.Parameters.AddWithValue("$eventDate", ev.Date.ToString("o"));
                    cmd.Parameters.AddWithValue("$eventType", ev.Type ?? string.Empty);
                    cmd.Parameters.AddWithValue("$description", ev.Description ?? string.Empty);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SaveAnimals(IEnumerable<Animal> animals)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                foreach (var animal in animals)
                {
                    SaveSingleAnimal(connection, animal);
                    SaveAnimalAttributes(connection, animal);
                }
            }
        }

        private void SaveSingleAnimal(SqliteConnection connection, Animal animal)
        {
            using (var cmd = new SqliteCommand())
            {
                cmd.Connection = connection;
                cmd.CommandText = animal.Id <= 0
                    ? @"INSERT INTO Animals (Name, SpeciesId, EnclosureId, NextFeedingTime) 
                       VALUES (@n, @sid, @eid, @ft)"
                    : @"INSERT OR REPLACE INTO Animals (Id, Name, SpeciesId, EnclosureId, NextFeedingTime) 
                       VALUES (@id, @n, @sid, @eid, @ft)";

                if (animal.Id > 0)
                    cmd.Parameters.AddWithValue("@id", animal.Id);

                cmd.Parameters.AddWithValue("@n", animal.Name);
                cmd.Parameters.AddWithValue("@sid", animal.SpeciesId);
                cmd.Parameters.AddWithValue("@eid", (object)animal.EnclosureId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ft", animal.NextFeedingTime.ToString("o"));
                cmd.ExecuteNonQuery();

                if (animal.Id <= 0)
                {
                    animal.Id = Convert.ToInt32(
                        new SqliteCommand("SELECT last_insert_rowid()", connection).ExecuteScalar());
                }
            }
        }

        private void SaveAnimalAttributes(SqliteConnection connection, Animal animal)
        {
            foreach (var attr in animal.Attributes)
            {
                using (var cmd = new SqliteCommand(@"
                    INSERT OR REPLACE INTO AnimalAttributes (AnimalId, FieldDefinitionId, ValueText)
                    SELECT @aid, Id, @val FROM SpeciesFieldDefinitions WHERE FieldName = @fname AND SpeciesId = @sid", connection))
                {
                    cmd.Parameters.AddWithValue("@aid", animal.Id);
                    cmd.Parameters.AddWithValue("@val", attr.Value.ToString());
                    cmd.Parameters.AddWithValue("@fname", attr.Key);
                    cmd.Parameters.AddWithValue("@sid", animal.SpeciesId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public IEnumerable<Employee> LoadEmployees() => 
            LoadEntities(LoadEmployeesWithQualifications);

        private List<Employee> LoadEmployeesWithQualifications(SqliteConnection connection)
        {
            var employees = new List<Employee>();
            using (var cmd = new SqliteCommand("SELECT Id, FirstName, LastName FROM Employees", connection))
            {
                using (var reader = cmd.ExecuteReader())
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

            foreach (var emp in employees)
            {
                LoadEmployeeQualifications(connection, emp);
            }

            return employees;
        }

        private void LoadEmployeeQualifications(SqliteConnection connection, Employee emp)
        {
            using (var cmd = new SqliteCommand("SELECT SpeciesId FROM EmployeeQualifications WHERE EmployeeId = @id", connection))
            {
                cmd.Parameters.AddWithValue("@id", emp.Id);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        emp.QualifiedSpeciesIds.Add(reader.GetInt32(0));
                    }
                }
            }
        }

        public void SaveEmployees(IEnumerable<Employee> employees)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                foreach (var emp in employees)
                {
                    SaveSingleEmployee(connection, emp);
                    SaveEmployeeQualifications(emp.Id, emp.QualifiedSpeciesIds);
                }
            }
        }

        private void SaveSingleEmployee(SqliteConnection connection, Employee emp)
        {
            using (var cmd = new SqliteCommand())
            {
                cmd.Connection = connection;
                cmd.CommandText = emp.Id <= 0
                    ? "INSERT INTO Employees (FirstName, LastName) VALUES (@f, @l)"
                    : "INSERT OR REPLACE INTO Employees (Id, FirstName, LastName) VALUES (@id, @f, @l)";

                if (emp.Id > 0)
                    cmd.Parameters.AddWithValue("@id", emp.Id);

                cmd.Parameters.AddWithValue("@f", emp.FirstName);
                cmd.Parameters.AddWithValue("@l", emp.LastName);
                cmd.ExecuteNonQuery();

                if (emp.Id <= 0)
                {
                    emp.Id = Convert.ToInt32(
                        new SqliteCommand("SELECT last_insert_rowid()", connection).ExecuteScalar());
                }
            }
        }

        public void SaveEmployeeQualifications(int employeeId, List<int> speciesIds)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new SqliteCommand("DELETE FROM EmployeeQualifications WHERE EmployeeId = @id", connection))
                {
                    cmd.Parameters.AddWithValue("@id", employeeId);
                    cmd.ExecuteNonQuery();
                }

                foreach (var sid in speciesIds)
                {
                    using (var cmd = new SqliteCommand("INSERT INTO EmployeeQualifications (EmployeeId, SpeciesId) VALUES (@eid, @sid)", connection))
                    {
                        cmd.Parameters.AddWithValue("@eid", employeeId);
                        cmd.Parameters.AddWithValue("@sid", sid);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public IEnumerable<Species> LoadSpecies() => 
            LoadEntities(LoadSpeciesEntities);

        private List<Species> LoadSpeciesEntities(SqliteConnection connection)
        {
            var list = new List<Species>();
            using (var cmd = new SqliteCommand("SELECT * FROM Species", connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Species
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            RequiredClimate = reader.IsDBNull(2) ? null : reader.GetString(2),
                            NeedsWater = reader.GetInt32(3) == 1,
                            MinSpacePerAnimal = reader.GetDouble(4)
                        });
                    }
                }
            }
            return list;
        }

        public void SaveSpecies(IEnumerable<Species> speciesList)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                foreach (var s in speciesList)
                {
                    SaveSingleSpecies(connection, s);
                }
            }
        }

        private void SaveSingleSpecies(SqliteConnection connection, Species s)
        {
            using (var cmd = new SqliteCommand())
            {
                cmd.Connection = connection;
                cmd.CommandText = s.Id <= 0
                    ? "INSERT INTO Species (Name, RequiredClimate, NeedsWater, MinSpacePerAnimal) VALUES (@n, @c, @w, @s)"
                    : "INSERT OR REPLACE INTO Species (Id, Name, RequiredClimate, NeedsWater, MinSpacePerAnimal) VALUES (@id, @n, @c, @w, @s)";

                if (s.Id > 0)
                    cmd.Parameters.AddWithValue("@id", s.Id);

                cmd.Parameters.AddWithValue("@n", s.Name);
                cmd.Parameters.AddWithValue("@c", (object)s.RequiredClimate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@w", s.NeedsWater ? 1 : 0);
                cmd.Parameters.AddWithValue("@s", s.MinSpacePerAnimal);
                cmd.ExecuteNonQuery();

                if (s.Id <= 0)
                {
                    s.Id = Convert.ToInt32(
                        new SqliteCommand("SELECT last_insert_rowid()", connection).ExecuteScalar());
                }
            }
        }

        public IEnumerable<Enclosure> LoadEnclosures() => 
            LoadEntities(LoadEnclosureEntities);

        private List<Enclosure> LoadEnclosureEntities(SqliteConnection connection)
        {
            var list = new List<Enclosure>();
            using (var cmd = new SqliteCommand("SELECT * FROM Enclosures", connection))
            {
                using (var reader = cmd.ExecuteReader())
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
        }

        public void SaveEnclosures(IEnumerable<Enclosure> enclosures)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                foreach (var enc in enclosures)
                {
                    SaveSingleEnclosure(connection, enc);
                }
            }
        }

        private void SaveSingleEnclosure(SqliteConnection connection, Enclosure enc)
        {
            using (var cmd = new SqliteCommand())
            {
                cmd.Connection = connection;
                cmd.CommandText = enc.Id <= 0
                    ? "INSERT INTO Enclosures (Name, ClimateType, HasWaterAccess, TotalArea, MaxCapacity) VALUES (@n, @c, @w, @a, @m)"
                    : "INSERT OR REPLACE INTO Enclosures (Id, Name, ClimateType, HasWaterAccess, TotalArea, MaxCapacity) VALUES (@id, @n, @c, @w, @a, @m)";

                if (enc.Id > 0)
                    cmd.Parameters.AddWithValue("@id", enc.Id);

                cmd.Parameters.AddWithValue("@n", enc.Name);
                cmd.Parameters.AddWithValue("@c", enc.ClimateType);
                cmd.Parameters.AddWithValue("@w", enc.HasWaterAccess ? 1 : 0);
                cmd.Parameters.AddWithValue("@a", enc.TotalArea);
                cmd.Parameters.AddWithValue("@m", enc.MaxCapacity);
                cmd.ExecuteNonQuery();

                if (enc.Id <= 0)
                {
                    enc.Id = Convert.ToInt32(
                        new SqliteCommand("SELECT last_insert_rowid()", connection).ExecuteScalar());
                }
            }
        }

        public IEnumerable<ZooEvent> LoadEvents() => 
            LoadEntities(LoadZooEvents);

        private List<ZooEvent> LoadZooEvents(SqliteConnection connection)
        {
            var list = new List<ZooEvent>();
            using (var cmd = new SqliteCommand("SELECT Title, Description, Start FROM ZooEvents ORDER BY Start ASC", connection))
            {
                using (var reader = cmd.ExecuteReader())
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
        }

        public void SaveEvents(IEnumerable<ZooEvent> events)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                foreach (var ev in events)
                {
                    using (var cmd = new SqliteCommand("INSERT INTO ZooEvents (Title, Description, Start) VALUES (@t, @d, @s)", connection))
                    {
                        cmd.Parameters.AddWithValue("@t", ev.Title);
                        cmd.Parameters.AddWithValue("@d", ev.Description);
                        cmd.Parameters.AddWithValue("@s", ev.Start.ToString("o"));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void DeleteAnimal(int animalId) => 
            DeleteEntity("Animals", animalId);

        public void DeleteEmployee(int employeeId) => 
            DeleteEntity("Employees", employeeId);

        public void DeleteSpecies(int speciesId) => 
            DeleteEntity("Species", speciesId);

        public void DeleteEnclosure(int enclosureId) => 
            DeleteEntity("Enclosures", enclosureId);

        public void DeleteEvent(string title, DateTime start)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                EnableForeignKeys(connection);
                using (var cmd = new SqliteCommand("DELETE FROM ZooEvents WHERE Title = @t AND Start = @s", connection))
                {
                    cmd.Parameters.AddWithValue("@t", title);
                    cmd.Parameters.AddWithValue("@s", start.ToString("o"));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void DeleteEntity(string tableName, int id)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                EnableForeignKeys(connection);
                using (var cmd = new SqliteCommand($"DELETE FROM {tableName} WHERE Id = @id", connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private IEnumerable<T> LoadEntities<T>(Func<SqliteConnection, List<T>> loader)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                return loader(connection);
            }
        }
    }
}