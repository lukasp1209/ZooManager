using Microsoft.Data.Sqlite;

namespace ZooManager.Infrastructure.Persistence.Data
{
    public static class SqliteDataReaderExtensions
    {
        public static int GetInt32Safe(this SqliteDataReader reader, string columnName)
        {
            return reader.GetInt32(reader.GetOrdinal(columnName));
        }

        public static string GetStringSafe(this SqliteDataReader reader, string columnName)
        {
            return reader.GetString(reader.GetOrdinal(columnName));
        }

        public static bool IsDBNullSafe(this SqliteDataReader reader, string columnName)
        {
            return reader.IsDBNull(reader.GetOrdinal(columnName));
        }

        public static int? GetNullableInt32(this SqliteDataReader reader, string columnName)
        {
            return reader.IsDBNullSafe(columnName) ? null : reader.GetInt32Safe(columnName);
        }

        public static string GetStringOrNull(this SqliteDataReader reader, string columnName)
        {
            return reader.IsDBNullSafe(columnName) ? null : reader.GetStringSafe(columnName);
        }
    }
}