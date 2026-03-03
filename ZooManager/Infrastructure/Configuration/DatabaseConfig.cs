using System.Configuration;

namespace ZooManager.Infrastructure.Configuration
{
    /// <summary>
    /// Provides access to the database connection configuration.
    /// </summary>
    public abstract class DatabaseConfig
    {
        /// <summary>
        /// Returns the configured connection string or a default fallback value.
        /// </summary>
        public static string GetConnectionString()
        {
            // Read connection string from App.config / Web.config
            var config = ConfigurationManager.ConnectionStrings["ZooManagerDB"];
            
            if (config != null)
            {
                return config.ConnectionString;
            }
            
            // Fallback to default SQLite database file
            return "Data Source=zoo.db";
        }
    }
}