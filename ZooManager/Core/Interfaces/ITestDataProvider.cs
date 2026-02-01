using Microsoft.Data.Sqlite;

namespace ZooManager.Core.Interfaces
{
    public interface ITestDataProvider
    {
        void InsertSpecies(SqliteConnection connection);
        void InsertEnclosures(SqliteConnection connection);
        void InsertEmployees(SqliteConnection connection);
        void InsertAnimals(SqliteConnection connection);
        void InsertEmployeeQualifications(SqliteConnection connection);
        void InsertAnimalEvents(SqliteConnection connection);
        void InsertZooEvents(SqliteConnection connection);
        void InsertUsers(SqliteConnection connection, Func<string, string> hashGenerator);
    }
}