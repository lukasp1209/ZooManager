using System.Configuration;

namespace ZooManager.Infrastructure.Configuration
{
    public abstract class DatabaseConfig
    {
        public static string GetConnectionString()
        {
            var config = ConfigurationManager.ConnectionStrings["ZooManagerDB"];
            
            if (config != null)
            {
                return config.ConnectionString;
            }
            
            return "Data Source=zoo.db";
        }
    }
}