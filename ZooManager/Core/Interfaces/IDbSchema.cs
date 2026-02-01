using Microsoft.Data.Sqlite;

namespace ZooManager.Core.Interfaces
{
    public interface IDbSchema
    {
        void CreateTables(SqliteConnection connection);
    }
}