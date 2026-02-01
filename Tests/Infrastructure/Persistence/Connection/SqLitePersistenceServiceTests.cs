using Microsoft.Data.Sqlite;
using NUnit.Framework;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence.Connection;

namespace Tests.Infrastructure.Persistence.Connection;

[TestFixture]
[NonParallelizable]
public class SqlitePersistenceServiceTests
{
    [Test]
    public void Constructor_ShouldInitializeDatabase_AndSeedSpecies()
    {
        using var db = new TempSqliteDbFile();

        var sut = new SqlitePersistenceService(
            dbFileName: db.Path,
            dbSchema: new MinimalSchema(),
            testDataProvider: new MinimalTestDataProvider());

        var species = sut.LoadSpecies();

        Assert.That(species, Is.Not.Null);
        Assert.That(species, Is.Not.Empty);
    }

    [Test]
    public void SaveUser_Insert_ThenGetUserByUsername_ShouldReturnUser()
    {
        using var db = new TempSqliteDbFile();

        var sut = new SqlitePersistenceService(
            dbFileName: db.Path,
            dbSchema: new MinimalSchema(),
            testDataProvider: new MinimalTestDataProvider());

        var ok = sut.SaveUser(new User
        {
            Id = 0,
            Username = "unit_test_user",
            PasswordHash = "hash-placeholder",
            Role = UserRole.Admin,
            EmployeeId = null,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });

        Assert.That(ok, Is.True);

        var loaded = sut.GetUserByUsername("unit_test_user");
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Username, Is.EqualTo("unit_test_user"));
        Assert.That(loaded.Role, Is.EqualTo(UserRole.Admin));
        Assert.That(loaded.IsActive, Is.True);
        Assert.That(loaded.EmployeeId, Is.Null);
    }

    [Test]
    public void SaveUser_UpdateExisting_ShouldPersistChanges()
    {
        using var db = new TempSqliteDbFile();

        var sut = new SqlitePersistenceService(
            dbFileName: db.Path,
            dbSchema: new MinimalSchema(),
            testDataProvider: new MinimalTestDataProvider());

        // Insert
        sut.SaveUser(new User
        {
            Id = 0,
            Username = "unit_test_user2",
            PasswordHash = "hash-placeholder",
            Role = UserRole.Employee,
            EmployeeId = 123,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });

        var inserted = sut.GetUserByUsername("unit_test_user2");
        Assert.That(inserted, Is.Not.Null);

        inserted!.Role = UserRole.Admin;
        inserted.IsActive = false;
        inserted.EmployeeId = null;

        var ok = sut.SaveUser(inserted);
        Assert.That(ok, Is.True);

        var reloaded = sut.GetUserById(inserted.Id);
        Assert.That(reloaded, Is.Not.Null);
        Assert.That(reloaded!.Role, Is.EqualTo(UserRole.Admin));
        Assert.That(reloaded.IsActive, Is.False);
        Assert.That(reloaded.EmployeeId, Is.Null);
    }

    private sealed class TempSqliteDbFile : IDisposable
    {
        public string Path { get; }

        public TempSqliteDbFile()
        {
            var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ZooManager.Tests");
            Directory.CreateDirectory(dir);

            Path = System.IO.Path.Combine(dir, $"zoo_test_{Guid.NewGuid():N}.db");
        }

        public void Dispose()
        {
            try { if (File.Exists(Path)) File.Delete(Path); } catch { /* ignore */ }
        }
    }

    private sealed class MinimalSchema : IDbSchema
    {
        public void CreateTables(SqliteConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Species (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    RequiredClimate TEXT NULL,
    NeedsWater INTEGER NOT NULL DEFAULT 0,
    MinSpacePerAnimal REAL NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    Role INTEGER NOT NULL,
    EmployeeId INTEGER NULL,
    CreatedAt TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1
);
";
            cmd.ExecuteNonQuery();
        }
    }

    private sealed class MinimalTestDataProvider : ITestDataProvider
    {
        public void InsertSpecies(SqliteConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
INSERT INTO Species (Name, RequiredClimate, NeedsWater, MinSpacePerAnimal)
VALUES ('TestSpecies', NULL, 0, 10.0);
";
            cmd.ExecuteNonQuery();
        }

        public void InsertEnclosures(SqliteConnection connection) { }
        public void InsertEmployees(SqliteConnection connection) { }
        public void InsertAnimals(SqliteConnection connection) { }
        public void InsertEmployeeQualifications(SqliteConnection connection) { }
        public void InsertAnimalEvents(SqliteConnection connection) { }
        public void InsertZooEvents(SqliteConnection connection) { }
        public void InsertUsers(SqliteConnection connection, Func<string, string> hashGenerator) { }
    }
}